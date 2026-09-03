using System;
using System.Collections.Generic;
using UnityEngine;

namespace EightBall.Audio
{
    /// <summary>
    /// Plays the game's sound effects by id from a pooled set of AudioSources. Created on the
    /// Table object by <see cref="TableSetup"/>; gameplay code only calls
    /// <see cref="Play"/> — it never touches clips or sources.
    /// Sounds are declared in <see cref="DefaultSounds"/> and their clips are loaded from
    /// <c>Resources/SFX</c> by file name, so replacing a sound means dropping a new file with
    /// the same name into that folder — no code or scene change.
    /// </summary>
    public class SfxManager : MonoBehaviour
    {
        [Serializable]
        private sealed class Sound
        {
            public string Id;
            public string[] ClipNames = Array.Empty<string>();
            public float Volume = 1f;
            /// <summary>Full pitch range is ±this factor, drawn per play so repeats vary.</summary>
            public float PitchVariation = 0.04f;
            /// <summary>Minimum seconds between two plays of this sound (throttles bursts).</summary>
            public float MinInterval;

            public AudioClip[] Clips;
            public float LastPlayedAt = float.NegativeInfinity;
        }

        /// <summary>AudioSources created on demand, capped so a burst cannot spawn dozens.</summary>
        private const int MaxSources = 12;

        private static SfxManager _instance;

        private readonly Dictionary<string, Sound> _sounds = new Dictionary<string, Sound>();
        private readonly List<AudioSource> _sources = new List<AudioSource>();
        private int _nextSource;

        public static void Play(string id, float volumeScale = 1f)
        {
            if (_instance == null) return;

            _instance.PlayInstance(id, volumeScale);
        }

        private void Awake()
        {
            _instance = this;

            foreach (Sound sound in DefaultSounds()) AddSound(sound);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void PlayInstance(string id, float volumeScale)
        {
            if (!_sounds.TryGetValue(id, out Sound sound))
            {
                Debug.LogWarning($"[SfxManager] Missing SFX id: {id}", this);
                return;
            }

            if (sound.Clips.Length == 0) return;
            if (Time.unscaledTime - sound.LastPlayedAt < sound.MinInterval) return;

            AudioClip clip = sound.Clips[UnityEngine.Random.Range(0, sound.Clips.Length)];
            if (clip == null) return;

            AudioSource source = GetSource();
            float pitch = 1f + UnityEngine.Random.Range(-sound.PitchVariation, sound.PitchVariation);
            source.pitch = pitch;
            source.volume = sound.Volume * Mathf.Clamp01(volumeScale);
            source.clip = clip;
            source.Play();

            sound.LastPlayedAt = Time.unscaledTime;
        }

        /// <summary>Round-robin over the pool, stealing the oldest source once all are busy.</summary>
        private AudioSource GetSource()
        {
            foreach (AudioSource source in _sources)
            {
                if (!source.isPlaying) return source;
            }

            if (_sources.Count < MaxSources)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f; // 2D — the table is always on screen
                _sources.Add(source);
                return source;
            }

            AudioSource stolen = _sources[_nextSource];
            _nextSource = (_nextSource + 1) % _sources.Count;
            return stolen;
        }

        private void AddSound(Sound sound)
        {
            sound.Clips = new AudioClip[sound.ClipNames.Length];
            for (int i = 0; i < sound.ClipNames.Length; i++)
            {
                sound.Clips[i] = Resources.Load<AudioClip>($"SFX/{sound.ClipNames[i]}");
                if (sound.Clips[i] == null)
                {
                    Debug.LogWarning($"[SfxManager] Missing clip Resources/SFX/{sound.ClipNames[i]} for '{sound.Id}'.", this);
                }
            }

            _sounds[sound.Id] = sound;
        }

        /// <summary>The game's sound table. Ids are what gameplay code plays; clip names map to
        /// files in Resources/SFX. More than one clip per sound = random variation per play.</summary>
        private static IEnumerable<Sound> DefaultSounds()
        {
            return new[]
            {
                new Sound { Id = "BallCollision", ClipNames = new[] { "ball_collision" }, MinInterval = 0.03f },
                new Sound { Id = "CushionCollision", ClipNames = new[] { "cushion_collision" }, MinInterval = 0.03f },
                new Sound { Id = "CueCollisionWeak", ClipNames = new[] { "cue_collision_weak" } },
                new Sound { Id = "CueCollisionStrong", ClipNames = new[] { "cue_collision_strong" } },
                new Sound { Id = "Pocket", ClipNames = new[] { "pocket" } },
                new Sound { Id = "YourTurn", ClipNames = new[] { "your_turn" } },
                new Sound { Id = "Foul", ClipNames = new[] { "foul" } },
                new Sound { Id = "Cheering", ClipNames = new[] { "cheering" } },
            };
        }
    }
}
