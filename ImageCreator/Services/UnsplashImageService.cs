using HtmlAgilityPack;

namespace ImageCreator.Services;
using Interfaces;

record CachedResult(List<HtmlNode> Nodes, DateTime DeleteAfter);

public class UnsplashImageService : IImageService
{
    private readonly Random _random = new();
    private readonly Dictionary<string, CachedResult> _cache = new();

    public string BaseUrl => "https://unsplash.com";

    async Task<List<HtmlNode>> Query(string query)
    {
        if (_cache.ContainsKey(query))
        {
            var result = _cache[query];
            
            // ensure we used up-to-date images from cache
            if (result.DeleteAfter >= DateTime.UtcNow)
                return result.Nodes;

            _cache.Remove(query);
        }

        var searchUri = string.Concat(BaseUrl, "/s/photos/", query);
        HtmlWeb web = new();
        var doc = await web.LoadFromWebAsync(searchUri);
        
        // grab all the links with the photo url
        var images = doc.DocumentNode.SelectNodes("//a[@itemprop='contentUrl']")
            .Where(x => x.Attributes["href"].Value.StartsWith("/photos"))
            .ToList();

        if (_cache.ContainsKey(query))
            _cache[query].Nodes.AddRange(images);
        else
            _cache.Add(query, new(images, DateTime.UtcNow.AddDays(1)));

        return images;
    }

    public async Task<string?> RandomImageUrl(string query)
    {
        var images = await Query(query);

        var index = _random.Next(0, images.Count);
        var photoUrl = $"{BaseUrl}{images[index].Attributes["href"].Value}";

        HtmlWeb web = new();
        var photoPage = await web.LoadFromWebAsync(photoUrl);

        var items = photoPage.DocumentNode.SelectNodes("//img").ToList();

        return items.Count > 1 ? items[2].Attributes["src"].Value : null;
    }
}