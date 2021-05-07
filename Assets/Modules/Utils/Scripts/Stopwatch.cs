// ------------------------------------------------------------------------------------
// <copyright file="Stopwatch.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

/// <summary>
/// Utility class to serve as a stopwatch. It allows to start several named stopwatches.
/// </summary>
public class Stopwatch
{
    /// <summary>
    /// instance reference for the singleton pattern.
    /// </summary>
    public static Stopwatch Instance = null;

    private readonly Stack<Tuple<System.Diagnostics.Stopwatch, string>> stopwatchStack;

    private Stopwatch()
    {
        stopwatchStack = new Stack<Tuple<System.Diagnostics.Stopwatch, string>>();
    }

    /// <summary>
    /// Starts a new stopwatch with the provided identifier.
    /// </summary>
    /// <param name="name">The name of the new stopwatch.</param>
    public static void Start(string name)
    {
        if (Instance == null)
        {
            Instance = new Stopwatch();
        }

        System.Diagnostics.Stopwatch newStopwatch = new System.Diagnostics.Stopwatch();
        Instance.stopwatchStack.Push(new Tuple<System.Diagnostics.Stopwatch, string>(newStopwatch, name));
        newStopwatch.Start();
    }

    /// <summary>
    /// Stops the stopwatch that was started last.
    /// </summary>
    public static void Stop()
    {
        if (Instance == null || Instance.stopwatchStack.Count == 0)
        {
            UnityEngine.Debug.LogWarning("Stopwatch.Stop() used without corresponding Stopwatch.Start(string)");
            return;
        }

        var tuple = Instance.stopwatchStack.Pop();
        tuple.Item1.Stop();
        UnityEngine.Debug.Log("STOPWATCH: " + tuple.Item2 + ": " + tuple.Item1.Elapsed);
    }
}