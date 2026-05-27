using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Task_Manager_Backend.Data;
using Task_Manager_Backend.DTOs;

namespace Task_Manager_Backend.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] string? search)
    {
        var query = _db.Users.Where(u => u.IsActive);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(u =>
                u.FullName.Contains(search) || u.Email.Contains(search));

        var users = await query
            .OrderBy(u => u.FullName)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.Role
            })
            .ToListAsync();

        return Ok(ApiResponse<IEnumerable<object>>.Ok(users));
    }
}
