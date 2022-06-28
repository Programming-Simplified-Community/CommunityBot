using Core.Caching;
using Discord;

namespace CodeJam;

public static class Cache
{
    static async Task<Dictionary<ulong, IGuildChannel>> ChannelCacheUpdate(IGuild guild)
    {
        var channels = await guild.GetChannelsAsync();
        return channels.ToDictionary(x => x.Id, x => x);
    }

    static Task<Dictionary<ulong, IRole>> RoleCacheUpdate(IGuild guild)
    {
        return Task.FromResult(guild.Roles.ToDictionary(x => x.Id, x => x));
    }
    
    public static CacheItem<ulong,ulong, IGuildChannel, IGuild> ChannelCache = new (ChannelCacheUpdate);
    public static CacheItem<ulong, ulong, IRole, IGuild> RoleCache = new(RoleCacheUpdate);

    public static async Task<List<IRole>> ModeratorCache(IGuild guild)
    {
        var allRoles = await RoleCache.GetDict(guild.Id, guild);

        return allRoles.Values
            .Where(x => x.Name.Contains("moderator", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}