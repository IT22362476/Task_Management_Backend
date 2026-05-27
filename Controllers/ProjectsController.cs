using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Task_Manager_Backend.DTOs;
using Task_Manager_Backend.DTOs.Projects;
using Task_Manager_Backend.Services;

namespace Task_Manager_Backend.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly ProjectService _projectService;

    public ProjectsController(ProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProjects()
    {
        var userId = GetUserId();
        var projects = await _projectService.GetProjectsAsync(userId);
        return Ok(ApiResponse<IEnumerable<ProjectSummary>>.Ok(projects));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProject(Guid id)
    {
        var userId = GetUserId();
        var project = await _projectService.GetProjectAsync(id, userId);
        if (project == null)
            return NotFound(ApiResponse<object>.Fail("Project not found"));
        return Ok(ApiResponse<ProjectDetail>.Ok(project));
    }

    [HttpPost]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest request)
    {
        var userId = GetUserId();
        var project = await _projectService.CreateProjectAsync(request, userId);
        return CreatedAtAction(nameof(GetProject), new { id = project.Id },
            ApiResponse<ProjectSummary>.Ok(project, "Project created successfully"));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateProject(Guid id, [FromBody] UpdateProjectRequest request)
    {
        var project = await _projectService.UpdateProjectAsync(id, request);
        if (project == null)
            return NotFound(ApiResponse<object>.Fail("Project not found"));
        return Ok(ApiResponse<ProjectSummary>.Ok(project, "Project updated successfully"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteProject(Guid id)
    {
        var result = await _projectService.DeleteProjectAsync(id);
        if (!result)
            return NotFound(ApiResponse<object>.Fail("Project not found"));
        return Ok(ApiResponse<object>.Ok(null, "Project deleted successfully"));
    }

    [HttpPost("{id:guid}/members")]
    public async Task<IActionResult> AddMember(Guid id, [FromBody] AddMemberRequest request)
    {
        var result = await _projectService.AddMemberAsync(id, request.UserId, request.Role ?? "member");
        if (!result)
            return BadRequest(ApiResponse<object>.Fail("Member already exists or project not found"));
        return Ok(ApiResponse<object>.Ok(null, "Member added successfully"));
    }

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId)
    {
        var result = await _projectService.RemoveMemberAsync(id, userId);
        if (!result)
            return NotFound(ApiResponse<object>.Fail("Member not found"));
        return Ok(ApiResponse<object>.Ok(null, "Member removed successfully"));
    }

    [HttpGet("{id:guid}/labels")]
    public async Task<IActionResult> GetLabels(Guid id)
    {
        var labels = await _projectService.GetLabelsAsync(id);
        return Ok(ApiResponse<IEnumerable<DTOs.Projects.LabelDto>>.Ok(labels));
    }

    [HttpPost("{id:guid}/labels")]
    public async Task<IActionResult> CreateLabel(Guid id, [FromBody] CreateLabelRequest request)
    {
        var label = await _projectService.CreateLabelAsync(id, request);
        return Ok(ApiResponse<DTOs.Projects.LabelDto>.Ok(label, "Label created"));
    }

    [HttpPut("labels/{labelId:guid}")]
    public async Task<IActionResult> UpdateLabel(Guid labelId, [FromBody] UpdateLabelRequest request)
    {
        var label = await _projectService.UpdateLabelAsync(labelId, request);
        if (label == null)
            return NotFound(ApiResponse<object>.Fail("Label not found"));
        return Ok(ApiResponse<DTOs.Projects.LabelDto>.Ok(label, "Label updated"));
    }

    [HttpDelete("labels/{labelId:guid}")]
    public async Task<IActionResult> DeleteLabel(Guid labelId)
    {
        var result = await _projectService.DeleteLabelAsync(labelId);
        if (!result)
            return NotFound(ApiResponse<object>.Fail("Label not found"));
        return Ok(ApiResponse<object>.Ok(null, "Label deleted"));
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("User not authenticated"));
}
