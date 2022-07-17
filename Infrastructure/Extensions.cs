using Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public static class Extensions
{
    /// <summary>
    /// Ensures the user is in the database, will auto populate user if not there
    /// </summary>
    /// <param name="context"></param>
    /// <param name="username"></param>
    /// <param name="discordId"></param>
    /// <returns></returns>
    public static async ValueTask<SocialUser> GetOrAddUser(this SocialDbContext context, string username, string discordId)
    {
        var existingUser = await context.Users.FirstOrDefaultAsync(x => x.DiscordUserId == discordId);

        if (existingUser is not null)
            return existingUser;

        existingUser = new()
        {
            Email = "changeme@email.com",
            UserName = username,
            DiscordDisplayName = username,
            NormalizedUserName = username.ToUpper(),
            DiscordUserId = discordId
        };

        context.Users.Add(existingUser);
        await context.SaveChangesAsync();
        
        return existingUser;
    }

    public static async ValueTask<SocialUser?> GetUserFromDiscordId(this SocialDbContext context, string discordId)
    {
        return await context.Users.FirstOrDefaultAsync(x => x.DiscordUserId == discordId);
    }
}