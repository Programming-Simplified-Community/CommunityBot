using CodeJam.Services;
using Data.CodeJam;
using Discord;
using Discord.Interactions;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CodeJam.Commands.Autocomplete;

/// <summary>
/// Provide a user with a list of active jams (that they're in) in which they can abandon
/// </summary>
public class AbandonJamAutoCompleteProvider : AutocompleteHandler
{
    protected static SocialDbContext Database;
    protected IQueryable<Topic> SearchTopics(string? text, string memberId, string guildId)
    {
        var now = DateTime.UtcNow;
        
        var queryable = (from req in Database.CodeJamRegistrations
                join topic in Database.CodeJamTopics
                    on req.TopicId equals topic.Id
                where
                    req.DiscordUserId == memberId &&
                    req.DiscordGuildId == guildId &&
                    // Should allow users to abandon anytime between registration start to jam end (not just during registration period)
                    now >= topic.RegistrationStartOn &&
                    now <= topic.EndDateOn
                select topic)
            .OrderBy(x => x.Title).Take(25).AsQueryable();

        return string.IsNullOrEmpty(text) ? queryable : queryable.Where(x => x.Title.StartsWith(text)).OrderBy(x => x.Title).Take(25).AsQueryable();
    }

    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter, IServiceProvider services)
    {
        if (Database is null)
            Database = CodeJamBot.Provider.GetRequiredService<SocialDbContext>();
        
        var response = await SearchTopics(autocompleteInteraction.Data.Current.Value?.ToString(),
                context.User.Id.ToString(),
                context.Guild.Id.ToString())
            .Select(x => new AutocompleteResult(x.Title, x.Title.Trim()))
            .ToListAsync();

        return AutocompletionResult.FromSuccess(response);
    }
}