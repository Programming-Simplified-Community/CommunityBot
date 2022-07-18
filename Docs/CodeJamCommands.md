# Registration Commands

### View

Will compile a list of all the Code-Jam-Topics that are currently accepting applicants. If any exist, the bot will DM the user those topics. Otherwise, they'll see a message indicating nothing is open for applicants.

### Confirm

This is meant to help confirm a user's continued interest in a code jam. Starting 3 days before the end of the application period users will start receiving reminders to confirm. 

**Parameters**:
```yml
topic: AutoCompleted via 'ConfirmJamAutoCompleteProvider'
confirm: True/False
```

### Apply

Allows a user to apply for a topic of their choosing. User must provide some basic information.

**Parameters**:
```yml
category: AutoCompleted via RegisterableJamAutoCompleteProvider
timezone: AutoCompleted via TimezoneAutoCompleteProvider
experienceLevel: AutoCompleted via ExperienceAutoCompleteProvider
preferTeam: True/False - does user want to be on a team or not
```

### Withdraw

When a user is no longer interested, or unable to participate in a jam they registered for... this command will let them leave! 

- If they leave during the registration period their record is deleted. They are able to reapply if they want, so long as it's still within the registration period.
- If they leave *during* a jam, and there are points associated with the [Withdraw Id](https://github.com/Programming-Simplified-Community/CommunityBot/blob/main/Data/CodeJam/Topic.cs#L51) - points will be deducted from their user-profile. This is to help assist us in placing users who commonly abandon jams together to prevent ruining the experience with other members.

# Submission Commands

### list

*Permissions*: `super_mega_ultra_moderator` (for now) - Was done before I realized why the permission attribute wasn't working

Allows a user to generate a list of users who have currently submitted their jam!

### submit

Allows a team to submit their project! Their URL is validated to be Github (for now, need to tweak it to allow gitlab, and potentially azure).
Will then validate that the repo is publicly available.

**Parameters**:
```yml
topic: AutoCompleted via ActiveJamAutoCompleteProvider
repo: URL to repository
```

# Team Commands

### change-name

Allows a code-jam team to vote on a new name for both their channel and role! Majority rules. If more than half favor the name, the changes are pushed through. Otherwise nothing happens. If all team members vote and it's a tie - nothing happens.
By nothing happens, the message is deleted but no changes are made to the role or channel name.

**Parameters**:
```yml
teamName: The suggested name
```

# Topic commands

### set-reg-dates / set-sub-dates

**Permissions**: `Administrator`

Allows user to update the registration or submission date period for a specific topic.

**Parameters**:
```yml
topic: AutoCompleted via ModeratorJamTopicAutoCompleteProvider
startMonth: DateMonthEnum
startDay: int
endMonth: DateMonthEnum
endDay: int
```

# Bot Commands

## shutdown

**Permissions**: `Administrator`

Cleanly shut the bot down.