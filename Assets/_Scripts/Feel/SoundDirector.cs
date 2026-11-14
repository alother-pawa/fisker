using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Plays music, ocean ambience and sound effects from gameplay events. Clips load from Resources/Audio by name; a
/// missing clip is skipped. Independent of Feel.Enabled (sound is not animation).
/// Music: menu theme on the main menu, game theme in play (ducked while paused), stopped with a jingle at game over.
/// Effects: throw, splash on a miss, splash + catch chime on a hit, a tick for each of the last seconds, UI clicks.
/// </summary>
public class SoundDirector : MonoBehaviour
{
    private const float MusicVolume = 0.45f;
    private const float PausedMusicVolume = 0.15f;
    private const float AmbienceVolume = 0.35f;
    private const int TickSeconds = 5;
    private const int PitchedVoices = 6;

    private AudioSource _music;
    private AudioSource _ambience;
    private AudioSource _sfx;
    private readonly AudioSource[] _pitched = new AudioSource[PitchedVoices];
    private int _nextPitched;

    private AudioClip _menuTheme, _gameTheme, _ocean;
    private AudioClip _throw, _splashMiss, _splashHit, _catch, _tick, _gameOver, _click;
    private int _lastTickSecond = -1;

    private void Awake()
    {
        _music = CreateSource(true);
        _ambience = CreateSource(true);
        _sfx = CreateSource(false);
        for (int i = 0; i < _pitched.Length; i++) _pitched[i] = CreateSource(false);

        _menuTheme = Load("music-menu");
        _gameTheme = Load("music-game");
        _ocean = Load("ambience-ocean");
        _throw = Load("sfx-throw");
        _splashMiss = Load("sfx-splash-miss");
        _splashHit = Load("sfx-splash-hit");
        _catch = Load("sfx-catch");
        _tick = Load("sfx-tick");
        _gameOver = Load("sfx-gameover");
        _click = Load("ui-click");
    }

    private void Start()
    {
        foreach (var button in FindObjectsByType<Button>(FindObjectsInactive.Include))
            button.onClick.AddListener(() => Play(_click, 0.8f));

        if (_ocean != null)
        {
            _ambience.clip = _ocean;
            _ambience.volume = AmbienceVolume;
            _ambience.Play();
        }
        OnStateChanged(GameManager.Instance.GameState);
    }

    private void OnEnable()
    {
        FiskerEvents.StateChanged += OnStateChanged;
        FiskerEvents.SpearThrown += OnSpearThrown;
        FiskerEvents.FishCaught += OnFishCaught;
        FiskerEvents.SpearMissed += OnSpearMissed;
    }

    private void OnDisable()
    {
        FiskerEvents.StateChanged -= OnStateChanged;
        FiskerEvents.SpearThrown -= OnSpearThrown;
        FiskerEvents.FishCaught -= OnFishCaught;
        FiskerEvents.SpearMissed -= OnSpearMissed;
    }

    private void Update()
    {
        if (GameManager.Instance.GameState != GameState.InGame) return;

        int second = Mathf.CeilToInt(GameManager.Instance.Time);
        if (second > TickSeconds || second <= 0 || second == _lastTickSecond) return;
        _lastTickSecond = second;
        Play(_tick, 0.7f);
    }

    private void OnStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.MainMenu:
                PlayMusic(_menuTheme);
                break;
            case GameState.InGame:
                PlayMusic(_gameTheme);
                _music.volume = MusicVolume;
                break;
            case GameState.Pause:
                _music.volume = PausedMusicVolume;
                break;
            case GameState.GameOver:
                _music.Stop();
                _music.clip = null;
                Play(_gameOver, 0.9f);
                break;
            case GameState.Restart:
                _lastTickSecond = -1;
                break;
        }
    }

    private void OnSpearThrown(Vector3 target) => Play(_throw, 0.6f, 0.08f);

    private void OnFishCaught(Vector3 point, Transform fish)
    {
        Play(_splashHit, 0.9f, 0.08f);
        Play(_catch, 0.7f);
    }

    private void OnSpearMissed(Vector3 point) => Play(_splashMiss, 0.7f, 0.1f);

    /// <summary>Starts a music loop unless that clip is already playing (resume from pause keeps its position).</summary>
    private void PlayMusic(AudioClip clip)
    {
        if (clip == null || (_music.clip == clip && _music.isPlaying)) return;
        _music.clip = clip;
        _music.volume = MusicVolume;
        _music.Play();
    }

    private void Play(AudioClip clip, float volume, float pitchJitter = 0f)
    {
        if (clip == null) return;
        if (pitchJitter > 0f)
        {
            // Pitched sounds rotate over their own sources, so a new pitch never bends a sound still playing
            // (unless more than PitchedVoices overlap).
            var source = _pitched[_nextPitched];
            _nextPitched = (_nextPitched + 1) % _pitched.Length;
            source.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            source.PlayOneShot(clip, volume);
            return;
        }
        _sfx.PlayOneShot(clip, volume);
    }

    private AudioSource CreateSource(bool loop)
    {
        var source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        return source;
    }

    private static AudioClip Load(string name) => Resources.Load<AudioClip>("Audio/" + name);
}
