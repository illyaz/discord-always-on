using System.ComponentModel.DataAnnotations;
using DiscordAlwaysOn.Payloads;

namespace DiscordAlwaysOn;

public class AlwaysOnOptions
{
    [Required] public string Token { get; set; } = default!;
    public string? Activities { get; set; }
    public PresenceStatus Status { get; set; }
    public bool Afk { get; set; }
}