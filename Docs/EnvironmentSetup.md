# IDE

This is ultimately developer preference. These are the suggested IDEs 

**Free**

[Visual Studio Code](https://code.visualstudio.com/download)

[Visual Studio 2022](https://visualstudio.microsoft.com/vs/)

**Paid or free with student email**

[JetBrains Rider](https://www.jetbrains.com/rider/download/#section=windows)

-----

# Framework
We're utilizing C# Net 6.0 right now
[Net 6.0](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)

We're utilizing Discord.NET as our bot library

[Discord.Net Doc Site](https://discordnet.dev/)

[Discord.NET Github](https://github.com/discord-net/Discord.Net)

# Docker
If you are testing, or adding to the Challenge Assistant service Docker **IS REQUIRED**. 
[Docker](https://docs.docker.com/get-docker/)

Right now, **MySql** is used for our database. Which docker takes care of. If you aren't using docker, you will need to grab MySQL from their website: 
[Download](https://dev.mysql.com/downloads/mysql/)

Your connection string can be updated using the steps [here](settings.md), except the keyname would be `ConnectionStrings:Default`. You can reference `appsettings.json` to see how to format the connection string. Please reference the [database](database.md) doc on more details for db setup.

# Misc
Make sure you have [Git](https://git-scm.com/downloads) installed on your machine!

# Discord Registration

For local testing you need to register your own discord bot! https://discord.com/developers/applications

Be sure to enable `Members Intent`! 

Then follow the steps [here](settings.md) to set that token in a safe location.

Once you add the bot to a test server you will need to:

Right Click --> Copy ID

![image](https://user-images.githubusercontent.com/28987352/179986684-fcf6ad1c-89dc-4426-9832-f3e8f5449d10.png)

Set your `CodeJamBot:Settings:PrimaryGuildId` to this value! 

Additionally, you will need to setup the channel and Role IDs the same way. Haven't quite looked into pulling these things dynamically through Names instead of IDs. 



