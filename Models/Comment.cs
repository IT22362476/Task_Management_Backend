using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Task_Manager_Backend.Models;

[Table("Comments")]
public class Comment
{
    [Key]
    public Guid Id { get; set; }

    [Required, MaxLength(5000)]
    public string Content { get; set; } = string.Empty;

    public Guid TaskId { get; set; }

    [ForeignKey(nameof(TaskId))]
    public TaskItem? Task { get; set; }

    public Guid AuthorId { get; set; }

    [ForeignKey(nameof(AuthorId))]
    public User? Author { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
