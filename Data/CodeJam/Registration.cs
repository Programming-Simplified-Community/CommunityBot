namespace Data.CodeJam;

public class Registration
{
    public int Id { get; set; }

    /// <summary>
    /// Date/Time in which user registered
    /// </summary>
    public DateTime RegisteredOn { get; set; } = DateTime.Now;

    /// <summary>
    /// Date/Time in which reminder was sent
    /// </summary>
    public DateTime? ReminderSentOn { get; set; }

    /// <summary>
    /// Discord GUILD this user registered on
    /// </summary>
    public string DiscordGuildId { get; set; }

    /// <summary>
    /// This User ID should link to a <see cref="SocialUser"/>
    /// </summary>
    public string DiscordUserId { get; set; }

    /// <summary>
    /// User's level of experience
    /// </summary>
    public SigmaLevel ExperienceLevel { get; set; }

    /// <summary>
    /// Topic to participate in
    /// </summary>
    public int TopicId { get; set; }
    
    /// <summary>
    /// Timezone this user wants to participate in
    /// </summary>
    public int TimezoneId { get; set; }
    
    /// <summary>
    /// Team (if applicable) this user is on for this jam
    /// </summary>
    public int? TeamId { get; set; }
    
    /// <summary>
    /// Does this user want to participate as a solo developer, or in a team?
    /// </summary>
    public bool IsSolo { get; set; }
    
    /// <summary>
    /// Date/Time in which user confirmed their participation
    /// </summary>
    public DateTime? ConfirmedOn { get; set; }

    /// <summary>
    /// Confirmation value the user provided
    /// </summary>
    public bool? ConfirmationValue { get; set; }

    /// <summary>
    /// Date/Time in which a user abandoned their registration
    /// </summary>
    public DateTime? AbandonedOn { get; set; }
}