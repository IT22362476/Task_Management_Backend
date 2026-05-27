using Microsoft.EntityFrameworkCore;
using Task_Manager_Backend.Data;
using Task_Manager_Backend.DTOs.Tasks;
using Task_Manager_Backend.Models;

namespace Task_Manager_Backend.Services;

public class TaskService
{
    private readonly AppDbContext _db;

    public TaskService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<TaskSummary>> GetTasksAsync(
        string? status = null, string? priority = null, Guid? projectId = null, Guid? assigneeId = null)
    {
        var query = _db.Tasks
            .Include(t => t.Project)
            .Include(t => t.Assignee)
            .Include(t => t.TaskLabels).ThenInclude(tl => tl.Label)
            .Where(t => t.IsActive);

        if (projectId.HasValue)
            query = query.Where(t => t.ProjectId == projectId.Value);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(t => t.Status == status);
        if (!string.IsNullOrEmpty(priority))
            query = query.Where(t => t.Priority == priority);
        if (assigneeId.HasValue)
            query = query.Where(t => t.AssigneeId == assigneeId.Value);

        var tasks = await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return tasks.Select(t => new TaskSummary(
            t.Id, t.Title, t.Description, t.Status, t.Priority,
            t.Project?.Name ?? "",
            t.Assignee?.FullName,
            t.TaskLabels.Where(tl => tl.Label != null).Select(tl => new LabelDto(
                tl.Label!.Id, tl.Label.Name, tl.Label.Color
            )),
            t.DueDate, t.CreatedAt
        ));
    }

    public async Task<IEnumerable<TaskSummary>> GetUserTasksAsync(
        Guid userId, List<Guid> projectIds,
        string? status = null, string? priority = null,
        Guid? projectId = null, Guid? assigneeId = null)
    {
        var query = _db.Tasks
            .Include(t => t.Project)
            .Include(t => t.Assignee)
            .Include(t => t.TaskLabels).ThenInclude(tl => tl.Label)
            .Where(t => t.IsActive);

        // User can see tasks assigned to them OR in projects they're members of
        query = query.Where(t =>
            t.AssigneeId == userId ||
            projectIds.Contains(t.ProjectId));

        if (projectId.HasValue)
            query = query.Where(t => t.ProjectId == projectId.Value);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(t => t.Status == status);
        if (!string.IsNullOrEmpty(priority))
            query = query.Where(t => t.Priority == priority);
        if (assigneeId.HasValue && assigneeId == userId)
            query = query.Where(t => t.AssigneeId == userId);

        var tasks = await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return tasks.Select(t => new TaskSummary(
            t.Id, t.Title, t.Description, t.Status, t.Priority,
            t.Project?.Name ?? "",
            t.Assignee?.FullName,
            t.TaskLabels.Where(tl => tl.Label != null).Select(tl => new LabelDto(
                tl.Label!.Id, tl.Label.Name, tl.Label.Color
            )),
            t.DueDate, t.CreatedAt
        ));
    }

    public async Task<TaskDetail?> GetTaskAsync(Guid id)
    {
        var task = await _db.Tasks
            .Include(t => t.Project)
            .Include(t => t.Assignee)
            .Include(t => t.CreatedBy)
            .Include(t => t.TaskLabels).ThenInclude(tl => tl.Label)
            .Include(t => t.Comments).ThenInclude(c => c.Author)
            .FirstOrDefaultAsync(t => t.Id == id && t.IsActive);

        if (task == null) return null;

        return new TaskDetail(
            task.Id, task.Title, task.Description, task.Status, task.Priority,
            task.ProjectId, task.Project?.Name ?? "",
            task.AssigneeId, task.Assignee?.FullName, null,
            task.CreatedById, task.CreatedBy?.FullName ?? "",
            task.TaskLabels.Where(tl => tl.Label != null).Select(tl => new LabelDto(
                tl.Label!.Id, tl.Label.Name, tl.Label.Color
            )),
            task.Comments.OrderByDescending(c => c.CreatedAt).Select(c => new CommentDto(
                c.Id, c.Content, c.AuthorId,
                c.Author?.FullName ?? "", null,
                c.CreatedAt, c.UpdatedAt
            )),
            task.DueDate, task.CreatedAt, task.UpdatedAt, task.CompletedAt
        );
    }

    public async Task<TaskSummary> CreateTaskAsync(CreateTaskRequest request, Guid createdById)
    {
        if (!Guid.TryParse(request.ProjectId, out var projectId))
            throw new ArgumentException("Invalid project ID");

        Guid? assigneeId = null;
        if (!string.IsNullOrEmpty(request.AssigneeId) && Guid.TryParse(request.AssigneeId, out var parsedAssignee))
            assigneeId = parsedAssignee;

        var task = new TaskItem
        {
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority,
            ProjectId = projectId,
            AssigneeId = assigneeId,
            DueDate = request.DueDate,
            CreatedById = createdById,
            Status = "todo"
        };

        // Resolve labels: find existing project labels or create new ones
        if (request.Labels != null)
        {
            var existingLabels = await _db.Labels
                .Where(l => l.ProjectId == projectId)
                .ToListAsync();

            foreach (var labelName in request.Labels)
            {
                var trimmed = labelName.Trim().ToLower();
                var label = existingLabels.FirstOrDefault(l =>
                    l.Name.ToLower() == trimmed);

                if (label == null)
                {
                    label = new Label
                    {
                        Name = labelName.Trim(),
                        Color = GetLabelColor(labelName.Trim()),
                        ProjectId = projectId
                    };
                    _db.Labels.Add(label);
                    existingLabels.Add(label);
                }

                task.TaskLabels.Add(new TaskLabel
                {
                    TaskId = task.Id,
                    LabelId = label.Id
                });
            }
        }

        _db.Tasks.Add(task);

        _db.ActivityLogs.Add(new ActivityLog
        {
            Action = "task_created",
            Description = $"Task '{task.Title}' was created",
            TaskId = task.Id,
            ProjectId = projectId,
            ActorId = createdById
        });

        await _db.SaveChangesAsync();

        // Reload with navigation properties for the response
        await _db.Entry(task).Reference(t => t.Project).LoadAsync();
        await _db.Entry(task).Reference(t => t.Assignee).LoadAsync();
        await _db.Entry(task).Collection(t => t.TaskLabels).Query()
            .Include(tl => tl.Label).LoadAsync();

        return new TaskSummary(
            task.Id, task.Title, task.Description, task.Status, task.Priority,
            task.Project?.Name ?? "",
            task.Assignee?.FullName,
            task.TaskLabels.Where(tl => tl.Label != null).Select(tl => new LabelDto(
                tl.Label!.Id, tl.Label.Name, tl.Label.Color
            )),
            task.DueDate, task.CreatedAt
        );
    }

    public async Task<TaskSummary?> UpdateTaskAsync(Guid id, UpdateTaskRequest request)
    {
        var task = await _db.Tasks
            .Include(t => t.Project)
            .Include(t => t.Assignee)
            .Include(t => t.TaskLabels).ThenInclude(tl => tl.Label)
            .FirstOrDefaultAsync(t => t.Id == id && t.IsActive);

        if (task == null) return null;

        var oldStatus = task.Status;

        if (request.Title != null) task.Title = request.Title;
        if (request.Description != null) task.Description = request.Description;
        if (request.Status != null) task.Status = request.Status;
        if (request.Priority != null) task.Priority = request.Priority;
        if (!string.IsNullOrEmpty(request.AssigneeId) && Guid.TryParse(request.AssigneeId, out var parsedAssignee))
            task.AssigneeId = parsedAssignee;
        if (request.DueDate != null) task.DueDate = request.DueDate;

        if (request.Status == "completed" && oldStatus != "completed")
            task.CompletedAt = DateTime.UtcNow;
        else if (request.Status != null && request.Status != "completed")
            task.CompletedAt = null;

        task.UpdatedAt = DateTime.UtcNow;

        // Update labels if provided
        if (request.Labels != null)
        {
            _db.TaskLabels.RemoveRange(task.TaskLabels);

            var existingLabels = await _db.Labels
                .Where(l => l.ProjectId == task.ProjectId)
                .ToListAsync();

            foreach (var labelName in request.Labels)
            {
                var trimmed = labelName.Trim().ToLower();
                var label = existingLabels.FirstOrDefault(l =>
                    l.Name.ToLower() == trimmed);

                if (label == null)
                {
                    label = new Label
                    {
                        Name = labelName.Trim(),
                        Color = GetLabelColor(labelName.Trim()),
                        ProjectId = task.ProjectId
                    };
                    _db.Labels.Add(label);
                    existingLabels.Add(label);
                }

                _db.TaskLabels.Add(new TaskLabel
                {
                    TaskId = task.Id,
                    LabelId = label.Id
                });
            }
        }

        await _db.SaveChangesAsync();

        // Auto-complete project if all tasks are done
        if (task.Status == "completed")
        {
            var allTasksInProject = await _db.Tasks
                .Where(t => t.ProjectId == task.ProjectId && t.IsActive)
                .ToListAsync();

            if (allTasksInProject.Count > 0 && allTasksInProject.All(t => t.Status == "completed"))
            {
                var project = await _db.Projects.FindAsync(task.ProjectId);
                if (project != null && project.Status != "completed")
                {
                    project.Status = "completed";
                    project.UpdatedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                }
            }
        }

        // Reload labels for response
        await _db.Entry(task).Collection(t => t.TaskLabels).Query()
            .Include(tl => tl.Label).LoadAsync();

        return new TaskSummary(
            task.Id, task.Title, task.Description, task.Status, task.Priority,
            task.Project?.Name ?? "",
            task.Assignee?.FullName,
            task.TaskLabels.Where(tl => tl.Label != null).Select(tl => new LabelDto(
                tl.Label!.Id, tl.Label.Name, tl.Label.Color
            )),
            task.DueDate, task.CreatedAt
        );
    }

    public async Task<bool> DeleteTaskAsync(Guid id)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id);
        if (task == null) return false;

        task.IsActive = false;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<CommentDto> AddCommentAsync(Guid taskId, AddCommentRequest request, Guid authorId)
    {
        var comment = new Comment
        {
            Content = request.Content,
            TaskId = taskId,
            AuthorId = authorId
        };

        _db.Comments.Add(comment);

        _db.ActivityLogs.Add(new ActivityLog
        {
            Action = "comment_added",
            Description = "A comment was added",
            TaskId = taskId,
            ActorId = authorId
        });

        await _db.SaveChangesAsync();

        return new CommentDto(
            comment.Id, comment.Content, comment.AuthorId,
            "", null, comment.CreatedAt, null
        );
    }

    private static string GetLabelColor(string name)
    {
        // Generate a consistent color from the label name using a simple hash
        var hash = name.GetHashCode(StringComparison.OrdinalIgnoreCase);
        var colors = new[]
        {
            "#ef4444", "#f59e0b", "#10b981", "#3b82f6",
            "#8b5cf6", "#ec4899", "#06b6d4", "#6366f1",
            "#14b8a6", "#a855f7", "#dc2626", "#d97706",
            "#059669", "#2563eb", "#7c3aed", "#db2777"
        };
        return colors[Math.Abs(hash) % colors.Length];
    }
}
