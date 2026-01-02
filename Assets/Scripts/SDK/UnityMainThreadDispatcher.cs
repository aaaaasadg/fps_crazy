using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Helper to execute actions on the main Unity thread with optional delays.
/// Used for simulating async operations in DummySDK and handling callbacks.
/// </summary>
public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher instance;
    public static UnityMainThreadDispatcher Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("UnityMainThreadDispatcher");
                instance = go.AddComponent<UnityMainThreadDispatcher>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    /// <summary>
    /// Enqueue an action to be executed on the main thread after a delay
    /// </summary>
    public void Enqueue(Action action, float delay = 0f)
    {
        if (delay <= 0f)
        {
            action?.Invoke();
        }
        else
        {
            StartCoroutine(DelayedAction(action, delay));
        }
    }

    private IEnumerator DelayedAction(Action action, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        action?.Invoke();
    }
}

