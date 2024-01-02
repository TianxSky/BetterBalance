# Better Balance
 Auto Balance Plugin for CS2 using CounterStrikeSharp
## Commands
1. css_balance: requires 2 arguments(balance mode, move mode) see below
   - Usage: !balance 3 2
    - I use !balance 3 2 all the time to create a fair and balanced team

## Balance Mode
* 1 - Max Difference: Based on team's difference in numbers of players, auto balanced if difference > Config.MaxDifference
* 2 - Team Limits: Based on max players can be on each team, defined in config
* 3 - Scramble: This will affect all players
## Move Mode
### Balance Mode 1/2:
* 1 - Move Recent: Move the last player that joins the server
* 2 - Move Random: Move a random player
### Balance Mode 3:
* 1 - Scramble Random: Randomly putting each player on both team
* 2 - Scramble Based On Kills: Putting players even only to both teams and will put the extra player(if any) on the opposite team of player with highest kill
  
## Plugin Config
```json
{
  "BalanceMode": 1, // 1: balance on team max difference, 2: balance on team max players 3: scamble mode
  "MoveMode": 1, // 1: move recent player 2: move random players | Scamble mode: 1: Scramble random 2: Scramble by kills
  "KillPlayerOnBalance": true,
  "MaxDifference": 1, // ignored if balancemode is 2
  "Max_CT_Players": 1, // ignored if balancemode is 1
  "Max_T_Players": 1, // ignored if balancemode is 1
  "ConfigVersion": 1
}
```
## Todo
- [ ] Schedule balance on round end 
