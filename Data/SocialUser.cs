using Microsoft.AspNetCore.Identity;

namespace Data;

public class SocialUser : IdentityUser
{
    public string? DiscordUserId { get; set; }
    public string? DiscordDisplayName { get; set; }
    public int Points { get; set; } = 0;
}