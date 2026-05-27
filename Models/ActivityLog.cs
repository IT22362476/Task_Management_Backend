using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Task_Manager_Backend.Models;

[Table("ActivityLogs")]
public class ActivityLog
{
    [Key]
    public Guid Id { get; set; }

    [Required, MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public Guid? TaskId { get; set; }

    [ForeignKey(nameof(TaskId))]
    public TaskItem? Task { get; set; }

    public Guid? ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public Project? Project { get; set; }

    public Guid ActorId { get; set; }

    [ForeignKey(nameof(ActorId))]
    public User? Actor { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
