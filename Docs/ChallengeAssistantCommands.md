# Challenge Commands

### generate

*Permissions*: `Administrator`

This command is meant to be used in a dedicated channel for `challenges`. It will delete all the current messages, then replace everything with a clean slate of challenges.

Each challenge is represented by a message with buttons representing the `language` to attempt that challenge with.

### leaderboard

Generates a leaderboard based on submissions.

Users are ranked by total number of points, then by least number of attempts, then by most performant code. This at least allows users who keep trying to gain the leaderboard despite having a high attempt count. Meanwhile, those with least attempts can end up at the top if they scored a perfect score.