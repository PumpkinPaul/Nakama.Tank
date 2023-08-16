// Copyright Pumpkin Games Ltd. All Rights Reserved.

using System;
using System.Diagnostics;

namespace NakamaTank.Engine;

public static class Logger
{
    public static void WriteLine(string value) => WriteLineInternal(value);

    static void WriteLineInternal(string value)
    {
        var timestamp = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.ffff}";
        Console.WriteLine($"{timestamp} - {value}");
        Debug.WriteLine($"{timestamp} - {value}");
    }
}
