using System.ComponentModel.DataAnnotations;

namespace Data.CodeJam;

public class Topic : IEntityWithTypedId<int>
{
    /// <summary>
    /// Primary key
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Date/Time in which users can start registering
    /// </summary>
    [Display(Name = "Registration Start")]
    public DateTime RegistrationStartOn { get; set; }
    
    /// <summary>
    /// Date/Time in which users will no longer be able to register
    /// </summary>
    [Display(Name="Registration Closed")]
    public DateTime RegistrationEndOn { get; set; }

    /// <summary>
    /// Date/Time in which users start the jam, and when users can start submitting
    /// </summary>
    [Display(Name="Start Date")]
    public DateTime StartDateOn { get; set; }
    
    /// <summary>
    /// Date/Time in which submissions close
    /// </summary>
    [Display(Name="End Date")]
    public DateTime EndDateOn { get; set; }

    public string Title { get; set; }
    public string Description { get; set; }
    public string Requirements { get; set; }

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// <see cref="PointType"/> record to use for people who completed a jam (but didn't win)
    /// </summary>
    public int? PointType_CompletionId { get; set; }
    
    /// <summary>
    /// <see cref="PointType"/> record to use when people abandon a jam
    /// </summary>
    public int? PointType_WithdrawId { get; set; }
    
    /// <summary>
    /// <see cref="PointType"/> record to use when applying points to people who won the code jam
    /// </summary>
    public int? PointType_WinnerId { get; set; }
}