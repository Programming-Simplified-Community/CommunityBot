using Discord;
using Discord.Interactions;

namespace CodeJam.Commands.Autocomplete;

/// <summary>
/// Provide a user with a list of options, easy selection of experience levels
/// </summary>
public class ExperienceAutoCompleteProvider : AutocompleteHandler
{
    private readonly string[] _options =
    {
        "White Belt (0-1 years)",
        "Yellow Belt (2-3 years)",
        "Green Belt (4-5 years)",
        "Blue Belt (6-7 years)",
        "Red Belt (8-9 years)",
        "Black Belt (10+ years)"
    };

    IQueryable<string> Search(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return _options.AsQueryable();

        return _options.Where(x => x.StartsWith(text, StringComparison.OrdinalIgnoreCase))
            .AsQueryable();
    }
    
    public override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter, IServiceProvider services)
    {
        return Task.FromResult(AutocompletionResult.FromSuccess(
            Search(autocompleteInteraction.Data.Current?.Value.ToString())
            .ToList()
            .Select(x => new AutocompleteResult(x, x.Trim()))));
    }
}