using CodeJam.Services;
using Data.CodeJam;
using Discord;
using Discord.Interactions;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CodeJam.Commands.Autocomplete;

/// <summary>
/// Provide a user with a list of jams that they can register for
/// </summary>
public class RegisterableJamAutoCompleteProvider : AutocompleteHandler
{
    protected static SocialDbContext? Database;
    protected static TimeSpan RefreshInterval = TimeSpan.FromSeconds(30);
    protected static List<Topic> Topics = new();
    protected static DateTime NextUpdate = DateTime.Now.AddSeconds(-30);
    
    protected virtual IQueryable<Topic> SearchTopics(string? text)
    {
        var now = DateTime.Now;

        if (string.IsNullOrEmpty(text))
            return Topics.Where(x => x.IsActive && now >= x.RegistrationStartOn && now <= x.RegistrationEndOn)
                .Take(25)
                .AsQueryable();

        return Topics.Where(x => x.IsActive &&
                                  now >= x.RegistrationStartOn &&
                                  now <= x.RegistrationEndOn &&
                                  x.Title.StartsWith(text, StringComparison.OrdinalIgnoreCase))
            .Take(25)
            .AsQueryable();
    }

    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter, IServiceProvider services)
    {
        if (Database is null)
            Database = CodeJamBot.Provider.GetRequiredService<SocialDbContext>();

        var now = DateTime.Now;
        if (now >= NextUpdate)
        {
            Topics = await Database.CodeJamTopics.ToListAsync();
            NextUpdate = now + RefreshInterval;
        }

        var response = SearchTopics(autocompleteInteraction.Data.Current?.Value.ToString())
            .Select(x => new AutocompleteResult(x.Title, x.Title.Trim()))
            .ToList();
        return AutocompletionResult.FromSuccess(response);
    }
}