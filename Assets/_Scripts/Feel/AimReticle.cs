using UnityEngine;

/// <summary>
/// The aim under the pointer while playing over the water, drawn from game state every frame (not an effect: it
/// ignores Feel.Enabled). Bright when a click throws a spear, faint while a spear is still out. It replaces the
/// system cursor over the water; outside it (the HUD, menus) it hides and the cursor shows as usual.
/// </summary>
public class AimReticle : MonoBehaviour
{
    private const int Order = 46; // over the water and fish, under the shore and the HUD
    private const float ReadyAlpha = 0.9f;
    private const float BlockedAlpha = 0.3f;

    private SpriteRenderer _sprite;
    private SpearManager _spears;

    public static AimReticle Create(Sprite sprite, Vector3 scale, SpearManager spears)
    {
        var reticle = new GameObject("AimReticle").AddComponent<AimReticle>();
        reticle._spears = spears;
        reticle._sprite = reticle.gameObject.AddComponent<SpriteRenderer>();
        reticle._sprite.sprite = sprite;
        reticle._sprite.sortingOrder = Order;
        reticle.transform.localScale = scale;
        reticle.LateUpdate();
        return reticle;
    }

    private void LateUpdate()
    {
        var pointer = SpearManager.PointerWorld();
        bool playing = GameManager.Instance.GameState == GameState.InGame && _spears != null && pointer.HasValue;
        bool overWater = playing && _spears.InThrowZone(pointer.Value);
        _sprite.enabled = overWater;
        Cursor.visible = !overWater;
        if (!overWater) return;

        transform.position = pointer.Value;
        var colour = Color.white;
        colour.a = _spears.CanThrow ? ReadyAlpha : BlockedAlpha;
        _sprite.color = colour;
    }

    private void OnDisable() => Cursor.visible = true;
}
