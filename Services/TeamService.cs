using Microsoft.EntityFrameworkCore;
using Task_Manager_Backend.Data;
using Task_Manager_Backend.DTOs.Team;

namespace Task_Manager_Backend.Services;

public class TeamService
{
    private readonly AppDbContext _db;

    public TeamService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<TeamStatsDto> GetStatsAsync()
    {
        var users = await _db.Users.Where(u => u.IsActive).ToListAsync();

        return new TeamStatsDto(
            users.Count,
            users.Count(u => u.Role == "Admin"),
            users.Count(u => u.Role == "Employee")
        );
    }

    public async Task<IEnumerable<TeamMemberDto>> GetMembersAsync(string? role = null, string? search = null)
    {
        var query = _db.Users.Where(u => u.IsActive);

        if (!string.IsNullOrEmpty(role))
            query = query.Where(u => u.Role == role);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(u =>
                u.FullName.Contains(search) || u.Email.Contains(search));

        var users = await query.OrderBy(u => u.FullName).ToListAsync();

        var result = new List<TeamMemberDto>();
        foreach (var user in users)
        {
            var taskCounts = await _db.Tasks
                .Where(t => t.IsActive && t.AssigneeId == user.Id)
                .GroupBy(t => 1)
                .Select(g => new
                {
                    Assigned = g.Count(),
                    Completed = g.Count(t => t.Status == "completed")
                })
                .FirstOrDefaultAsync();

            result.Add(new TeamMemberDto(
                user.Id, user.FullName, user.Email, user.Role,
                null,
                taskCounts?.Assigned ?? 0,
                taskCounts?.Completed ?? 0,
                user.CreatedAt
            ));
        }

        return result;
    }
}
