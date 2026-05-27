namespace Task_Manager_Backend.DTOs.Tasks;

public record TaskDetail(
    Guid Id,
    string Title,
    string? Description,
    string Status,
    string Priority,
    Guid ProjectId,
    string ProjectName,
    Guid? AssigneeId,
    string? AssigneeName,
    string? AssigneeAvatar,
    Guid CreatedById,
    string CreatedByName,
    IEnumerable<LabelDto> Labels,
    IEnumerable<CommentDto> Comments,
    DateTime? DueDate,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? CompletedAt
);

public record TaskSummary(
    Guid Id,
    string Title,
    string? Description,
    string Status,
    string Priority,
    string ProjectName,
    string? AssigneeName,
    IEnumerable<LabelDto> Labels,
    DateTime? DueDate,
    DateTime CreatedAt
);

public record CreateTaskRequest(
    string Title,
    string? Description,
    string Priority,
    string ProjectId,
    string? AssigneeId,
    DateTime? DueDate,
    IEnumerable<string>? Labels
);

public record UpdateTaskRequest(
    string? Title,
    string? Description,
    string? Status,
    string? Priority,
    string? AssigneeId,
    DateTime? DueDate,
    IEnumerable<string>? Labels
);

public record LabelDto(Guid Id, string Name, string Color);

public record CommentDto(
    Guid Id,
    string Content,
    Guid AuthorId,
    string AuthorName,
    string? AuthorAvatar,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record AddCommentRequest(string Content);
