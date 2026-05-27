using System.ComponentModel.DataAnnotations.Schema;

namespace Task_Manager_Backend.Models;

[Table("TaskLabels")]
public class TaskLabel
{
    public Guid TaskId { get; set; }

    [ForeignKey(nameof(TaskId))]
    public TaskItem? Task { get; set; }

    public Guid LabelId { get; set; }

    [ForeignKey(nameof(LabelId))]
    public Label? Label { get; set; }
}
