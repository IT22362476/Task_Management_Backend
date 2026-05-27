namespace Task_Manager_Backend.DTOs.Team;

public record TeamMemberDto(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    string? Department,
    int TasksAssigned,
    int TasksCompleted,
    DateTime JoinDate
);

public record TeamStatsDto(
    int TotalMembers,
    int Admins,
    int Employees
);
