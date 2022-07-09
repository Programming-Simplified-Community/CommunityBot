using Data.CodeJam;

namespace CodeJam.Commands.Autocomplete;

/// <summary>
/// A moderate shall be able to see all jams
/// </summary>
public class ModeratorJamTopicAutoCompleteProvider : RegisterableJamAutoCompleteProvider
{
    protected override IQueryable<Topic> SearchTopics(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return Topics.Take(25).AsQueryable();

        return Topics.Where(x => x.Title.StartsWith(text, StringComparison.OrdinalIgnoreCase))
            .Take(25)
            .AsQueryable();
    }
}