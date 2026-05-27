namespace Task_Manager_Backend.DTOs.Projects;

public record ProjectSummary(
    Guid Id,
    string Name,
    string? Description,
    string Status,
    string OwnerName,
    string? OwnerAvatar,
    int MemberCount,
    int TotalTasks,
    int CompletedTasks,
    double ProgressPercent,
    DateTime CreatedAt
);

public record ProjectDetail(
    Guid Id,
    string Name,
    string? Description,
    string Status,
    Guid OwnerId,
    string OwnerName,
    int MemberCount,
    IEnumerable<ProjectMemberDto> Members,
    IEnumerable<TaskSummary> Tasks,
    IEnumerable<LabelDto> Labels,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record ProjectMemberDto(
    Guid Id,
    Guid UserId,
    string Name,
    string Email,
    string Role,
    string? Avatar
);

public record CreateProjectRequest(string Name, string? Description, string? Status);

public record UpdateProjectRequest(string? Name, string? Description, string? Status);

public record TaskSummary(
    Guid Id,
    string Title,
    string Status,
    string Priority,
    string? AssigneeName,
    DateTime? DueDate
);

public record LabelDto(Guid Id, string Name, string Color);

public record CreateLabelRequest(string Name, string Color);

public record UpdateLabelRequest(string? Name, string? Color);

public record AddMemberRequest(Guid UserId, string? Role);
