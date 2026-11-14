"""Synthesizes every sound of Fisker into Assets/Resources/Audio (16-bit mono WAV, 44.1 kHz).

Run from the repository root:  python Tools/AudioGen/generate_sounds.py   (standard library only)

Retro-style sounds built from oscillators and filtered noise; output is byte-identical on every run (fixed seeds).
A final sound replaces a file keeping its name (and Unity GUID). SoundDirector loads them by name:

  music-menu       main menu theme, seamless loop (8 bars, 100 bpm, C major pentatonic)
  music-game       gameplay theme, seamless loop (16 bars, 120 bpm, A minor pentatonic, drums)
  ambience-ocean   ocean swell under everything, seamless loop (8 s)
  sfx-throw        whoosh when a spear is thrown
  sfx-splash-miss  small splash, spear lands on empty water
  sfx-splash-hit   heavy splash with a thud, spear hits a fish
  sfx-catch        rising chime for a catch (plays with sfx-splash-hit)
  sfx-tick         wood tick for each of the last 5 seconds
  sfx-gameover     falling jingle when time runs out
  ui-click         button click
"""
import math
import pathlib
import random
import struct
import wave

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "Assets/Resources/Audio"
RATE = 44100
TAU = 2 * math.pi


# ---------------------------------------------------------------- building blocks

def silence(seconds):
    return [0.0] * int(seconds * RATE)


def midi(note):
    return 440.0 * 2 ** ((note - 69) / 12)


def osc(kind, phase, duty=0.5):
    """One sample of a waveform at phase in [0, 1)."""
    if kind == "sine":
        return math.sin(TAU * phase)
    if kind == "triangle":
        return 4 * abs(phase - 0.5) - 1
    if kind == "square":
        return 1.0 if phase < duty else -1.0
    raise ValueError(kind)


def lowpass(samples, cutoff):
    """One-pole lowpass; cutoff is a number or a function of time (seconds)."""
    out, y = [], 0.0
    for i, x in enumerate(samples):
        fc = cutoff(i / RATE) if callable(cutoff) else cutoff
        a = 1 - math.exp(-TAU * fc / RATE)
        y += a * (x - y)
        out.append(y)
    return out


def noise(seconds, rng):
    return [rng.uniform(-1, 1) for _ in range(int(seconds * RATE))]


def mix(target, source, at=0.0, gain=1.0):
    start = int(at * RATE)
    for i, s in enumerate(source):
        j = start + i
        if j >= len(target):
            break
        target[j] += s * gain


def tone(freq, seconds, kind="triangle", duty=0.5, attack=0.005, decay=8.0, sustain=0.0, release=0.03,
         sweep_to=None):
    """A note with attack, exponential decay towards sustain, and a release at its end. Optional pitch sweep."""
    n = int(seconds * RATE)
    out, phase = [], 0.0
    for i in range(n):
        t = i / RATE
        f = freq if sweep_to is None else freq * (sweep_to / freq) ** (t / seconds)
        phase = (phase + f / RATE) % 1.0
        env = min(1.0, t / attack) if attack > 0 else 1.0
        env *= sustain + (1 - sustain) * math.exp(-t * decay)
        remaining = seconds - t
        if remaining < release:
            env *= remaining / release
        out.append(osc(kind, phase, duty) * env)
    return out


def envelope(samples, attack, decay):
    return [s * min(1.0, (i / RATE) / attack) * math.exp(-(i / RATE) * decay) for i, s in enumerate(samples)]


def normalize(samples, peak):
    top = max(abs(s) for s in samples) or 1.0
    return [s * peak / top for s in samples]


def write(name, samples, peak=0.89):
    OUTPUT.mkdir(parents=True, exist_ok=True)
    samples = normalize(samples, peak)
    path = OUTPUT / f"{name}.wav"
    with wave.open(str(path), "wb") as f:
        f.setnchannels(1)
        f.setsampwidth(2)
        f.setframerate(RATE)
        f.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, s)) * 32767)) for s in samples))
    print(f"{path.relative_to(ROOT)}  {len(samples) / RATE:.2f}s")


# ---------------------------------------------------------------- effects

def throw():
    rng = random.Random(1)
    d = 0.28
    # Band of noise whose centre rises then falls: highpass by subtracting a lower lowpass.
    n = noise(d, rng)
    high = lowpass(n, lambda t: 900 + 3600 * math.sin(math.pi * t / d))
    low = lowpass(n, 500)
    band = [h - l for h, l in zip(high, low)]
    return envelope(band, 0.06, 9)


def splash(seed, length, bright, plop_from, plop_to, thud, drops):
    rng = random.Random(seed)
    out = silence(length)
    body = lowpass(noise(length, rng), lambda t: 900 + bright * math.exp(-t * 10))
    mix(out, envelope(body, 0.004, 8), gain=0.9)
    mix(out, tone(plop_from, 0.09, "sine", decay=20, sweep_to=plop_to), gain=0.7)
    if thud:
        mix(out, tone(95, 0.14, "sine", decay=25, sweep_to=55), gain=0.8)
    for _ in range(drops):
        mix(out, tone(rng.uniform(1300, 2400), 0.04, "sine", decay=60), at=rng.uniform(0.06, length - 0.08),
            gain=rng.uniform(0.15, 0.3))
    return out


def catch():
    out = silence(0.5)
    for k, note in enumerate((88, 91, 96)):  # E6 G6 C7
        last = k == 2
        mix(out, tone(midi(note), 0.32 if last else 0.09, "triangle", decay=6 if last else 20), at=k * 0.07, gain=0.6)
        mix(out, tone(midi(note), 0.32 if last else 0.09, "square", duty=0.25, decay=10 if last else 30),
            at=k * 0.07, gain=0.15)
    return out


def tick():
    rng = random.Random(4)
    out = tone(1400, 0.08, "sine", attack=0.001, decay=60)
    mix(out, tone(2800, 0.05, "sine", attack=0.001, decay=90), gain=0.4)
    mix(out, envelope(noise(0.01, rng), 0.0005, 300), gain=0.3)
    return out


def gameover():
    out = silence(1.5)
    notes = ((79, 0.0, 0.22), (76, 0.2, 0.22), (72, 0.4, 0.22), (67, 0.62, 0.85))  # G5 E5 C5 G4
    for note, at, length in notes:
        mix(out, tone(midi(note), length, "triangle", decay=3, sustain=0.3, release=0.15), at=at, gain=0.6)
        mix(out, tone(midi(note - 12), length, "square", duty=0.25, decay=4, sustain=0.2, release=0.15), at=at,
            gain=0.12)
    return lowpass(out, 5000)


def click():
    rng = random.Random(5)
    out = tone(1800, 0.05, "square", duty=0.3, attack=0.001, decay=70)
    mix(out, envelope(noise(0.008, rng), 0.0005, 400), gain=0.4)
    return lowpass(out, 6000)


# ---------------------------------------------------------------- loops

def ocean():
    """Brown noise swelling twice per loop, with hiss at the crest. Crossfaded tail makes the noise seamless."""
    rng = random.Random(6)
    loop, fade = 8.0, 1.0
    raw, y = [], 0.0
    for _ in range(int((loop + fade) * RATE)):
        y = 0.995 * y + rng.uniform(-1, 1) * 0.05  # leaky integrator: brown noise
        raw.append(y)
    body = lowpass(raw, 700)
    hiss = lowpass([rng.uniform(-1, 1) for _ in raw], 3000)
    out = []
    for i in range(len(raw)):
        t = (i / RATE) % loop
        swell = 0.5 + 0.5 * math.sin(TAU * t / (loop / 2)) ** 2 * (0.7 + 0.3 * math.sin(TAU * t / loop))
        out.append(body[i] * (0.35 + 0.65 * swell) * 4 + hiss[i] * swell ** 3 * 0.12)
    n, f = int(loop * RATE), int(fade * RATE)
    for i in range(f):  # equal-power crossfade of the extra tail into the head
        w = i / f
        out[i] = out[i] * math.sin(w * math.pi / 2) + out[n + i] * math.cos(w * math.pi / 2)
    return out[:n]


def song(bpm, chords, melody, lead_kind, drums, bass_eighths):
    """chords: one root (MIDI) per bar, minor or major chosen by the third given: (root, third).
    melody: list of (note or None, beats). Returns a buffer of exactly len(chords) bars."""
    beat = 60.0 / bpm
    bar = 4 * beat
    out = silence(bar * len(chords))
    rng = random.Random(7)

    for b, (root, third) in enumerate(chords):
        start = b * bar
        tones = (root, root + third, root + 7, root + 12)
        # Bass
        if bass_eighths:
            for k in range(8):
                note = root - 12 + (12 if k % 2 else 0)
                mix(out, tone(midi(note), beat / 2 * 0.9, "square", duty=0.5, decay=6, sustain=0.4), start + k * beat / 2,
                    gain=0.10)
        else:
            mix(out, tone(midi(root - 12), beat * 2 * 0.95, "triangle", decay=1.5, sustain=0.5), start, gain=0.35)
            mix(out, tone(midi(root - 5), beat * 2 * 0.95, "triangle", decay=1.5, sustain=0.5), start + 2 * beat,
                gain=0.35)
        # Arpeggio in eighths
        for k in range(8):
            note = tones[k % 4] + 12
            mix(out, tone(midi(note), beat / 2 * 0.8, "square", duty=0.125, decay=12), start + k * beat / 2, gain=0.05)
        # Drums
        if drums:
            for k in range(4):
                at = start + k * beat
                if k in (0, 2):
                    mix(out, tone(120, 0.16, "sine", attack=0.001, decay=18, sweep_to=45), at, gain=0.45)
                else:
                    mix(out, envelope(lowpass(noise(0.12, rng), 5000), 0.001, 30), at, gain=0.18)
                mix(out, envelope(noise(0.03, rng), 0.0005, 120), at + beat / 2, gain=0.06)

    at = 0.0
    for note, beats in melody:
        if note is not None:
            mix(out, tone(midi(note), beats * beat * 0.92, lead_kind, duty=0.25, decay=2.5, sustain=0.45, release=0.05),
                at, gain=0.22)
        at += beats * beat
    assert abs(at - len(out) / RATE) < 1e-6, "melody length must equal the loop"
    return lowpass(out, 7000)


C, A, F, G = (60, 4), (57, 3), (53, 4), (55, 4)  # (root, third): C major, A minor, F major, G major

MENU_MELODY = [
    (76, 2), (79, 1), (81, 1),
    (79, 1.5), (76, 0.5), (74, 2),
    (72, 1), (74, 1), (76, 1), (79, 1),
    (74, 3), (None, 1),
    (76, 1), (79, 1), (84, 2),
    (81, 1.5), (79, 0.5), (76, 2),
    (74, 1), (76, 1), (79, 1), (76, 1),
    (74, 2), (72, 1.5), (None, 0.5),
]

GAME_PHRASE = [
    (69, 0.5), (72, 0.5), (76, 1), (74, 0.5), (72, 0.5), (69, 1),
    (72, 1), (74, 0.5), (76, 0.5), (79, 1.5), (76, 0.5),
    (76, 0.5), (79, 0.5), (81, 1), (79, 0.5), (76, 0.5), (74, 1),
    (74, 1.5), (76, 0.5), (74, 1), (None, 1),
    (81, 1), (79, 0.5), (76, 0.5), (74, 1), (76, 1),
    (72, 0.5), (74, 0.5), (76, 1), (79, 1), (81, 1),
    (79, 0.5), (76, 0.5), (74, 1), (72, 1), (74, 1),
]
GAME_MELODY = GAME_PHRASE + [(76, 2), (None, 2)] + GAME_PHRASE + [(69, 2), (None, 2)]


def main():
    write("sfx-throw", throw())
    write("sfx-splash-miss", splash(2, 0.4, 3500, 520, 180, False, 3))
    write("sfx-splash-hit", splash(3, 0.55, 2600, 380, 110, True, 6))
    write("sfx-catch", catch())
    write("sfx-tick", tick())
    write("sfx-gameover", gameover())
    write("ui-click", click(), peak=0.7)
    write("ambience-ocean", ocean(), peak=0.7)
    write("music-menu", song(100, [C, A, F, G] * 2, MENU_MELODY, "triangle", drums=False, bass_eighths=False), peak=0.8)
    write("music-game", song(120, [A, F, C, G] * 4, GAME_MELODY, "square", drums=True, bass_eighths=True), peak=0.8)


if __name__ == "__main__":
    main()
