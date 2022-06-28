using Data;
using Data.CodeJam;

namespace CodeJam.Interfaces;

public record UserRegistrationRecord(SocialUser User, Registration Registration);

public interface ITeamCreationService
{
    Task<Dictionary<Timezone, List<UserRegistrationRecord>>> GetRegisteredUsersByTimezone(int topicId);

    Task<bool> CalculateTeamsFor(Topic topic, Timezone timezone, List<UserRegistrationRecord> userPool);
    Task<bool> GenerateTeams();
}