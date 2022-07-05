using Data;
using Data.CodeJam;
using Discord;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CodeJam;

public enum MessageType
{
    Info,
    Warning,
    Success,
    Pending,
    Error
}

public static class Util
{
    public static async Task InitializeDb(SocialDbContext context)
    {
        if (!context.CodeJamTopics.Any())
        {
            Console.WriteLine("Creating topic");
            var topic = new Topic
            {
                Title = "Sound & Audio",
                Description = "Find a creative way to integrate sound/audio into an application!",
                Requirements = "Your repository must be public",
                
                StartDateOn = new(2022, 7, 1, 0, 0, 0),
                EndDateOn = new(2022, 7, 31, 23,59,59),
                
                RegistrationStartOn = new(2022,06,28,0,0,0),
                RegistrationEndOn = new(2022, 07, 04, 23,59,59)
            };
            
            context.CodeJamTopics.Add(topic);
            await context.SaveChangesAsync();
        }

        if (!context.CodeJamRequirements.Any())
        {
            var id = ((await context.CodeJamTopics.OrderBy(x=>x.Id).FirstOrDefaultAsync())!).Id;
            context.CodeJamRequirements.Add(new()
            {
                TopicId = id,
                Information = "Your project must creatively integrate sound & audio!",
                AcceptanceCriteria = @"1. Your project must be publicly available on GitHub\n
2. All members of your team need to participate! We do not want to see 99% of the work be done by 1 person.
3. Your project must be original."
            });
            await context.SaveChangesAsync();
        }

        if (!context.CodeJamTimezones.Any())
        {
            string[] zones =
            {
                "North/South America",
                "Europe",
                "Asia"
            };

            foreach (var zone in zones)
                context.CodeJamTimezones.Add(new()
                {
                    Name = zone
                });
            await context.SaveChangesAsync();
        }

        #if DEBUG
        if (!context.Users.Any())
        {
            var badgerId = Guid.NewGuid().ToString();
            var user = new SocialUser
            {
                Id = badgerId,
                DiscordUserId = "117744034247737353",
                DiscordDisplayName = "Badger 2-3",
                UserName = "Badger 2-3",
                NormalizedUserName = "BADGER 2-3",
                Email = "changeme",
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var topicId = (await context.CodeJamTopics.OrderBy(x=>x.Id).FirstOrDefaultAsync()).Id;
            var timezoneId = (await context.CodeJamTimezones.OrderBy(x=>x.Id).FirstOrDefaultAsync()).Id;

            context.CodeJamRegistrations.Add(new Registration
            {
                ExperienceLevel = SigmaLevel.Black,
                IsSolo = false,
                RegisteredOn = DateTime.Now.AddDays(-3),
                TimezoneId = timezoneId,
                TopicId = topicId,
                DiscordUserId = "117744034247737353",
                DiscordGuildId = "989689420695367690",
                //ConfirmationValue = true,
                //ConfirmedOn = DateTime.Now.AddDays(-1)
            });
            await context.SaveChangesAsync();
        }
        #endif

    }
    
    static Color GetColor(MessageType type)
    {
        return type switch
        {
            MessageType.Error => Color.Red,
            MessageType.Info => Color.Blue,
            MessageType.Success => Color.Green,
            MessageType.Warning => Color.Orange,
            MessageType.Pending => Color.Purple,
            _ => Color.LightGrey
        };
    }

    public static EmbedBuilder Embed(string title, string description, MessageType type = MessageType.Pending,
        string author = "Code Jam Helper")
    {
        return new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(description)
            .WithColor(GetColor(type))
            .WithAuthor(author);
    }

    public static SigmaLevel GetExperienceLevel(string text)
    {
        return text.ToLower().Split(' ').First() switch
        {
            "white" => SigmaLevel.White,
            "yellow" => SigmaLevel.Yellow,
            "green" => SigmaLevel.Green,
            "blue" => SigmaLevel.Blue,
            "black" => SigmaLevel.Black,
            _ => SigmaLevel.White
        };
    }
}