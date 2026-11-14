using System;
using UnityEngine;

/// <summary>
/// Gameplay notifications, raised after the game state has already changed. Listeners only present them
/// (effects, sound) and never feed anything back into the game.
/// </summary>
public static class FiskerEvents
{
    public static event Action<GameState> StateChanged;

    /// <summary>A throw started; the argument is the aimed point on the water.</summary>
    public static event Action<Vector3> SpearThrown;

    /// <summary>The big spear is back in place and a new throw is allowed.</summary>
    public static event Action SpearReloaded;

    /// <summary>The spear hit a fish. Arguments: landing point, the caught fish (already returned to the pool, inactive).</summary>
    public static event Action<Vector3, Transform> FishCaught;

    /// <summary>The spear landed on empty water; the argument is the landing point.</summary>
    public static event Action<Vector3> SpearMissed;

    public static void RaiseStateChanged(GameState state) => StateChanged?.Invoke(state);
    public static void RaiseSpearThrown(Vector3 target) => SpearThrown?.Invoke(target);
    public static void RaiseSpearReloaded() => SpearReloaded?.Invoke();
    public static void RaiseFishCaught(Vector3 point, Transform fish) => FishCaught?.Invoke(point, fish);
    public static void RaiseSpearMissed(Vector3 point) => SpearMissed?.Invoke(point);
}
