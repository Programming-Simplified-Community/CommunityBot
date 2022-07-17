using Discord;

namespace CodeJam;

public sealed class Settings
{
    private OverwritePermissions? _generalCache;
    private GuildPermissions? _guildPerms;

    public ulong PrimaryGuildId { get; init; }
    public ulong WelcomeChannelId { get; init; }
    public ulong CodeJamRoleId { get; init; }
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