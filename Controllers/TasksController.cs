using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Task_Manager_Backend.Data;
using Task_Manager_Backend.DTOs;
using Task_Manager_Backend.DTOs.Tasks;
using Task_Manager_Backend.Services;

namespace Task_Manager_Backend.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly TaskService _taskService;
    private readonly AppDbContext _db;

    public TasksController(TaskService taskService, AppDbContext db)
    {
        _taskService = taskService;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetTasks(
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] Guid? projectId,
        [FromQuery] Guid? assigneeId)
    {
        var userId = GetUserId();
        var isAdmin = User.IsInRole("Admin");

        // Admins see all tasks; employees see tasks from their projects
        if (!isAdmin)
        {
            var userProjectIds = await _db.ProjectMembers
                .Where(pm => pm.UserId == userId)
                .Select(pm => pm.ProjectId)
                .ToListAsync();

            var tasks = await _taskService.GetUserTasksAsync(
                userId, userProjectIds, status, priority, projectId, assigneeId);
            return Ok(ApiResponse<IEnumerable<TaskSummary>>.Ok(tasks));
        }

        var adminTasks = await _taskService.GetTasksAsync(
            status, priority, projectId, assigneeId);
        return Ok(ApiResponse<IEnumerable<TaskSummary>>.Ok(adminTasks));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTask(Guid id)
    {
        var task = await _taskService.GetTaskAsync(id);
        if (task == null)
            return NotFound(ApiResponse<object>.Fail("Task not found"));
        return Ok(ApiResponse<TaskDetail>.Ok(task));
    }

    [HttpPost]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
    {
        var userId = GetUserId();
        var task = await _taskService.CreateTaskAsync(request, userId);
        if (task == null)
            return BadRequest(ApiResponse<object>.Fail("Failed to create task"));
        return CreatedAtAction(nameof(GetTask), new { id = task.Id },
            ApiResponse<TaskSummary>.Ok(task, "Task created successfully"));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateTask(Guid id, [FromBody] UpdateTaskRequest request)
    {
        var task = await _taskService.UpdateTaskAsync(id, request);
        if (task == null)
            return NotFound(ApiResponse<object>.Fail("Task not found"));
        return Ok(ApiResponse<TaskSummary>.Ok(task, "Task updated successfully"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTask(Guid id)
    {
        var result = await _taskService.DeleteTaskAsync(id);
        if (!result)
            return NotFound(ApiResponse<object>.Fail("Task not found"));
        return Ok(ApiResponse<object>.Ok(null, "Task deleted successfully"));
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<IActionResult> AddComment(Guid id, [FromBody] AddCommentRequest request)
    {
        var userId = GetUserId();
        var comment = await _taskService.AddCommentAsync(id, request, userId);
        return CreatedAtAction(nameof(GetTask), new { id },
            ApiResponse<CommentDto>.Ok(comment, "Comment added successfully"));
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("User not authenticated"));
}
