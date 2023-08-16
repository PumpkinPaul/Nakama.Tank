// Copyright Pumpkin Games Ltd. All Rights Reserved.

using Microsoft.Xna.Framework;

namespace NakamaTank.Engine.Extensions;

public static class Vector2Extensions
{
    public static Vector2 Rotate(this Vector2 vector, float radians)
    {
        return Vector2.Transform(vector, Matrix.CreateRotationZ(radians));
    }
}
