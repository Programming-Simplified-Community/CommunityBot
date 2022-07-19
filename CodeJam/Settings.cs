using Discord;

namespace CodeJam;

public sealed class Settings
{
    private OverwritePermissions? _generalCache;
    private GuildPermissions? _guildPerms;
    
    /// <summary>
    /// Server in which we are processing requests for.
    /// </summary>
    public ulong PrimaryGuildId { get; init; }
    
    /// <summary>
    /// Channel in which welcome messages will be put in. These
    /// messages during a registration period will contain components for
    /// users to interact with
    /// </summary>
    public ulong WelcomeChannelId { get; init; }
    
    /// <summary>
    /// Role ID associated to code-jam. This will be applied
    /// to users who want to join the jam
    /// </summary>
    public ulong CodeJamRoleId { get; init; }
    
    /// <summary>
    /// When a user joins the code-jam they will be redirect to this channel
    /// </summary>
    public ulong CodeJamGeneralId { get; init; }

    public GuildPermissions GuildPermissions
    {
        get
        {
            if (_guildPerms.HasValue)
                return _guildPerms.Value;

            _guildPerms = new GuildPermissions(
                false,
                false,
                false,
                false,
                false,
                false,
                true,
                false,
                false,
                true,
                true,
                true,
                false,
                true,
                true,
                true,
                false,
                true,
                true,
                true,
                false,
                false,
                false,
                true,
                false,
                true,
                false,
                false,
                false,
                false,
                false,
                true,
                true);
            
            return _guildPerms.Value;
        }
    }
    
    public OverwritePermissions GeneralPermissions
    {
        get
        {
            if (_generalCache.HasValue)
                return _generalCache.Value;

            _generalCache = new OverwritePermissions(PermValue.Deny, PermValue.Deny,
                PermValue.Allow,
                PermValue.Allow,
                PermValue.Allow,
                PermValue.Allow,
                PermValue.Allow,
                PermValue.Allow,
                PermValue.Allow,
                PermValue.Allow,
                PermValue.Deny,
                PermValue.Allow,
                PermValue.Allow,
                PermValue.Allow,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Allow,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Deny,
                PermValue.Allow,
                PermValue.Allow,
                PermValue.Allow,
                PermValue.Allow,
                PermValue.Deny,
                PermValue.Allow,
                PermValue.Deny,
                PermValue.Allow,
                PermValue.Allow,
                PermValue.Allow,
                PermValue.Allow,
                PermValue.Allow);
            
            return _generalCache.Value;
        }
    }
    public string CodeJamRoleName { get; init; }
    public string CodeJamCategoryName { get; init; }
}