using Data.Challenges;
using Infrastructure;

namespace ChallengeAssistant;

public static class SeedDb
{
    public static async Task Seed(SocialDbContext context)
    {
        if (!context.ProgrammingChallenges.Any())
        {
            var question = @"This scenario tries to validate some of your OOP skills. You must create a class called `Person` with the following requirements.
```md
Must have a constructor that accepts:
1. first
2. last
3. age

Must have the following attributes:
1. 'first' - represents a person's first name
2. 'last' - represents a person's last name
3. 'age' - represents the person's age

Must have the following property:
1. `name` - a property which is a combination of 'first' and 'last' attributes. ""FIRST LAST"" - notice the space in between.

If the object is printed, it should output in the following format:

First: PersonsFirstNameGoesHere
Last: PersonsLastNameGoesHere
Age: PersonsAgeGoesHere
 
Lastly, must have the following method:

`greeting_message` - which returns the following format:
""Hello! my name is FIRSTNAME LASTNAME and I'm AGE years old""
```";
            var personClass = new ProgrammingChallenge
            {
                Question = question,
                Title = "OOP - Person Class",
                Explanation = ""
            };

            context.ProgrammingChallenges.Add(personClass);
            await context.SaveChangesAsync();
            
            var personTest = new ProgrammingTest
            {
                TestDockerImage = @"ghcr.io/jbraunsmajr/pythonsimplifiedtestsrepo:release",
                ProgrammingChallengeId = personClass.Id,
                ExecutableFileMountDestination = "/app/main.py",
                Language = ProgrammingLanguage.Python,
                DockerEntryPoint = @"pytest --json-report --json-report-file /app/reports/report.json -v tests/oop-class-str.py"
            };

            context.ProgrammingTests.Add(personTest);
        }

        await context.SaveChangesAsync();
    }
}