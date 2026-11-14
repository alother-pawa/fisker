using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Presents gameplay events as effects: water droplets and ripples where the spear lands, the caught fish popping
/// out with a "+1", a short camera shake on a catch, the reticle locking on, the big spear popping back on reload,
/// and HUD punches (score on a catch, time in the last seconds).
/// Holds no game state: each effect starts from the state the game already reached, and with Feel.Enabled off it
/// is skipped. The time turning red in the last seconds is read from GameManager every frame, in both modes.
/// Created at scene load next to the GameManager, together with the sound and UI feel components.
/// </summary>
public class FeelDirector : MonoBehaviour
{
    private const int PixelsPerUnit = 16;
    private const float WarningSeconds = 10f;

    // Sorting: fish draw at 0, the shore tilemap at 50, the spear at 90, the canvases at 100. Water effects stay
    // above the fish and under the shore, so they never cover the HUD.
    private const int RippleOrder = 1;
    private const int DropletOrder = 45;
    private const int PopupOrder = 49;
    private static readonly Color WaterColor = new Color(0.85f, 0.95f, 1f, 0.95f);
    private static readonly Color WarningColor = new Color(1f, 0.35f, 0.3f);

    private Camera _camera;
    private Vector3 _cameraOrigin;
    private Transform _reticle;
    private Vector3 _reticleScale;
    private Transform _bigSpear;
    private Vector3 _bigSpearScale;
    private Transform _score;
    private TMP_Text _time;
    private Color _timeColor;
    private int _lastWarnedSecond = -1;

    private Sprite _dot;
    private Sprite _ring;
    private readonly List<SpriteRenderer> _sprites = new List<SpriteRenderer>();
    private readonly List<TextMeshPro> _popups = new List<TextMeshPro>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (FindAnyObjectByType<GameManager>(FindObjectsInactive.Include) == null) return;

        var root = new GameObject("----Feel----");
        root.AddComponent<FeelDirector>();
        root.AddComponent<SoundDirector>();

        foreach (var button in FindObjectsByType<Button>(FindObjectsInactive.Include))
            button.gameObject.AddComponent<ButtonFeel>();

        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            if (canvas.transform.parent != null && canvas.transform.parent.name == "----Canvases----" && canvas.name != "InGame")
                canvas.gameObject.AddComponent<PanelFeel>();
    }

    private void Awake()
    {
        // A catch runs about 60 tweens (16 droplets); room for a few overlapping without DOTween resizing.
        DOTween.SetTweensCapacity(500, 200);
        _dot = MakeSprite(3, (x, y, c) => true);
        _ring = MakeSprite(24, (x, y, c) =>
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
            return d <= c + 0.5f && d >= c - 1.5f;
        });
    }

    private void Start()
    {
        _camera = Camera.main;
        if (_camera != null) _cameraOrigin = _camera.transform.position;

        // Only the sprite children are scaled: the DestinationPoint collider decides hits and must keep its size.
        var destination = FindAnyObjectByType<DestinationPoint>(FindObjectsInactive.Include);
        var reticle = destination != null ? destination.GetComponentInChildren<SpriteRenderer>(true) : null;
        if (reticle != null) { _reticle = reticle.transform; _reticleScale = _reticle.localScale; }

        var bigSpear = FindAnyObjectByType<BigSpear>(FindObjectsInactive.Include);
        var spearModel = bigSpear != null ? bigSpear.GetComponentInChildren<SpriteRenderer>(true) : null;
        if (spearModel != null) { _bigSpear = spearModel.transform; _bigSpearScale = _bigSpear.localScale; }

        var score = FindAnyObjectByType<ScoreText>(FindObjectsInactive.Include);
        if (score != null && score.Text != null) _score = score.Text.transform;

        var time = FindAnyObjectByType<TimeText>(FindObjectsInactive.Include);
        if (time != null && time.Text != null) { _time = time.Text; _timeColor = _time.color; }
    }

    private void OnEnable()
    {
        FiskerEvents.StateChanged += OnStateChanged;
        FiskerEvents.SpearThrown += OnSpearThrown;
        FiskerEvents.SpearReloaded += OnSpearReloaded;
        FiskerEvents.FishCaught += OnFishCaught;
        FiskerEvents.SpearMissed += OnSpearMissed;
    }

    private void OnDisable()
    {
        FiskerEvents.StateChanged -= OnStateChanged;
        FiskerEvents.SpearThrown -= OnSpearThrown;
        FiskerEvents.SpearReloaded -= OnSpearReloaded;
        FiskerEvents.FishCaught -= OnFishCaught;
        FiskerEvents.SpearMissed -= OnSpearMissed;
    }

    private void LateUpdate()
    {
        if (_time == null) return;

        // Runs after GameManager wrote the text this frame; the colour follows the remaining time in both modes.
        bool inGame = GameManager.Instance.GameState == GameState.InGame;
        float remaining = GameManager.Instance.Time;
        bool warning = inGame && remaining <= WarningSeconds;
        _time.color = warning ? WarningColor : _timeColor;

        int second = Mathf.CeilToInt(remaining);
        if (!warning || second == _lastWarnedSecond) return;
        _lastWarnedSecond = second;
        if (Feel.Enabled) Punch(_time.transform, 0.25f);
    }

    private void OnStateChanged(GameState state)
    {
        if (state == GameState.InGame || state == GameState.Pause) return;

        // Leaving play: end every running effect so nothing lingers over the menus.
        Feel.CompleteAll();
        _lastWarnedSecond = -1;
    }

    private void OnSpearThrown(Vector3 target)
    {
        if (!Feel.Enabled || _reticle == null) return;
        _reticle.DOKill(true);
        _reticle.localScale = _reticleScale * 1.6f;
        _reticle.DOScale(_reticleScale, 0.2f).SetEase(Ease.OutQuad).SetId(Feel.TweenId)
            .OnKill(() => _reticle.localScale = _reticleScale);
    }

    private void OnSpearReloaded()
    {
        if (!Feel.Enabled || _bigSpear == null) return;
        _bigSpear.DOKill(true);
        _bigSpear.localScale = _bigSpearScale * 0.5f;
        _bigSpear.DOScale(_bigSpearScale, 0.25f).SetEase(Ease.OutBack).SetId(Feel.TweenId)
            .OnKill(() => _bigSpear.localScale = _bigSpearScale);
    }

    private void OnSpearMissed(Vector3 point)
    {
        if (!Feel.Enabled) return;
        Splash(point, 8, 0.6f);
        Ripple(point, 0f, 1.2f);
    }

    private void OnFishCaught(Vector3 point, Transform fish)
    {
        if (!Feel.Enabled) return;
        Splash(point, 16, 1f);
        Ripple(point, 0f, 1.6f);
        Ripple(point, 0.1f, 1.1f);
        if (fish != null) FishPop(fish);
        Popup("+1", point);
        Shake();
        if (_score != null) Punch(_score, 0.35f);
    }

    private void Splash(Vector3 point, int count, float size)
    {
        for (int i = 0; i < count; i++)
        {
            var drop = TakeSprite(_dot, WaterColor, DropletOrder);
            drop.transform.position = point;
            var target = point + (Vector3)(Random.insideUnitCircle.normalized * Random.Range(0.4f, 1.1f) * size);
            float duration = Random.Range(0.35f, 0.5f);
            DOTween.Sequence().SetId(Feel.TweenId)
                .Join(drop.transform.DOJump(target, Random.Range(0.4f, 0.9f) * size, 1, duration))
                .Insert(duration * 0.5f, drop.DOFade(0f, duration * 0.5f))
                .OnComplete(() => Release(drop));
        }
    }

    private void Ripple(Vector3 point, float delay, float size)
    {
        var ring = TakeSprite(_ring, WaterColor, RippleOrder);
        ring.transform.position = point;
        ring.transform.localScale = Vector3.one * 0.3f;
        ring.enabled = delay <= 0f;
        DOTween.Sequence().SetId(Feel.TweenId)
            .AppendInterval(delay)
            .AppendCallback(() => ring.enabled = true)
            .Append(ring.transform.DOScale(size, 0.5f).SetEase(Ease.OutQuad))
            .Join(ring.DOFade(0f, 0.5f).SetEase(Ease.InQuad))
            .OnComplete(() => Release(ring));
    }

    private void FishPop(Transform fish)
    {
        var source = fish.GetComponentInChildren<SpriteRenderer>(true);
        if (source == null || source.sprite == null) return;

        var copy = TakeSprite(source.sprite, source.color, source.sortingOrder + 2);
        copy.flipX = source.flipX;
        copy.flipY = source.flipY;
        copy.sortingLayerID = source.sortingLayerID;
        copy.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
        var scale = source.transform.lossyScale;
        copy.transform.localScale = scale;

        DOTween.Sequence().SetId(Feel.TweenId)
            .Append(copy.transform.DOScale(scale * 1.3f, 0.1f).SetEase(Ease.OutQuad))
            .Append(copy.transform.DOMove(copy.transform.position + Vector3.up * 0.6f, 0.35f).SetEase(Ease.OutQuad))
            .Join(copy.transform.DOScale(Vector3.zero, 0.35f).SetEase(Ease.InBack))
            .Join(copy.DOFade(0f, 0.35f))
            .OnComplete(() => Release(copy));
    }

    private void Popup(string text, Vector3 point)
    {
        var popup = TakePopup();
        popup.text = text;
        popup.alpha = 1f;
        popup.transform.position = point + Vector3.up * 0.4f;
        DOTween.Sequence().SetId(Feel.TweenId)
            .Append(popup.transform.DOMove(popup.transform.position + Vector3.up * 1f, 0.6f).SetEase(Ease.OutCubic))
            .Insert(0.3f, DOTween.To(() => popup.alpha, a => popup.alpha = a, 0f, 0.3f))
            .OnComplete(() => popup.gameObject.SetActive(false));
    }

    private void Shake()
    {
        if (_camera == null) return;
        var t = _camera.transform;
        t.DOKill(true);
        t.DOShakePosition(0.18f, new Vector3(0.15f, 0.15f, 0f), 25, 90f, false, true).SetId(Feel.TweenId)
            .OnKill(() => t.position = _cameraOrigin);
    }

    private static void Punch(Transform target, float strength)
    {
        // Completing the previous punch puts the scale back at rest before reading it.
        target.DOKill(true);
        var rest = target.localScale;
        target.DOPunchScale(rest * strength, 0.25f, 6, 0.5f).SetId(Feel.TweenId)
            .OnKill(() => target.localScale = rest);
    }

    private SpriteRenderer TakeSprite(Sprite sprite, Color color, int order)
    {
        SpriteRenderer renderer = _sprites.Find(s => !s.gameObject.activeSelf);
        if (renderer == null)
        {
            renderer = new GameObject("Fx").AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(transform, false);
            _sprites.Add(renderer);
        }
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        renderer.flipX = renderer.flipY = false;
        renderer.enabled = true;
        renderer.transform.rotation = Quaternion.identity;
        renderer.transform.localScale = Vector3.one;
        renderer.gameObject.SetActive(true);
        return renderer;
    }

    private static void Release(SpriteRenderer renderer) => renderer.gameObject.SetActive(false);

    private TextMeshPro TakePopup()
    {
        TextMeshPro popup = _popups.Find(p => !p.gameObject.activeSelf);
        if (popup == null)
        {
            popup = new GameObject("Popup").AddComponent<TextMeshPro>();
            popup.transform.SetParent(transform, false);
            popup.fontSize = 8f;
            popup.fontStyle = FontStyles.Bold;
            popup.alignment = TextAlignmentOptions.Center;
            popup.color = new Color(1f, 0.95f, 0.6f);
            popup.outlineWidth = 0.25f;
            popup.outlineColor = new Color32(40, 30, 30, 255);
            popup.sortingOrder = PopupOrder;
            popup.rectTransform.sizeDelta = new Vector2(3f, 1.5f);
            _popups.Add(popup);
        }
        popup.gameObject.SetActive(true);
        return popup;
    }

    /// <summary>Builds a white point-filtered sprite of size x size pixels; the predicate chooses the opaque pixels.</summary>
    private static Sprite MakeSprite(int size, System.Func<int, int, float, bool> opaque)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        float centre = (size - 1) / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                texture.SetPixel(x, y, opaque(x, y, centre) ? Color.white : Color.clear);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }
}
