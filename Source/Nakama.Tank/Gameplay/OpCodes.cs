// Copyright Pumpkin Games Ltd. All Rights Reserved.

namespace NakamaTank.NakamaMultiplayer;

/// <summary>
/// Defines the various network operations that can be sent/received.
/// </summary>
public class OpCodes
{
    public const long TANK_PACKET = 1;
    public const long SCORED = 2;
    public const long RESPAWNED = 3;
    public const long NEW_ROUND = 4;
}
