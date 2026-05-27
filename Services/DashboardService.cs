using Microsoft.EntityFrameworkCore;
using Task_Manager_Backend.Data;
using Task_Manager_Backend.DTOs.Dashboard;

namespace Task_Manager_Backend.Services;

public class DashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AdminDashboardDto> GetAdminDashboardAsync()
    {
        var tasks = await _db.Tasks.Where(t => t.IsActive).ToListAsync();
        var now = DateTime.UtcNow;

        var stats = new TaskStatsDto(
            tasks.Count,
            tasks.Count(t => t.Status == "todo"),
            tasks.Count(t => t.Status == "in-progress"),
            tasks.Count(t => t.Status == "review"),
            tasks.Count(t => t.Status == "completed"),
            tasks.Count(t => t.Status != "completed" && t.DueDate < now)
        );

        var projects = await _db.Projects
            .Include(p => p.Tasks)
            .Where(p => p.IsActive)
            .ToListAsync();

        var progress = projects.Select(p => new ProjectProgressDto(
            p.Id, p.Name,
            p.Tasks.Count > 0
                ? Math.Round((double)p.Tasks.Count(t => t.Status == "completed") / p.Tasks.Count * 100, 1)
                : 0
        ));

        var activity = await _db.ActivityLogs
            .Include(a => a.Actor)
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .ToListAsync();

        var deadlines = await _db.Tasks
            .Include(t => t.Project)
            .Where(t => t.IsActive && t.Status != "completed" && t.DueDate != null)
            .OrderBy(t => t.DueDate)
            .Take(5)
            .ToListAsync();

        return new AdminDashboardDto(
            stats, progress,
            activity.Select(a => new ActivityDto(
                a.Id, a.Action, a.Description,
                a.Actor?.FullName ?? "System", a.CreatedAt
            )),
            deadlines.Select(d => new DeadlineDto(
                d.Id, d.Title, d.Project?.Name ?? "",
                d.DueDate, (d.DueDate!.Value - now).Days
            ))
        );
    }

    public async Task<EmployeeDashboardDto> GetEmployeeDashboardAsync(Guid userId)
    {
        // Get projects the user is a member of
        var userProjectIds = await _db.ProjectMembers
            .Where(pm => pm.UserId == userId)
            .Select(pm => pm.ProjectId)
            .ToListAsync();

        // Get tasks: assigned to user OR in a project they're a member of
        var tasks = await _db.Tasks
            .Include(t => t.Project)
            .Where(t => t.IsActive && (
                t.AssigneeId == userId ||
                userProjectIds.Contains(t.ProjectId)
            ))
            .ToListAsync();

        var now = DateTime.UtcNow;

        var stats = new TaskStatsDto(
            tasks.Count,
            tasks.Count(t => t.Status == "todo"),
            tasks.Count(t => t.Status == "in-progress"),
            tasks.Count(t => t.Status == "review"),
            tasks.Count(t => t.Status == "completed"),
            tasks.Count(t => t.Status != "completed" && t.DueDate < now)
        );

        var completionRate = tasks.Count > 0
            ? Math.Round((double)tasks.Count(t => t.Status == "completed") / tasks.Count * 100, 1)
            : 0;

        var activity = await _db.ActivityLogs
            .Include(a => a.Actor)
            .Where(a => a.ActorId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .ToListAsync();

        var deadlines = await _db.Tasks
            .Include(t => t.Project)
            .Where(t => t.IsActive && t.AssigneeId == userId
                        && t.Status != "completed" && t.DueDate != null)
            .OrderBy(t => t.DueDate)
            .Take(5)
            .ToListAsync();

        return new EmployeeDashboardDto(
            stats,
            activity.Select(a => new ActivityDto(
                a.Id, a.Action, a.Description,
                a.Actor?.FullName ?? "System", a.CreatedAt
            )),
            deadlines.Select(d => new DeadlineDto(
                d.Id, d.Title, d.Project?.Name ?? "",
                d.DueDate, (d.DueDate!.Value - now).Days
            )),
            completionRate
        );
    }
}
