# WorkingMan

## Introduction

`WorkingMan` is a plugin for servers wishing to limit the play time of players. Why would you want to do this? This plugin is designed for people who enjoy playing vanilla Rust with full PvP, but have lives outside of the game. Work, family, friends, and/or other interests take up the bulk of your day, but you still enjoy playing Rust for a few hours a day, several days a week. Unfortunately, Rust is designed in such a way that the more you play, the more of an advantage you have against other players on the server. Limiting all players to the same amount of play time per day and/or per week puts them on a more level playing field and prevents the all-to-common scenario where people are roaming the map on wipe day with AKs killing nakeds with bows.

## Details

`WorkingMan` allows you to configure your server to limit the play time of players to a certain number of seconds per day, and/or per week. It defaults to 4 hours per day and no weekly limit. Players can use their play time whenever they like during the day, each day, but when they reach their daily or weekly limit, they will be kicked from the server until the next day or week time cycle begins. Warnings are displayed to a user coming close to their time limit via server messages and GUI banner displays. Once a player's play time crosses the first warning threshold (when he has 30 minutes left, by default), he will receive a warning every 5 minutes. Once he reaches the second threshold (when he has 10 minutes left, by default), he will be warned every minute. This gives the player time to find a safe spot to log off for the rest of the cycle. A player trying to log back into the server when he has no time left in the current cycle is denied and provided with a message informing him of when the next cycle begins.

## Dependencies

[GUI Announcements](https://umod.org/plugins/gui-announcements)

## Considerations

`WorkingMan` uses your server's configured timezone for determining the daily and weekly cycle start times. Be sure your server's timezone is configured how you desire. If you update your server's timezone, you can reload the plugin to have it sync with your new timezone as well.

## Configuration
`secondsPerDay` = Number of seconds allowed per day, set to `0` to disable this limit. Default is `14400` seconds, or 4 hours.
`secondsPerWeek` = Number of seconds allowed per week, set to `0` to disable this limit. Default is `0`.
`warningThreshold1` = Once a player's timer has less than this many seconds left, warnings are sent every 5 minutes. Defaults to `1800`, or 30 minutes.
`warningThreshold2` = Once a player's timer has less than this many seconds left, warnings are sent every minute. Defaults to `600`, or 10 minutes. 
`dayOfWeek` = Zero-based index of the day of the week that the weekly cycle begins on. Defaults to `4`, or Thursday.

## Player Commands

Players have just one command they can issue via chat: `/checktimer`. This will have a message sent to them specifying how much time they have played for the current cycle(s) and how much time is left until the next cycle(s).

## Admin Commands
Admin commands are all issued via the console.

`workingman.reset` - Resets the admin's play time for the current cycle(s) to zero.

`workingman.setdaytimer <player name or id> <seconds>` - Sets the designated player's daily timer for the current day cycle to `<seconds>`.

`workingman.setweektimer <player name or id> <seconds>` - Sets the designated player's weekly timer for the current week cycle to `<seconds>`.

`workingman.setdaylimit <seconds>` - Updates the server's configured number of seconds allowed per day. Set this to 0 to disable the daily time limit.

`workingman.setweeklimit <seconds>` - Updates the server's configured number of seconds allowed per week. Set this to 0 to disable the weekly time limit.

`workingman.setweekstartday <day>` - Sets the day that the weekly cycle starts if you have a weekly time limit configured. `<day>` should be a number from 0 to 6, 0 being Sunday and 6 being Saturday. This value defaults to Thursday (4), a typical wipe day for many servers.

`workingman.resetdefaults` - Resets the server's configuration to the default settings (4 hours per day, 30 minute and 10 minute warning thresholds).

`workingman.setwarn1 <seconds>` - Sets warning threshold 1 to be `<seconds>` remaining. Once this threshold is reached, a player will receive a warning every 5 minutes.

`workingman.setwarn2 <seconds>` - Sets warning threshold 2 to be `<seconds>` remaining. Once this threshold is reached, a player will receive a warning every minute.

`workingman.givetimeday <player name or id> <seconds>` - Subtracts `<seconds>` from the designated player's daily timer.

`workingman.givetimeweek <player name or id> <seconds>` - Subtracts `<seconds>` from the designated player's weekly timer.

