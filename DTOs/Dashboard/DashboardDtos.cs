namespace Task_Manager_Backend.DTOs.Dashboard;

public record AdminDashboardDto(
    TaskStatsDto TaskStats,
    IEnumerable<ProjectProgressDto> ProjectProgress,
    IEnumerable<ActivityDto> RecentActivity,
    IEnumerable<DeadlineDto> UpcomingDeadlines
);

public record EmployeeDashboardDto(
    TaskStatsDto TaskStats,
    IEnumerable<ActivityDto> RecentActivity,
    IEnumerable<DeadlineDto> UpcomingDeadlines,
    double CompletionRate
);

public record TaskStatsDto(
    int Total,
    int Todo,
    int InProgress,
    int Review,
    int Completed,
    int Overdue
);

public record ProjectProgressDto(
    Guid Id,
    string Name,
    double Percent
);

public record ActivityDto(
    Guid Id,
    string Action,
    string? Description,
    string ActorName,
    DateTime CreatedAt
);

public record DeadlineDto(
    Guid TaskId,
    string Title,
    string ProjectName,
    DateTime? DueDate,
    int DaysRemaining
);
