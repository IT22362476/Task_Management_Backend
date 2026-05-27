using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Task_Manager_Backend.Models;

[Table("Users")]
public class User
{
    [Key]
    public Guid Id { get; set; }

    [Required, MaxLength(150)]
    public string FullName { get; set; } = null!;

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = null!;

    public string? PasswordHash { get; set; }

    [Required, MaxLength(20)]
    public string Role { get; set; } = "Employee";

    [MaxLength(100)]
    public string? Department { get; set; }

    [Required, MaxLength(20)]
    public string Provider { get; set; } = "Local";

    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
