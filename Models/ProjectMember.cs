using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Task_Manager_Backend.Models;

[Table("ProjectMembers")]
public class ProjectMember
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public Project? Project { get; set; }

    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [MaxLength(20)]
    public string Role { get; set; } = "member";

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
