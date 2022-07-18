[appsettings.json](https://github.com/Programming-Simplified-Community/CommunityBot/blob/main/Api/appsettings.json) acts as our entrypoint for the bot.

Anything considered **secret** should **not** be stored in this file. For instance, the Discord token! 

Please use the following command to stash secrets in a location outside of the project solution:

```bash
dotnet user-secrets set "CodeJamBot:Discord:Token" "MY_SUPER_AWESOME_TOKEN_GOES_HERE"
```

If for whatever reason user-secrets fails to set, it could be you need to initialize your secret location via
```bash
dotnet user-secrets init
```

You might be curious why it looks like that... the path... well, if you check out the file you'll see
```json
"CodeJamBot": {
    
    "Discord": {
      "Token": ""
    }
  }
```
We wanted to update/modify the **Token** value, and to get to it you have to into CodeJamBot, then Discord, and finally Token. Which becomes
`CodeJamBot:Discord:Token`

### Important
ANY VALUE - that you stored in user-secrets will OVERRIDE any value in the configuration. So, even if you were to update the token inside `appsettings.json`, it will be using the one from `user-secrets`. 
