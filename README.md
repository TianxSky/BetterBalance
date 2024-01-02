<a name="readme-top"></a>
<!-- PROJECT SHIELDS -->
[![Contributors][contributors-shield]][contributors-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![MIT License][license-shield]][license-url]

<!-- PROJECT LOGO -->
<br />
<div align="center">
  <h3 align="center">Better Balance</h3>
  <p align="center">
    Auto Balance Plugin for CS2 using CounterStrikeSharp
  </p>
</div>

<!-- TABLE OF CONTENTS -->
<details>
  <summary>Table of Contents</summary>
  <ol>
    <li><a href="#commands">Commands</a></li>
    <li><a href="#balance-mode">Balance Mode</a></li>
    <li><a href="#move-mode">Move Mode</a></li>
    <li><a href="#plugin-config">Plugin Config</a></li>
    <li><a href="#todo">Todo</a></li>
  </ol>
</details>

<!-- ABOUT THE PROJECT -->
## About The Project

Here's a plugin for CounterStrikeSharp that automatically balances teams based on different modes. It provides various commands and configurations for efficient team balancing.

### Commands

1. `css_balance`: requires 2 arguments(balance mode, move mode)
   - Usage: `!balance 3 2`
     - I use `!balance 3 2` all the time to create a fair and balanced team

## Balance Mode
* 1 - Max Difference: Based on team's difference in numbers of players, auto balanced if difference > Config.MaxDifference
* 2 - Team Limits: Based on max players can be on each team, defined in config
* 3 - Scramble: This will affect all players
  

## Move Mode
### Balance Mode 1/2:
* 1 - Move Recent: Move the last player that joins the server
* 2 - Move Random: Move a random player
### Balance Mode 3:
* 1 - Scramble Random: Randomly putting each player on both teams
* 2 - Scramble Based On Kills: Putting players to both teams based on kills and will put the extra player(if any) on the opposite team of player with highest kill

  
## Plugin Config
```json
{
  "BalanceMode": 1,
  "MoveMode": 1,
  "KillPlayerOnBalance": true,
  "MaxDifference": 1,
  "Max_CT_Players": 1,
  "Max_T_Players": 1,
  "ConfigVersion": 1
}
```
## Roadmap

- [ ] Schedule balance on round end
      
<p align="right">(<a href="#readme-top">back to top</a>)</p>

[contributors-shield]: https://img.shields.io/github/contributors/TianxSky/BetterBalance.svg?style=for-the-badge
[contributors-url]: https://github.com/TianxSky/BetterBalance/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/TianxSky/BetterBalance.svg?style=for-the-badge
[forks-url]: https://github.com/TianxSky/BetterBalance/network/members
[stars-shield]: https://img.shields.io/github/stars/TianxSky/BetterBalance.svg?style=for-the-badge
[stars-url]: https://github.com/TianxSky/BetterBalance/stargazers
[issues-shield]: https://img.shields.io/github/issues/TianxSky/BetterBalance.svg?style=for-the-badge
[issues-url]: https://github.com/TianxSky/BetterBalance/issues
[license-shield]: https://img.shields.io/github/license/TianxSky/BetterBalance.svg?style=for-the-badge
[license-url]: https://github.com/TianxSky/BetterBalance/blob/master/LICENSE.txt
