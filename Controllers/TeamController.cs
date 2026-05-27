using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Task_Manager_Backend.DTOs;
using Task_Manager_Backend.DTOs.Team;
using Task_Manager_Backend.Services;

namespace Task_Manager_Backend.Controllers;

[ApiController]
[Route("api/team")]
[Authorize(Roles = "Admin")]
public class TeamController : ControllerBase
{
    private readonly TeamService _teamService;

    public TeamController(TeamService teamService)
    {
        _teamService = teamService;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var stats = await _teamService.GetStatsAsync();
        return Ok(ApiResponse<TeamStatsDto>.Ok(stats));
    }

    [HttpGet("members")]
    public async Task<IActionResult> GetMembers(
        [FromQuery] string? role,
        [FromQuery] string? search)
    {
        var members = await _teamService.GetMembersAsync(role, search);
        return Ok(ApiResponse<IEnumerable<TeamMemberDto>>.Ok(members));
    }
}
