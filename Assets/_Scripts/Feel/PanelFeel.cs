using DG.Tweening;
using UnityEngine;

/// <summary>
/// Fades a menu canvas in when it is shown and pops its panel (a child named "...Panel", not the dimming
/// "BlurPanel") from slightly smaller. Showing and hiding stay with the game code (SetActive); this only animates
/// the entrance. Disabled, or with Feel.Enabled off, the canvas sits at full alpha and the panel at rest scale.
/// Runs on unscaled time (pause and game over stop time).
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class PanelFeel : MonoBehaviour
{
    private CanvasGroup _group;
    private Transform _panel;
    private Vector3 _panelRest;

    private void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        foreach (var child in GetComponentsInChildren<RectTransform>(true))
        {
            if (child.name.EndsWith("Panel") && child.name != "BlurPanel")
            {
                _panel = child;
                _panelRest = child.localScale;
                break;
            }
        }
    }

    private void OnEnable()
    {
        Rest();
        if (!Feel.Enabled) return;

        _group.alpha = 0f;
        _group.DOFade(1f, 0.2f).SetUpdate(true).SetId(Feel.TweenId);
        if (_panel != null)
        {
            _panel.localScale = _panelRest * 0.85f;
            _panel.DOScale(_panelRest, 0.25f).SetEase(Ease.OutBack).SetUpdate(true).SetId(Feel.TweenId);
        }
    }

    private void OnDisable() => Rest();

    private void Rest()
    {
        _group.DOKill();
        _group.alpha = 1f;
        if (_panel == null) return;
        _panel.DOKill();
        _panel.localScale = _panelRest;
    }
}
