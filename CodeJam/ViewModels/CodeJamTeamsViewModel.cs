using Data.CodeJam;

namespace CodeJam.ViewModels;

public record UserInfo(string Username, string DiscordAvatarUrl);
public record TeamView(string TeamName, Submission? Submission, Dictionary<string, string> Members);
