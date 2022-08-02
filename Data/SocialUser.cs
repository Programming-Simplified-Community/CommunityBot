using Microsoft.AspNetCore.Identity;

namespace Data;

public class SocialUser : IdentityUser, IEntityWithTypedId<string>
{
    public string? DiscordUserId { get; set; }
    public string? DiscordDisplayName { get; set; }
    public int Points { get; set; } = 0;
}