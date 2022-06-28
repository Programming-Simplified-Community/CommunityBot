using CodeJam.Services;
using Data.CodeJam;
using Discord;
using Discord.Interactions;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CodeJam.Commands.Autocomplete;

public class TimezoneAutoCompleteProvider : AutocompleteHandler
{
    private static SocialDbContext? _database;
    private static List<Timezone> _timezones = new();
    private static TimeSpan _refreshInterval = TimeSpan.FromMinutes(1);
    private static DateTime _nextUpdate = DateTime.Now.AddSeconds(-30);

    private IQueryable<Timezone> Search(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return _timezones.Take(25).AsQueryable();

        return _timezones.Where(x => x.Name.StartsWith(text, StringComparison.OrdinalIgnoreCase)).Take(25)
            .AsQueryable();
    }
    
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter, IServiceProvider services)
    {
        if (_database is null)
            _database = CodeJamBot.Provider.GetRequiredService<SocialDbContext>();

        if (DateTime.Now >= _nextUpdate)
        {
            _timezones = await _database.CodeJamTimezones.ToListAsync();
            _nextUpdate = DateTime.Now + _refreshInterval;
        }

        var response= Search(autocompleteInteraction.Data.Current?.Value?.ToString())
            .Select(x => new AutocompleteResult(x.Name, x.Name.Trim()))
            .ToList();
        return AutocompletionResult.FromSuccess(response);
    }
}