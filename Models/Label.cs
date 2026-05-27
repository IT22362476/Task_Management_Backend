using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Task_Manager_Backend.Models;

[Table("Labels")]
public class Label
{
    [Key]
    public Guid Id { get; set; }

    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Color { get; set; } = "#3b82f6";

    public Guid ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public Project? Project { get; set; }

    public ICollection<TaskLabel> TaskLabels { get; set; } = new List<TaskLabel>();
}
