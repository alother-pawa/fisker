using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Scales a button up under the pointer and down while pressed. Purely visual: the scale returns to rest whenever
/// the button is disabled, and with Feel.Enabled off it never leaves rest. Runs on unscaled time (menus show while
/// the game is paused).
/// </summary>
public class ButtonFeel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private const float HoverScale = 1.06f;
    private const float PressScale = 0.94f;

    private Vector3 _rest;
    private bool _hovered;

    private void Awake() => _rest = transform.localScale;

    private void OnDisable()
    {
        transform.DOKill();
        transform.localScale = _rest;
        _hovered = false;
    }

    public void OnPointerEnter(PointerEventData eventData) { _hovered = true; ScaleTo(HoverScale); }
    public void OnPointerExit(PointerEventData eventData) { _hovered = false; ScaleTo(1f); }
    public void OnPointerDown(PointerEventData eventData) => ScaleTo(PressScale);
    public void OnPointerUp(PointerEventData eventData) => ScaleTo(_hovered ? HoverScale : 1f);

    private void ScaleTo(float factor)
    {
        transform.DOKill();
        if (!Feel.Enabled)
        {
            transform.localScale = _rest;
            return;
        }
        transform.DOScale(_rest * factor, 0.1f).SetUpdate(true).SetId(Feel.TweenId);
    }
}
