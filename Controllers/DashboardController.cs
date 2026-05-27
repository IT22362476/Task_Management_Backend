using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Task_Manager_Backend.DTOs;
using Task_Manager_Backend.DTOs.Dashboard;
using Task_Manager_Backend.Services;

namespace Task_Manager_Backend.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly DashboardService _dashboardService;

    public DashboardController(DashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAdminDashboard()
    {
        var data = await _dashboardService.GetAdminDashboardAsync();
        return Ok(ApiResponse<AdminDashboardDto>.Ok(data));
    }

    [HttpGet("employee")]
    public async Task<IActionResult> GetEmployeeDashboard()
    {
        var userId = GetUserId();
        var data = await _dashboardService.GetEmployeeDashboardAsync(userId);
        return Ok(ApiResponse<EmployeeDashboardDto>.Ok(data));
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("User not authenticated"));
}
