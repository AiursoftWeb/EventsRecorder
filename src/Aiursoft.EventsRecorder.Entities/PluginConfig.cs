using System.ComponentModel.DataAnnotations;

namespace Aiursoft.EventsRecorder.Entities;

public class PluginConfig
{
    public int Id { get; set; }

    [Required]
    public required string UserId { get; set; }

    public User? User { get; set; }

    [Required]
    [MaxLength(100)]
    public required string PluginId { get; set; }

    public string ConfigJson { get; set; } = "{}";
}
