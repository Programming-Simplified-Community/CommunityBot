using CodeJam.Services;
using Data.CodeJam;
using Discord;
using Discord.Interactions;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CodeJam.Commands.Autocomplete;

public class ActiveJamAutoCompleteProvider : AutocompleteHandler
{
    private static SocialDbContext? Database { get; set; }
    private static List<Topic> _topics = new();
    private static DateTime _nextUpdate = DateTime.Now.AddSeconds(-30);
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(30);

    protected virtual IQueryable<Topic> SearchTopics(string? text)
    {
        var now = DateTime.Now;

        if (string.IsNullOrEmpty(text))
            return _topics
                .Where(x => x.IsActive && now >= x.StartDateOn && now <= x.EndDateOn)
                .Take(25).AsQueryable();

        return _topics
            .Where(x => x.IsActive && now >= x.StartDateOn && now <= x.EndDateOn && x.Title.StartsWith(text))
            .Take(25)
            .AsQueryable();
    }

    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter, IServiceProvider services)
    {
        if (Database is null)
            Database = CodeJamBot.Provider.GetRequiredService<SocialDbContext>();
        
        var now = DateTime.Now;

        if (now >= _nextUpdate)
        {
            _topics = await Database.CodeJamTopics.ToListAsync();
            _nextUpdate = now + RefreshInterval;
        }

        var response = SearchTopics( autocompleteInteraction.Data.Current?.Value?.ToString())
            .Select(x => new AutocompleteResult(x.Title, x.Title.Trim()))
            .ToList();

        return AutocompletionResult.FromSuccess(response);
    }
}