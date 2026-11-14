using DG.Tweening;
using UnityEngine;

/// <summary>
/// Global switch for visual effects (tweens, shakes, splashes, popups). Effects never carry game state: with the
/// switch off every effect is skipped and the screen shows its end state right away. Turning it off mid-effect
/// completes every running effect tween. Persisted in PlayerPrefs.
/// </summary>
public static class Feel
{
    /// <summary>Id given to every effect tween, so all of them can be completed at once.</summary>
    public const string TweenId = "feel";

    private const string PrefKey = "fisker.feel";
    private static bool? _enabled;

    public static bool Enabled
    {
        get => _enabled ??= PlayerPrefs.GetInt(PrefKey, 1) == 1;
        set
        {
            _enabled = value;
            PlayerPrefs.SetInt(PrefKey, value ? 1 : 0);
            if (!value) CompleteAll();
        }
    }

    /// <summary>Jumps every running effect to its end state (pooled objects are released by their callbacks).</summary>
    public static void CompleteAll() => DOTween.Complete(TweenId, true);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache() => _enabled = null;
}
