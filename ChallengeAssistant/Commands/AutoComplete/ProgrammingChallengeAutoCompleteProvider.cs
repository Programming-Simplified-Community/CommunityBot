using Data.Challenges;
using Discord;
using Discord.Interactions;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ChallengeAssistant.Commands.AutoComplete;

public class ProgrammingChallengeAutoCompleteProvider : AutocompleteHandler
{
    protected static SocialDbContext Database;

    protected IQueryable<ProgrammingChallenge> Search(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return Database.ProgrammingChallenges.AsQueryable();

        return Database.ProgrammingChallenges.Where(x =>
            x.Title.StartsWith(text, StringComparison.InvariantCultureIgnoreCase));
    }
    
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter, IServiceProvider services)
    {
        if (Database is null)
            Database = services.GetRequiredService<SocialDbContext>();

        var response = await Search(autocompleteInteraction.Data.Current.Value?.ToString())
            .Select(x => new AutocompleteResult(x.Title, x.Title.Trim()))
            .ToListAsync();

        return AutocompletionResult.FromSuccess(response);
    }
}