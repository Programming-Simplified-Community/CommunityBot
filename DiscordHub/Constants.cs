namespace DiscordHub;

public static class Constants
{
    public const string CHALLENGE_BUTTON_NAME_FORMAT = $"{CHALLENGE_BUTTON_PREFIX}_{{0}}";
    public const string CHALLENGE_BUTTON_PREFIX = "challenge";

    public const string CHALLENGE_MODAL_PREFIX = "mchallenge";
    public const string CHALLENGE_MODAL_NAME_FORMAT = $"{CHALLENGE_MODAL_PREFIX}_{{0}}_{{1}}";

    public const string JOIN_CODE_JAM_BUTTON_PREFIX = "cjj";
    public const string JOIN_CODE_JAM_BUTTON_NAME_FORMAT = $"{JOIN_CODE_JAM_BUTTON_PREFIX}_{{0}}";

    public const string NO_THANKS_JAM_BUTTON_PREFIX = "cji";
    public const string NO_THANKS_JAM_BUTTON_NAME_FORMAT = $"{NO_THANKS_JAM_BUTTON_PREFIX}_{{0}}";

    public const string ATTEMPT_BUTTON_PREFIX = "attempt";
    public const string ATTEMPT_BUTTON_NAME_FORMAT = $"{ATTEMPT_BUTTON_PREFIX}_{{0}}_{{1}}";

    public const string TEAM_NAME_VOTE_YES_BUTTON_PREFIX = "tnvy";
    public const string TEAM_NAME_VOTE_NO_BUTTON_PREFIX = "tnvn";

    /// <summary>
    /// <p>String format</p>
    /// <list type="number">
    /// <item>TeamId</item>
    /// <item>TeamNameVoteId</item>
    /// </list>
    /// </summary>
    public const string TEAM_NAME_VOTE_YES_BUTTON_NAME_FORMAT = $"{TEAM_NAME_VOTE_YES_BUTTON_PREFIX}_{{0}}_{{1}}";

    /// <summary>
    /// <p>String format</p>
    /// <list type="number">
    /// <item>Team Id</item>
    /// <item>TeamNameVoteId</item>
    /// </list>
    /// </summary>
    public const string TEAM_NAME_VOTE_NO_BUTTON_NAME_FORMAT = $"{TEAM_NAME_VOTE_NO_BUTTON_PREFIX}_{{0}}_{{1}}";
}