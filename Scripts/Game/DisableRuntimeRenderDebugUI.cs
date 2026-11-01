using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// This game does not use Unity's in-game Rendering Debugger.
/// Disabling it also prevents Core RP from creating its debug canvas in play
/// mode, where an uninitialized MessageBox widget can throw every frame.
/// </summary>
public static class DisableRuntimeRenderDebugUI
{
    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Disable()
    {
        DebugManager manager =
            DebugManager.instance;

        if (manager == null)
        {
            return;
        }

        manager.displayRuntimeUI = false;
        manager.displayPersistentRuntimeUI = false;
        manager.enableRuntimeUI = false;
    }
}