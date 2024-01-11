using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace BetterBalance
{
    public class BetterBalance : BasePlugin, IPluginConfig<BetterBalanceConfig>
    {
        private readonly List<CCSPlayerController> RecentPlayers = new();
        public BetterBalanceConfig Config { get; set; } = new();
        public override string ModuleAuthor => "Tian";
        public override string ModuleName => "Better Balance";
        public override string ModuleVersion => "1.0";

        public static bool IsPlayerValid(CCSPlayerController? player)
        {
            return player is { IsValid: true, Connected: PlayerConnectedState.PlayerConnected, TeamNum: 2 or 3 };
        }

        public override void Load(bool hotReload)
        {
            base.Load(hotReload);
            AddCommand(Config.ScrambleCommand, "Scramble Teams",
                [CommandHelper(minArgs: 1, usage: "[1: Random, 2: Based on kills]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
            (player, info) =>
            {
                if (!player!.IsValid && AdminManager.PlayerHasPermissions(player, Config.PermissionRequired))
                {
                    return;
                }

                string arg1 = info.GetArg(1);
                List<CCSPlayerController> players = Utilities.GetPlayers().Where(IsPlayerValid).ToList();
                if (!int.TryParse(arg1, out int result))
                {
                    result = Config.MoveMode;
                    return;
                }
                if (player!.IsValid)
                {
                    BBLog.LogToChat(player, $" {ChatColors.Green}Scrambling teams with mode {result}");
                    BBLog.Log(0, $"Scrambling teams with mode {result}");
                }
                TryScramble(players, result);
            });
            AddCommand(Config.BalanceCommand, "Balance Teams",
                [CommandHelper(minArgs: 2, usage: "[balance mode] [move mode]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
            (player, info) =>
                {
                    if (!player!.IsValid && AdminManager.PlayerHasPermissions(player, Config.PermissionRequired))
                    {
                        BBLog.LogToChat(player, $" {ChatColors.Red}You don't have permission to use this command");
                        return;
                    }

                    string arg1 = info.GetArg(1);
                    string arg2 = info.GetArg(2);
                    List<CCSPlayerController> players = Utilities.GetPlayers().Where(IsPlayerValid).ToList();
                    if (!int.TryParse(arg1, out int result1))
                    {
                        BBLog.LogToChat(player, $" {ChatColors.Red}Invalid balance mode");
                        return;
                    }
                    if (!int.TryParse(arg2, out int result2))
                    {
                        BBLog.LogToChat(player, $" {ChatColors.Red}Invalid move mode");
                        return;
                    }
                    if (player!.IsValid)
                    {
                        BBLog.LogToChat(player, $" {ChatColors.Green}Balancing teams with mode {result1} and move mode {result2}");
                        BBLog.Log(0, $"Balancing teams with mode {result1} and move mode {result2}");
                    }
                    TryBalance(players, result1, result2);
                });
            RegisterEventHandler<EventCsWinPanelMatch>((e, info) =>
            {
                RecentPlayers.Clear();
                return HookResult.Continue;
            });
            RegisterEventHandler<EventRoundEnd>((e, info) =>
            {
                if (!Config.AutoBalance)
                {
                    return HookResult.Continue;
                }

                TryBalance(Utilities.GetPlayers().Where(IsPlayerValid).ToList(), Config.BalanceMode, Config.MoveMode);
                return HookResult.Continue;
            });
            RegisterEventHandler<EventPlayerConnect>((e, info) =>
            {
                RecentPlayers.Add(e.Userid);
                return HookResult.Continue;
            });
            RegisterEventHandler<EventPlayerDisconnect>((e, info) =>
            {
                _ = RecentPlayers.Remove(e.Userid);
                return HookResult.Continue;
            });
            BBLog.Log(1, $"{ModuleName} version {ModuleVersion} loaded");
        }

        public void OnConfigParsed(BetterBalanceConfig config)
        {
            Config = config;
            BBLog.SetPrefix(Config.ChatPrefix);
            BBLog.Log(1, $"{ModuleName} version {ModuleVersion} config loaded");
        }

        public override void Unload(bool hotReload)
        {
            base.Unload(hotReload);
            BBLog.Log(1, $"{ModuleName} version {ModuleVersion} unloaded");
        }

        private static void ScramblePlayersByKills(List<CCSPlayerController> players)
        {
            // Combine players from both teams and create a dictionary with player-to-kills mapping
            Dictionary<CCSPlayerController, int?> playerKills = players.ToDictionary(player => player, player => player?.ActionTrackingServices?.MatchStats.Kills);

            // Order players by kills in descending order
            List<CCSPlayerController> orderedPlayers = playerKills.OrderByDescending(pair => pair.Value)
                                            .Select(pair => pair.Key)
                                            .ToList();

            bool IsT = orderedPlayers.FirstOrDefault()!.TeamNum == 2;

            // Separate players with fewer kills to the other team
            for (int i = 1; i < orderedPlayers.Count; i++)
            {
                CCSPlayerController player = orderedPlayers[i];
                if (!IsT)
                {
                    player.SwitchTeam(CsTeam.Terrorist);
                    IsT = !IsT;
                }
                else
                {
                    player.SwitchTeam(CsTeam.CounterTerrorist);
                    IsT = !IsT;
                }
                if (orderedPlayers.Count % 2 == 1)
                {
                    orderedPlayers[^1].SwitchTeam(IsT ? CsTeam.CounterTerrorist : CsTeam.Terrorist);
                }
            }
        }

        private void BalanceDiff(List<CCSPlayerController> tplayers, List<CCSPlayerController> ctplayers,
            int tcounts, int ctcounts, int diff, int movemode)
        {
            int numtomove = (int)Math.Floor((double)diff / 2);
            if (tcounts > ctcounts)
            {
                BalanceTeams(tplayers, numtomove, CsTeam.CounterTerrorist, movemode);
            }
            else if (ctcounts > tcounts)
            {
                BalanceTeams(ctplayers, numtomove, CsTeam.Terrorist, movemode);
            }
        }

        private void BalanceMax(List<CCSPlayerController> players, int movemode)
        {
            List<CCSPlayerController> tplayers = new();
            List<CCSPlayerController> ctplayers = new();

            foreach (CCSPlayerController player in players)
            {
                if (player.TeamNum == 2)
                {
                    tplayers.Add(player);
                }
                else if (player.TeamNum == 3)
                {
                    ctplayers.Add(player);
                }
            }

            int tcounts = tplayers.Count;
            int ctcounts = ctplayers.Count;
            int tdiff = tcounts - Config.MaxTPlayers;
            int ctdiff = ctcounts - Config.MaxCTPlayers;

            if (tdiff < 0 && ctdiff < 0)
            {
                BBLog.Log(1, "Team limit has not been reached");
                return;
            }
            if (tdiff > 0 && ctdiff > 0)
            {
                MoveExceededPlayersToSpectator(tplayers, ctplayers, tdiff, ctdiff, movemode);
            }

            if (tdiff > 0)
            {
                BalanceTeams(tplayers, tdiff, CsTeam.CounterTerrorist, 1);
            }
            if (ctdiff > 0)
            {
                BalanceTeams(ctplayers, ctdiff, CsTeam.Terrorist, 1);
            }
        }

        private void BalanceTeams(List<CCSPlayerController> sourceTeam, int numToMove, CsTeam targetType, int movemode)
        {
            switch (movemode)
            {
                case 1:
                    if (RecentPlayers.Where(IsPlayerValid).Any())
                    {
                        MoveRecentPlayers(targetType, numToMove);
                    }
                    else
                    {
                        MoveRandomPlayers(sourceTeam, targetType, numToMove);
                    }
                    break;

                case 2:
                    MoveRandomPlayers(sourceTeam, targetType, numToMove);
                    break;

                default:
                    MoveRandomPlayers(sourceTeam, targetType, numToMove);
                    break;
            }
        }

        private void MoveExceededPlayersToSpectator(
            List<CCSPlayerController> tplayers, List<CCSPlayerController> ctplayers, int tdiff, int ctdiff, int movemode)
        {
            int diff = tdiff + ctdiff;
            if (movemode == 1)
            {
                for (int i = 0; i < diff; i++)
                {
                    CCSPlayerController recentPlayer = RecentPlayers.Last();
                    if (IsPlayerValid(recentPlayer))
                    {
                        SwitchMode(recentPlayer, CsTeam.Spectator);
                    }
                    else
                    {
                        i--;
                        _ = RecentPlayers.Remove(recentPlayer);
                    }
                }
            }
            else
            {
                for (int i = 0; i < diff; i++)
                {
                    Random rand = new();
                    int tplayer = rand.Next(0, tplayers.Count);
                    int ctplayer = rand.Next(0, ctplayers.Count);
                    if (tdiff > 0)
                    {
                        SwitchMode(tplayers[tplayer], CsTeam.Spectator);
                        tplayers.RemoveAt(tplayer);
                        tdiff--;
                    }
                    else if (ctdiff > 0)
                    {
                        SwitchMode(ctplayers[ctplayer], CsTeam.Spectator);
                        ctplayers.RemoveAt(ctplayer);
                        ctdiff--;
                    }
                }
            }
        }

        private void MoveRandomPlayers(List<CCSPlayerController> sourceTeam, CsTeam targetType, int numToMove)
        {
            Random rand = new();

            for (int i = 0; i < numToMove; i++)
            {
                int sourceCount = sourceTeam.Count;
                int a = rand.Next(0, sourceCount);
                SwitchMode(sourceTeam[a], targetType);
            }
        }

        private void MoveRecentPlayers(CsTeam targetType, int numToMove)
        {
            for (int i = 0; i < numToMove; i++)
            {
                CCSPlayerController recentPlayer = RecentPlayers.Last();
                if (IsPlayerValid(recentPlayer))
                {
                    if (!Config.KillPlayerOnSwitch)
                    {
                        recentPlayer.SwitchTeam(targetType);
                    }
                    else
                    {
                        recentPlayer.ChangeTeam(targetType);
                    }
                }
                else
                {
                    i--;
                    _ = RecentPlayers.Remove(recentPlayer);
                }
            }
        }

        private void ScrambleAllPlayers(List<CCSPlayerController> players)
        {
            bool isTerrorist = true;
            Random rand = new();
            for (int i = 0; i < players.Count; i++)
            {
                List<int> result = Enumerable.Range(0, players.Count).OrderBy(g => rand.NextDouble()).ToList();
                CsTeam teamToJoin = isTerrorist ? CsTeam.Terrorist : CsTeam.CounterTerrorist;
                SwitchMode(players[result[i]], teamToJoin);
                isTerrorist = !isTerrorist;
            }
        }

        private void SwitchMode(CCSPlayerController player, CsTeam team)
        {
            if (Config.KillPlayerOnSwitch)
            {
                player.ChangeTeam(team);
            }
            else
            {
                player.SwitchTeam(team);
            }
        }

        private void TryBalance(List<CCSPlayerController> players, int balancemode, int movemode)
        {
            List<CCSPlayerController> tplayers = players.Where(p => p.TeamNum == 2).ToList();
            List<CCSPlayerController> ctplayers = players.Where(p => p.TeamNum == 3).ToList();
            int tcounts = tplayers.Count;
            int ctcounts = ctplayers.Count;
            int maxTPlayers = Config.MaxTPlayers;
            int maxCTPlayers = Config.MaxCTPlayers;

            if (balancemode == 2) // Mode based on maximum players
            {
                if (tcounts <= maxTPlayers && ctcounts <= maxCTPlayers)
                {
                    BBLog.Log(0, "Team players count is under the Max team limit");
                    return;
                }
                else
                {
                    BBLog.LogToChatAll(Config.ImBalanceMessage);
                    BalanceMax(players, movemode);
                }
            }
            else if (balancemode == 1) // Mode based on team size difference
            {
                int diff = Math.Abs(tcounts - ctcounts);
                if (diff <= Config.MaxDifference)
                {
                    BBLog.Log(0, "Team difference is under the Max difference");
                    return;
                }
                else
                {
                    BBLog.LogToChatAll(Config.ImBalanceMessage);
                }

                BalanceDiff(tplayers, ctplayers, tcounts, ctcounts, diff, movemode);
            }
            else
            {
                BBLog.Log(2, "Invalid balance mode");
            }
        }

        private void TryScramble(List<CCSPlayerController> players, int movemode)
        {
            if (movemode == 1)
            {
                ScrambleAllPlayers(players);
            }
            else
            {
                ScramblePlayersByKills(players);
            }
        }
    }
}