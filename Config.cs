namespace BetterBalance;

using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

public class BetterBalanceConfig : BasePluginConfig
{
    [JsonPropertyName("BalanceMode")]
    public int BalanceMode { get; set; } = 1; // 1: balance on team max difference, 2: balance on team max players

    [JsonPropertyName("MoveMode")]
    public int MoveMode { get; set; } = 1; // 1: move recent player 2: move random players 3: scramble all players 4: scramble all players based on kills

    [JsonPropertyName("KillPlayerOnBalance")]
    public bool KillPlayerOnSwitch { get; set; } = false;

    [JsonPropertyName("MaxDifference")]
    public int MaxDifference { get; set; } = 1; // ignored if balancemode is 2

    [JsonPropertyName("Max_CT_Players")]
    public int MaxCTPlayers { get; set; } = 1; // ignored if balancemode is 1

    [JsonPropertyName("Max_T_Players")]
    public int MaxTPlayers { get; set; } = 1; // ignored if balancemode is 1
}