using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ChallengeAssistant;

internal static class JsonExtensions
{
    private static readonly JsonSerializerSettings Settings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };
    
    /// <summary>
    /// Convert <paramref name="obj"/> into Json
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static string ToJson(this object obj)
        => JsonConvert.SerializeObject(obj, Settings);

    /// <summary>
    /// Convert <typeparamref name="T"/> from Json
    /// </summary>
    /// <param name="json"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T? FromJson<T>(this string json)
        => JsonConvert.DeserializeObject<T>(json, Settings);
}