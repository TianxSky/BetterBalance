using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace BetterBalance
{
    public class BetterBalance : BasePlugin, IPluginConfig<BetterBalanceConfig>
    {
        private List<CCSPlayerController> RecentPlayers = new();
        public BetterBalanceConfig Config { get; set; } = new();
        public override string ModuleAuthor => "Tian";
        public override string ModuleName => "Better Balance";
        public override string ModuleVersion => "1.0";

        public static bool IsPlayerValid(CCSPlayerController? player)
        {
            return player is { IsValid: true, IsHLTV: false, TeamNum: 2 or 3 };
        }

        [ConsoleCommand("css_balance", "Balance Teams")]
        [CommandHelper(minArgs: 2, usage: "balance_mode move_mode]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        [RequiresPermissions("@css/generic")]
        public void Balance(CCSPlayerController? player, CommandInfo info)
        {
            var arg1 = info.GetArg(1);
            var arg2 = info.GetArg(2);
            var players = Utilities.GetPlayers().Where(IsPlayerValid).ToList();
            var tplayers = players.Where(p => p.TeamNum == 2).ToList();
            var ctplayers = players.Where(p => p.TeamNum == 3).ToList();
            BBLog.Log(0, $"PlayerCounts: {players.Count}");
            BBLog.Log(0, $"T PlayerCounts: {tplayers.Count}");
            BBLog.Log(0, $"CT PlayerCounts: {ctplayers.Count}");
            if (!int.TryParse(arg1, out int result1))
            {
                BBLog.Log(2, "Invalid format for arg 1(expected int): " + arg1);
                return;
            }
            if (!int.TryParse(arg2, out int result2))
            {
                BBLog.Log(2, "Invalid format for arg 2(expected int): " + arg2);
                return;
            }

            TryBalance(players, result1, result2);
        }

        public override void Load(bool hotReload)
        {
            base.Load(hotReload);
            RegisterEventHandler<EventCsWinPanelMatch>((e, info) =>
            {
                RecentPlayers.Clear();
                return HookResult.Continue;
            });
            RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
            RegisterEventHandler<EventPlayerConnectFull>(OnConnect);
            RegisterEventHandler<EventPlayerDisconnect>((e, info) =>
            {
                RecentPlayers.Remove(e.Userid);
                return HookResult.Continue;
            }, HookMode.Post);
            BBLog.LogToConsole(ConsoleColor.Green, $"[BetterBalance] -> {ModuleName} version {ModuleVersion} loaded");
        }

        public void OnConfigParsed(BetterBalanceConfig config)
        {
            Config = config;
            BBLog.LogToConsole(ConsoleColor.Green, $"[BetterBalance] -> Loading config file");
        }

        public override void Unload(bool hotReload)
        {
            base.Unload(hotReload);
            BBLog.LogToConsole(ConsoleColor.Green, $"[BetterBalance] -> {ModuleName} version {ModuleVersion} unloaded");
        }

        private static void ScrambleAllPlayers(List<CCSPlayerController> tplayers, List<CCSPlayerController> ctplayers)
        {
            var rand = new Random();

            foreach (var player in tplayers.Concat(ctplayers))
            {
                var teamToJoin = rand.Next(0, 2) == 0 ? CsTeam.Terrorist : CsTeam.CounterTerrorist;
                player.SwitchTeam(teamToJoin);
            }
        }

        private static void ScramblePlayersByKills(List<CCSPlayerController> tplayers, List<CCSPlayerController> ctplayers)
        {
            // Combine players from both teams and create a dictionary with player-to-kills mapping
            var playerKills = tplayers.Concat(ctplayers)
                                      .ToDictionary(player => player, player => player?.ActionTrackingServices?.MatchStats.Kills);

            // Order players by kills in descending order
            var orderedPlayers = playerKills.OrderByDescending(pair => pair.Value)
                                            .Select(pair => pair.Key)
                                            .ToList();

            var IsT = true;

            // Separate players with fewer kills to the other team
            for (var i = 0; i < orderedPlayers.Count - 1; i++)
            {
                var player = orderedPlayers[i];
                if (IsT)
                {
                    player.SwitchTeam(CsTeam.Terrorist);
                    IsT = false;
                }
                else
                {
                    player.SwitchTeam(CsTeam.CounterTerrorist);
                    IsT = true;
                }
            }

            // Handle the last player separately
            var lastPlayer = orderedPlayers.Last();
            lastPlayer.SwitchTeam(IsT ? CsTeam.CounterTerrorist : CsTeam.Terrorist);
        }

        private void BalanceBasedOnMaxPlayers(List<CCSPlayerController> tplayers, List<CCSPlayerController> ctplayers,
    int tcounts, int ctcounts, int maxTPlayers, int maxCTPlayers)
        {
            if (tcounts <= maxTPlayers && ctcounts <= maxCTPlayers)
            {
                BBLog.Log(1, "Team limit has not been reached");
                return;
            }

            if (tcounts > maxTPlayers)
            {
                BalanceTeams(tplayers, maxCTPlayers - ctcounts, CsTeam.CounterTerrorist, 1);
            }
            if (ctcounts > maxCTPlayers)
            {
                BalanceTeams(ctplayers, maxTPlayers - tcounts, CsTeam.Terrorist, 1);
            }
            if (tcounts > maxTPlayers && ctcounts > maxCTPlayers)
            {
                MoveExceededPlayersToSpectator(tcounts, ctcounts, maxTPlayers, maxCTPlayers);
            }
        }

        private void BalanceBasedOnTeamDifference(List<CCSPlayerController> tplayers, List<CCSPlayerController> ctplayers,
            int tcounts, int ctcounts, int diff, int movemode)
        {
            int numtomove = (int)Math.Floor((double)diff / 2);
            if (tcounts > ctcounts)
            {
                switch (movemode)
                {
                    case 1:
                        MoveRecentPlayers(CsTeam.CounterTerrorist, numtomove);
                        break;

                    case 2:
                        MoveRandomPlayers(tplayers, CsTeam.CounterTerrorist, numtomove);
                        break;
                }
            }
            else if (ctcounts > tcounts)
            {
                switch (movemode)
                {
                    case 1:
                        MoveRecentPlayers(CsTeam.Terrorist, numtomove);
                        break;

                    case 2:
                        MoveRandomPlayers(ctplayers, CsTeam.Terrorist, numtomove);
                        break;
                }
            }
        }

        private void BalanceTeams(List<CCSPlayerController> sourceTeam, int numToMove, CsTeam targetType, int movemode)
        {
            switch (movemode)
            {
                case 1:
                    MoveRecentPlayers(targetType, numToMove);
                    break;

                case 2:
                    MoveRandomPlayers(sourceTeam, targetType, numToMove);
                    break;
            }
        }

        private void MoveExceededPlayersToSpectator(
            int count1, int count2, int maxCount1, int maxCount2)
        {
            var counts = count1 + count2;
            var max = maxCount1 + maxCount2;
            var NumToMove = counts - max;

            for (var i = 0; i < NumToMove; i++)
            {
                if (counts < max)
                {
                    break; // Stop if one of the teams is within its limit
                }

                var recentPlayer = RecentPlayers.Last();
                recentPlayer.SwitchTeam(CsTeam.Spectator);
                counts--;
            }
        }

        private void MoveRandomPlayers(List<CCSPlayerController> sourceTeam, CsTeam targetType, int numToMove)
        {
            var rand = new Random();

            for (var i = 0; i < numToMove; i++)
            {
                var sourceCount = sourceTeam.Count;
                var a = rand.Next(0, sourceCount);
                if (!Config.KillPlayerOnSwitch)
                {
                    sourceTeam[a].SwitchTeam(targetType);
                }
                else
                {
                    sourceTeam[a].ChangeTeam(targetType);
                }
            }
        }

        private void MoveRecentPlayers(CsTeam targetType, int numToMove)
        {
            for (var i = 0; i < numToMove; i++)
            {
                var recentPlayer = RecentPlayers.Last();
                if (!Config.KillPlayerOnSwitch)
                {
                    recentPlayer.SwitchTeam(targetType);
                }
                else
                {
                    recentPlayer.ChangeTeam(targetType);
                }
            }
        }

        private HookResult OnConnect(EventPlayerConnectFull e, GameEventInfo info)
        {
            RecentPlayers.Add(e.Userid);
            return HookResult.Continue;
        }

        private HookResult OnRoundEnd(EventRoundEnd e, GameEventInfo info)
        {
            TryBalance(Utilities.GetPlayers().Where(IsPlayerValid).ToList(), Config.BalanceMode, Config.MoveMode);
            return HookResult.Continue;
        }

        private void TryBalance(List<CCSPlayerController> players, int balancemode, int movemode)
        {
            var tplayers = players.Where(p => p.TeamNum == 2).ToList();
            var ctplayers = players.Where(p => p.TeamNum == 3).ToList();
            var tcounts = tplayers.Count;
            var ctcounts = ctplayers.Count;
            var maxTPlayers = Config.MaxTPlayers;
            var maxCTPlayers = Config.MaxCTPlayers;

            if (balancemode == 2) // Mode based on maximum players
            {
                if (tcounts <= maxTPlayers && ctcounts <= maxCTPlayers)
                {
                    BBLog.Log(0, "Team players count is under the Max team limit");
                    return;
                }
                else
                {
                    Server.PrintToChatAll($" {ChatColors.Red}Teams are imbalanced, balancing now");
                }

                BalanceBasedOnMaxPlayers(tplayers, ctplayers, tcounts, ctcounts, maxTPlayers, maxCTPlayers);
            }
            else if (balancemode == 1) // Mode based on team size difference
            {
                var diff = Math.Abs(tcounts - ctcounts);
                if (diff <= Config.MaxDifference)
                {
                    BBLog.Log(0, "Team difference is under the Max allowed difference");
                    return;
                }

                BalanceBasedOnTeamDifference(tplayers, ctplayers, tcounts, ctcounts, diff, movemode);
            }
            else
            {
                if (movemode == 1)
                {
                    ScrambleAllPlayers(tplayers, ctplayers);
                }
                else
                {
                    ScramblePlayersByKills(tplayers, ctplayers);
                }
            }
        }
    }
}