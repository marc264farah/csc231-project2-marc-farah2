using UnityEngine;

namespace NeonVeil
{
    public class QuarantineOutbreakMoment : MonoBehaviour
    {
        [SerializeField] private ChapterGame game;
        [SerializeField] private ChapterUI ui;
        [SerializeField] private Transform player;
        [SerializeField] private AudioSource alarm;
        [SerializeField] private SpriteRenderer[] warningLights;
        [SerializeField] private CanvasGroup purgeAlert;
        [SerializeField] private float triggerX = 438f;
        [SerializeField] private float warningDuration = 4.2f;

        private Color[] normalColors;
        private float warningEndsAt;
        private bool triggered;
        private bool armed = true;

        private void Awake()
        {
            normalColors = new Color[warningLights.Length];
            for (int i = 0; i < warningLights.Length; i++)
                normalColors[i] = warningLights[i].color;
            if (purgeAlert != null)
                purgeAlert.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!armed && player.position.x < triggerX - 4f)
                armed = true;

            if (!triggered && armed && game.CanPlay && player.position.x >= triggerX)
                TriggerOutbreak();

            if (!triggered)
                return;

            if (!game.CanPlay)
            {
                ClearWarning();
                if (game.State != ChapterGame.GameState.Paused)
                    triggered = false;
                return;
            }

            if (alarm != null && NeonAudio.Instance != null)
                alarm.volume = NeonAudio.Instance.Volume * 0.45f;

            if (Time.time < warningEndsAt)
            {
                float flash = Mathf.PingPong(Time.time * 5f, 1f);
                for (int i = 0; i < warningLights.Length; i++)
                    warningLights[i].color = Color.Lerp(normalColors[i],
                        new Color(1f, 0.18f, 0.25f, normalColors[i].a), flash);
                if (purgeAlert != null)
                {
                    purgeAlert.gameObject.SetActive(true);
                    purgeAlert.alpha = 0.85f + 0.15f * flash;
                }
            }
            else
            {
                ClearWarning();
                ui.ShowSection("04  QUARANTINE ZONE",
                    "Mimics wake nearby and lunge. Keep space for a counterattack.");
                triggered = false;
            }
        }

        private void ClearWarning()
        {
            for (int i = 0; i < warningLights.Length; i++)
                warningLights[i].color = normalColors[i];
            if (purgeAlert != null)
                purgeAlert.gameObject.SetActive(false);
        }

        private void TriggerOutbreak()
        {
            triggered = true;
            armed = false;
            warningEndsAt = Time.time + warningDuration;
            ui.ShowSection("PURGE PROTOCOL",
                "Containment failed. Mimics released.");
            if (purgeAlert != null)
            {
                purgeAlert.gameObject.SetActive(true);
                purgeAlert.alpha = 1f;
            }
            if (alarm != null)
            {
                if (NeonAudio.Instance != null)
                    alarm.volume = NeonAudio.Instance.Volume * 0.45f;
                alarm.Play();
            }
        }
    }
}