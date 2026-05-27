using Microsoft.EntityFrameworkCore;
using Task_Manager_Backend.Data;
using Task_Manager_Backend.DTOs.Projects;
using Task_Manager_Backend.Models;

namespace Task_Manager_Backend.Services;

public class ProjectService
{
    private readonly AppDbContext _db;

    public ProjectService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<ProjectSummary>> GetProjectsAsync(Guid userId)
    {
        var projects = await _db.Projects
            .Include(p => p.Owner)
            .Include(p => p.Members)
            .Include(p => p.Tasks)
            .Where(p => p.IsActive && (p.OwnerId == userId || p.Members.Any(m => m.UserId == userId)))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return projects.Select(p => new ProjectSummary(
            p.Id, p.Name, p.Description, p.Status,
            p.Owner?.FullName ?? "Unknown",
            null,
            p.Members.Count,
            p.Tasks.Count,
            p.Tasks.Count(t => t.Status == "completed"),
            p.Tasks.Count > 0
                ? Math.Round((double)p.Tasks.Count(t => t.Status == "completed") / p.Tasks.Count * 100, 1)
                : 0,
            p.CreatedAt
        ));
    }

    public async Task<ProjectDetail?> GetProjectAsync(Guid id, Guid userId)
    {
        var project = await _db.Projects
            .Include(p => p.Owner)
            .Include(p => p.Members).ThenInclude(m => m.User)
            .Include(p => p.Tasks).ThenInclude(t => t.Assignee)
            .Include(p => p.Labels)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

        if (project == null) return null;

        return new ProjectDetail(
            project.Id, project.Name, project.Description, project.Status,
            project.OwnerId, project.Owner?.FullName ?? "Unknown",
            project.Members.Count,
            project.Members.Select(m => new ProjectMemberDto(
                m.Id, m.UserId,
                m.User?.FullName ?? "Unknown",
                m.User?.Email ?? "",
                m.Role, null
            )),
            project.Tasks.Where(t => t.IsActive).Select(t => new DTOs.Projects.TaskSummary(
                t.Id, t.Title, t.Status, t.Priority,
                t.Assignee?.FullName, t.DueDate
            )),
            project.Labels.Select(l => new DTOs.Projects.LabelDto(l.Id, l.Name, l.Color)),
            project.CreatedAt, project.UpdatedAt
        );
    }

    public async Task<ProjectSummary> CreateProjectAsync(CreateProjectRequest request, Guid ownerId)
    {
        var project = new Project
        {
            Name = request.Name,
            Description = request.Description,
            OwnerId = ownerId,
            Status = !string.IsNullOrEmpty(request.Status) ? request.Status : "planning"
        };

        project.Members.Add(new ProjectMember
        {
            ProjectId = project.Id,
            UserId = ownerId,
            Role = "owner"
        });

        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        return new ProjectSummary(
            project.Id, project.Name, project.Description, project.Status,
            "", null, 1, 0, 0, 0, project.CreatedAt
        );
    }

    public async Task<ProjectSummary?> UpdateProjectAsync(Guid id, UpdateProjectRequest request)
    {
        var project = await _db.Projects
            .Include(p => p.Owner)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

        if (project == null) return null;

        if (request.Name != null) project.Name = request.Name;
        if (request.Description != null) project.Description = request.Description;
        if (request.Status != null) project.Status = request.Status;
        project.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return new ProjectSummary(
            project.Id, project.Name, project.Description, project.Status,
            project.Owner?.FullName ?? "", null,
            0, 0, 0, 0, project.CreatedAt
        );
    }

    public async Task<bool> DeleteProjectAsync(Guid id)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id);
        if (project == null) return false;

        project.IsActive = false;
        project.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AddMemberAsync(Guid projectId, Guid userId, string role = "member")
    {
        var project = await _db.Projects
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project == null || project.Members.Any(m => m.UserId == userId))
            return false;

        project.Members.Add(new ProjectMember
        {
            ProjectId = projectId,
            UserId = userId,
            Role = role
        });

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveMemberAsync(Guid projectId, Guid userId)
    {
        var member = await _db.ProjectMembers
            .FirstOrDefaultAsync(m => m.ProjectId == projectId && m.UserId == userId);

        if (member == null) return false;

        _db.ProjectMembers.Remove(member);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<DTOs.Projects.LabelDto>> GetLabelsAsync(Guid projectId)
    {
        var labels = await _db.Labels
            .Where(l => l.ProjectId == projectId)
            .OrderBy(l => l.Name)
            .ToListAsync();

        return labels.Select(l => new DTOs.Projects.LabelDto(l.Id, l.Name, l.Color));
    }

    public async Task<DTOs.Projects.LabelDto> CreateLabelAsync(Guid projectId, CreateLabelRequest request)
    {
        var project = await _db.Projects.FindAsync(projectId);
        if (project == null)
            throw new KeyNotFoundException("Project not found");

        var label = new Label
        {
            Name = request.Name,
            Color = request.Color,
            ProjectId = projectId
        };

        _db.Labels.Add(label);
        await _db.SaveChangesAsync();

        return new DTOs.Projects.LabelDto(label.Id, label.Name, label.Color);
    }

    public async Task<DTOs.Projects.LabelDto?> UpdateLabelAsync(Guid labelId, UpdateLabelRequest request)
    {
        var label = await _db.Labels.FindAsync(labelId);
        if (label == null) return null;

        if (request.Name != null) label.Name = request.Name;
        if (request.Color != null) label.Color = request.Color;

        await _db.SaveChangesAsync();

        return new DTOs.Projects.LabelDto(label.Id, label.Name, label.Color);
    }

    public async Task<bool> DeleteLabelAsync(Guid labelId)
    {
        var label = await _db.Labels.FindAsync(labelId);
        if (label == null) return false;

        _db.Labels.Remove(label);
        await _db.SaveChangesAsync();
        return true;
    }
}
