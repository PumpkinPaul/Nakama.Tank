// Copyright Pumpkin Games Ltd. All Rights Reserved.

namespace NakamaTank.Engine.DebugTools;

public class DebugEchoListner : IDebugEchoListner
{
    public void Echo(DebugCommandMessage messageType, string text)
    {
        System.Diagnostics.Debug.WriteLine($"{messageType}\t{text}");
    }
}