using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;

namespace BetterBalance
{
    public class BetterBalance : BasePlugin, IPluginConfig<BetterBalanceConfig>
    {
        public override string ModuleName => "Better Balance";
        public override string ModuleVersion => "1.0";
        public override string ModuleAuthor => "Tian";

        public BetterBalanceConfig Config { get; set; } = new();
        private List<CCSPlayerController> RecentPlayers = new();

        public override void Load(bool hotReload)
        {
            RegisterListener<OnMapStart>(mapname =>
            {
                RecentPlayers.Clear();
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

        public override void Unload(bool hotReload)
        {
            BBLog.LogToConsole(ConsoleColor.Green, $"[BetterBalance] -> {ModuleName} version {ModuleVersion} unloaded");
        }

        public void OnConfigParsed(BetterBalanceConfig config)
        {
            Config = config;
            BBLog.LogToConsole(ConsoleColor.Green, $"[BetterBalance] -> Loading config file");
        }

        private HookResult OnRoundEnd(EventRoundEnd e, GameEventInfo info)
        {
            TryBalance(Utilities.GetPlayers().Where(p => IsPlayerValid(p)).Where(p => p.TeamNum == 2).ToList(), Utilities.GetPlayers().Where(p => IsPlayerValid(p)).Where(p => p.TeamNum == 3).ToList(), Config.BalanceMode, Config.MoveMode);
            return HookResult.Continue;
        }

        [GameEventHandler(HookMode.Post)]
        private HookResult OnConnect(EventPlayerConnectFull e, GameEventInfo info)
        {
            RecentPlayers.Add(e.Userid);
            return HookResult.Continue;
        }

        private void TryBalance(List<CCSPlayerController> tplayers, List<CCSPlayerController> ctplayers, int balancemode, int movemode)
        {
            var tcounts = tplayers.Count;
            var ctcounts = ctplayers.Count;
            var maxTPlayers = Config.MaxTPlayers;
            var maxCTPlayers = Config.MaxCTPlayers;

            if (balancemode == 2) // Mode based on maximum players
            {
                if (tcounts <= maxTPlayers && ctcounts <= maxCTPlayers)
                {
                    BBLog.Log(2, "Team players count is under the Max team limit");
                    return;
                }

                BalanceBasedOnMaxPlayers(tplayers, ctplayers, tcounts, ctcounts, maxTPlayers, maxCTPlayers);
            }
            else if (balancemode == 1) // Mode based on team size difference
            {
                var diff = Math.Abs(tcounts - ctcounts);
                if (diff <= Config.MaxDifference)
                {
                    BBLog.Log(2, "Team difference is under the Max allowed difference");
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
                BalanceTeams(tplayers, ctplayers, tcounts, ctcounts, maxCTPlayers, CsTeam.CounterTerrorist, 1);
            }
            if (ctcounts > maxCTPlayers)
            {
                BalanceTeams(ctplayers, tplayers, ctcounts, tcounts, maxTPlayers, CsTeam.Terrorist, 1);
            }
            if (tcounts > maxTPlayers && ctcounts > maxCTPlayers)
            {
                MoveExceededPlayersToSpectator(tplayers, ctplayers, tcounts, ctcounts, maxTPlayers, maxCTPlayers);
            }
        }

        private void MoveExceededPlayersToSpectator(List<CCSPlayerController> team1, List<CCSPlayerController> team2,
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

        private void BalanceBasedOnTeamDifference(List<CCSPlayerController> tplayers, List<CCSPlayerController> ctplayers,
            int tcounts, int ctcounts, int diff, int movemode)
        {
            if (tcounts > ctcounts)
            {
                BalanceTeams(tplayers, ctplayers, tcounts, ctcounts, diff, CsTeam.CounterTerrorist, movemode);
            }
            else if (ctcounts > tcounts)
            {
                BalanceTeams(ctplayers, tplayers, ctcounts, tcounts, diff, CsTeam.Terrorist, movemode);
            }
        }

        private void BalanceTeams(List<CCSPlayerController> sourceTeam, List<CCSPlayerController> targetTeam,
            int sourceCount, int targetCount, int numToMove, CsTeam targetType, int movemode)
        {
            switch (movemode)
            {
                case 1:
                    MoveRecentPlayers(targetType, numToMove);
                    break;

                case 2:
                    MoveRandomPlayers(sourceTeam, targetType, numToMove);
                    break;

                default:
                    break;
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

        private void ScrambleAllPlayers(List<CCSPlayerController> tplayers, List<CCSPlayerController> ctplayers)
        {
            var rand = new Random();

            foreach (var player in tplayers.Concat(ctplayers))
            {
                var teamToJoin = rand.Next(0, 2) == 0 ? CsTeam.Terrorist : CsTeam.CounterTerrorist;
                player.SwitchTeam(teamToJoin);
            }
        }

        private void ScramblePlayersByKills(List<CCSPlayerController> tplayers, List<CCSPlayerController> ctplayers)
        {
            var playerKills = tplayers.Concat(ctplayers).ToDictionary(player => player, player => player.ActionTrackingServices.MatchStats.Kills);

            var orderedPlayers = playerKills.OrderByDescending(pair => pair.Value).Select(pair => pair.Key).ToList();

            var isTerroristTeam = true;

            // Determine the team with fewer players
            var teamToFill = tplayers.Count < ctplayers.Count ? tplayers : ctplayers;

            foreach (var player in orderedPlayers)
            {
                if (teamToFill.Count > 0)
                {
                    player.SwitchTeam(isTerroristTeam ? CsTeam.Terrorist : CsTeam.CounterTerrorist);
                    isTerroristTeam = !isTerroristTeam;
                    teamToFill.Remove(teamToFill.First()); // Remove a player from the team that just received a player
                }
                else
                {
                    // If one team is full, assign the remaining players to the other team
                    var oppositeTeam = isTerroristTeam ? ctplayers : tplayers;
                    player.SwitchTeam(oppositeTeam.First().TeamNum == 2 ? CsTeam.CounterTerrorist : CsTeam.Terrorist);
                    oppositeTeam.Remove(oppositeTeam.First());
                }
            }
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

            TryBalance(tplayers, ctplayers, result1, result2);
        }

        public static bool IsPlayerValid(CCSPlayerController? player)
        {
            return player is { IsValid: true, IsHLTV: false, TeamNum: 2 or 3 };
        }
    }
}