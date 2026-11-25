using UnityEngine;

namespace NeonVeil
{
    public class NeonAudio : MonoBehaviour
    {
        public enum Cue { Jump, Land, Collect, Hurt, Win, Attack, Phase, Death }

        [SerializeField] private AudioSource music;
        [SerializeField] private AudioSource effects;
        [SerializeField] private AudioClip jump;
        [SerializeField] private AudioClip land;
        [SerializeField] private AudioClip collect;
        [SerializeField] private AudioClip hurt;
        [SerializeField] private AudioClip death;
        [SerializeField] private AudioClip win;
        [SerializeField] private AudioClip attack;
        [SerializeField] private AudioClip phase;
        [SerializeField] private AudioClip metalImpact;
        [SerializeField] private AudioClip collectResonance;

        private const string VolumeKey = "NeonVeil.Volume";
        private const string MutedKey = "NeonVeil.Muted";

        public static NeonAudio Instance { get; private set; }
        public float Volume { get; private set; }
        public bool Muted { get; private set; }

        private void Awake()
        {
            Instance = this;
            Volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 0.72f));
            Muted = PlayerPrefs.GetInt(MutedKey, 0) != 0;
            ApplySettings();
        }

        private void Start()
        {
            if (music != null && music.clip != null)
            {
                music.time = 0f;
                music.Play();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Play(Cue cue)
        {
            AudioClip clip = cue switch
            {
                Cue.Jump => jump,
                Cue.Land => land,
                Cue.Collect => collect,
                Cue.Hurt => hurt,
                Cue.Death => death,
                Cue.Win => win,
                Cue.Attack => attack,
                Cue.Phase => phase,
                _ => null
            };
            if (cue == Cue.Win && music != null)
                music.volume = Volume * 0.18f;

            if (effects == null || clip == null) return;
            effects.PlayOneShot(clip);
            if ((cue == Cue.Land || cue == Cue.Hurt) && metalImpact != null)
                effects.PlayOneShot(metalImpact, cue == Cue.Land ? 0.32f : 0.26f);
            if (cue == Cue.Collect && collectResonance != null)
                effects.PlayOneShot(collectResonance, 0.42f);
        }

        public void SetVolume(float volume)
        {
            Volume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(VolumeKey, Volume);
            ApplySettings();
        }

        public void SetMuted(bool muted)
        {
            Muted = muted;
            PlayerPrefs.SetInt(MutedKey, Muted ? 1 : 0);
            ApplySettings();
        }

        private void ApplySettings()
        {
            if (music != null)
            {
                music.volume = Volume * 0.85f;
                music.mute = Muted;
            }
            if (effects != null)
            {
                effects.volume = Volume * 0.80f;
                effects.mute = false;
            }
        }
    }
}
