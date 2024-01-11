using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace BetterBalance;

public class BetterBalanceConfig : BasePluginConfig
{
    [JsonPropertyName("AutoBalance")]
    public bool AutoBalance { get; set; } = true;

    [JsonPropertyName("PermissionRequired")]
    public string PermissionRequired { get; set; } = "@css/generic";

    [JsonPropertyName("ChatPrefix")]
    public string ChatPrefix { get; set; } = "[BetterBalance]";

    [JsonPropertyName("BalanceCommand")]
    public string BalanceCommand { get; set; } = "css_balance";

    [JsonPropertyName("ScrambleCommand")]
    public string ScrambleCommand { get; set; } = "css_scramble";

    [JsonPropertyName("ImBalanceMessage")]
    public string ImBalanceMessage { get; set; } = "{red} Teams are imbalanced, balancing now!";

    [JsonPropertyName("BalanceMode")]
    public int BalanceMode { get; set; } = 1; // 1: balance on team max difference, 2: balance on team max players 3: scamble mode

    [JsonPropertyName("MoveMode")]
    public int MoveMode { get; set; } = 1; // 1: move recent player 2: move random players | Scamble mode: 1: Scramble random 2: Scramble by kills

    [JsonPropertyName("KillPlayerOnBalance")]
    public bool KillPlayerOnSwitch { get; set; } = false;

    [JsonPropertyName("MaxDifference")]
    public int MaxDifference { get; set; } = 1; // ignored if balancemode is 2

    [JsonPropertyName("Max_CT_Players")]
    public int MaxCTPlayers { get; set; } = 1; // ignored if balancemode is 1

    [JsonPropertyName("Max_T_Players")]
    public int MaxTPlayers { get; set; } = 1; // ignored if balancemode is 1

    [JsonPropertyName("ConfigVersion")]
    public override int Version { get; set; } = 2;
}