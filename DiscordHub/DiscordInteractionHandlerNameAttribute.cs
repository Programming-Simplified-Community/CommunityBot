namespace DiscordHub;

[AttributeUsage(AttributeTargets.Class)]
public class DiscordInteractionHandlerNameAttribute : Attribute
{
    /// <summary>
    /// Name of interaction handler
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// CustomId prefix that associates an interaction with this interaction
    /// </summary>
    public string Prefix { get; }

    public DiscordInteractionHandlerNameAttribute(string name, string prefix)
    {
        Name = name;
        Prefix = prefix;
    }
}