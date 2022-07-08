using Data;
using Data.Challenges;
using Data.CodeJam;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public class SocialDbContext : IdentityDbContext<SocialUser>
{
    public SocialDbContext(DbContextOptions options) : base(options)
    {
        
    }

    public DbSet<PointType> CodeJamPointTypes { get; set; }
    public DbSet<Registration> CodeJamRegistrations { get; set; }
    public DbSet<Requirement> CodeJamRequirements { get; set; }
    public DbSet<Submission> CodeJamSubmissions { get; set; }
    public DbSet<Team> CodeJamTeams { get; set; }
    public DbSet<TeamMember> CodeJamTeamMembers { get; set; }
    public DbSet<Timezone> CodeJamTimezones { get; set; }
    public DbSet<Topic> CodeJamTopics { get; set; }
    public DbSet<ProgrammingChallengeReport> ChallengeReports { get; set; }
    public DbSet<ProgrammingTestResult> TestResults { get; set; }

    public DbSet<ProgrammingChallenge> ProgrammingChallenges { get; set; }
    public DbSet<ProgrammingTest> ProgrammingTests { get; set; }
    public DbSet<ProgrammingChallengeSubmission> ProgrammingChallengeSubmissions { get; set; }
}