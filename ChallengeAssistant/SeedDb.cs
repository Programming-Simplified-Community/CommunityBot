using Infrastructure;

namespace ChallengeAssistant;

public static class SeedDb
{
    public static async Task Seed(SocialDbContext context)
    {
        await context.SaveChangesAsync();
    }
}