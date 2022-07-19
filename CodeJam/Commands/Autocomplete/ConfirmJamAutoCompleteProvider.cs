using CodeJam.Services;
using Data.CodeJam;
using Discord;
using Discord.Interactions;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CodeJam.Commands.Autocomplete;

/// <summary>
/// Provide a user with a list of jams that they have not yet confirmed (and are still accepting registrations)
/// </summary>
public class ConfirmJamAutoCompleteProvider : AutocompleteHandler
{
    protected static SocialDbContext Database;

    protected IQueryable<Topic> SearchTopics(string? text, string memberId, string guildId)
    {
        var now = DateTime.UtcNow;

        var queryable = (from req in Database.CodeJamRegistrations
                join topic in Database.CodeJamTopics
                    on req.TopicId equals topic.Id
                where req.DiscordUserId == memberId &&
                      req.DiscordGuildId == guildId &&
                      now >= topic.RegistrationStartOn &&
                      now <= topic.RegistrationEndOn &&
                      req.AbandonedOn == null &&
                      req.ConfirmedOn == null
                select topic)
            .OrderBy(x => x.Title)
            .AsQueryable();

        if (string.IsNullOrEmpty(text))
            return queryable.Take(25);

        return queryable.Where(x => x.Title.StartsWith(text))
            .OrderBy(x => x.Title)
            .Take(25)
            .AsQueryable();
    }
    
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter, IServiceProvider services)
    {
        if (Database is null)
            Database = CodeJamBot.Provider.GetRequiredService<SocialDbContext>();

        return AutocompletionResult.FromSuccess(await SearchTopics(autocompleteInteraction.Data.Current?.Value?.ToString(), context.User.Id.ToString(),
                context.Guild.Id.ToString())
            .Select(x => new AutocompleteResult(x.Title, x.Title.Trim()))
            .ToListAsync());
    }
}