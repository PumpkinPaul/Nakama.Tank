// Copyright Pumpkin Games Ltd. All Rights Reserved.

//Based on code from the FishGame Unity sample from Herioc Labs.
//https://github.com/heroiclabs/fishgame-unity/blob/main/FishGame/Assets/Entities/Player/MatchDataJson.cs

using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace NakamaTank.NakamaMultiplayer.Players;

public static class MatchDataJson
{
    /// <summary>
    /// Creates a network message containing velocity and position.
    /// </summary>
    /// <returns>A JSONified string containing velocity and position data.</returns>
    public static string TankPacket(
        float totalSeconds,
        // Send the current state of the tank.
        Vector2 position,
        Vector2 velocity,
        float tankRotation,
        float turretRotation,
        // Also send our current inputs. These can be used to more accurately
        // predict how the tank is likely to move in the future.
        Vector2 tankInput,
        Vector2 turretInput)
    {
        var values = new Dictionary<string, string>
        {
            { "totalSeconds", totalSeconds.ToString() },
            { "position.x", position.X.ToString() },
            { "position.y", position.Y.ToString() },
            { "velocity.x", velocity.X.ToString() },
            { "velocity.y", velocity.Y.ToString() },
            { "tankRotation", tankRotation.ToString() },
            { "turretRotation", turretRotation.ToString() },
            { "tankInput.x", tankInput.X.ToString() },
            { "tankInput.y", tankInput.Y.ToString() },
            { "turretInput.x", turretInput.X.ToString() },
            { "turretInput.y", turretInput.Y.ToString() },
        };

        return Newtonsoft.Json.JsonConvert.SerializeObject(values);
    }
}
