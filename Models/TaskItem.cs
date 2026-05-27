using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Task_Manager_Backend.Models;

[Table("Tasks")]
public class TaskItem
{
    [Key]
    public Guid Id { get; set; }

    [Required, MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "todo";

    [MaxLength(20)]
    public string Priority { get; set; } = "medium";

    public Guid ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public Project? Project { get; set; }

    public Guid? AssigneeId { get; set; }

    [ForeignKey(nameof(AssigneeId))]
    public User? Assignee { get; set; }

    public Guid CreatedById { get; set; }

    [ForeignKey(nameof(CreatedById))]
    public User? CreatedBy { get; set; }

    public DateTime? DueDate { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public ICollection<TaskLabel> TaskLabels { get; set; } = new List<TaskLabel>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
