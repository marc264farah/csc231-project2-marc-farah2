using UnityEngine;
using UnityEngine.UI;

namespace NeonVeil
{
    public class ChapterUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ChapterGame game;
        [SerializeField] private Subject09Controller player;
        [SerializeField] private Subject09Combat combat;

        [Header("Screens")]
        [SerializeField] private GameObject titlePanel;
        [SerializeField] private GameObject introPanel;
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject completePanel;

        [Header("HUD")]
        [SerializeField] private Text healthLabel;
        [SerializeField] private Text cellLabel;
        [SerializeField] private Text defeatLabel;
        [SerializeField] private Text phaseLabel;
        [SerializeField] private Text sectionLabel;
        [SerializeField] private Text instructionLabel;
        [SerializeField] private Text checkpointLabel;
        [SerializeField] private Text resultLabel;
        [SerializeField] private Slider volume;
        [SerializeField] private Toggle mute;
        [SerializeField] private Text titleAudioLabel;
        [SerializeField] private Text hudAudioLabel;
        [SerializeField] private Text pauseAudioLabel;

        private float hideCheckpointAt;
        private int lastHealth = -1;
        private int lastCells = -1;
        private int lastDefeats = -1;
        private string lastPhaseText;

        private void Start()
        {
            if (NeonAudio.Instance == null) return;
            if (volume != null) volume.SetValueWithoutNotify(NeonAudio.Instance.Volume);
            if (mute != null) mute.SetIsOnWithoutNotify(NeonAudio.Instance.Muted);
            UpdateAudioLabels();
        }

        private void Update()
        {
            if (!hudPanel.activeSelf) return;
            if (lastHealth != player.CurrentHealth)
            {
                lastHealth = player.CurrentHealth;
                healthLabel.text = $"HEALTH  {lastHealth}/{player.MaximumHealth}";
            }

            if (lastCells != game.CellCount)
            {
                lastCells = game.CellCount;
                cellLabel.text = $"ENERGY CELLS  {lastCells}/{game.CellTotal}";
            }

            if (defeatLabel != null && lastDefeats != game.EnemyDefeats)
            {
                lastDefeats = game.EnemyDefeats;
                defeatLabel.text = $"ENEMIES  {lastDefeats}/{game.RequiredDefeats}";
            }

            float remaining = combat.PhaseRemaining;
            string phase = remaining > 0f ? $"PHASE  {remaining:0.0}s" : "PHASE  READY";
            if (lastPhaseText != phase)
            {
                lastPhaseText = phase;
                phaseLabel.text = phase;
            }
            if (checkpointLabel.gameObject.activeSelf && Time.time >= hideCheckpointAt)
                checkpointLabel.gameObject.SetActive(false);
        }

        public void Show(ChapterGame.GameState state)
        {
            titlePanel.SetActive(state == ChapterGame.GameState.Title);
            if (introPanel != null) introPanel.SetActive(state == ChapterGame.GameState.Intro);
            hudPanel.SetActive(state == ChapterGame.GameState.Playing || state == ChapterGame.GameState.Dying);
            pausePanel.SetActive(state == ChapterGame.GameState.Paused);
            gameOverPanel.SetActive(state == ChapterGame.GameState.GameOver);
            completePanel.SetActive(state == ChapterGame.GameState.Complete);
            if (state == ChapterGame.GameState.Complete)
            {
                int seconds = Mathf.FloorToInt(game.CompletionTime);
                resultLabel.text = $"ENEMIES  {game.EnemyDefeats}/{game.RequiredDefeats}    |    CELLS  {game.CellCount}/{game.CellTotal}    |    TIME  {seconds / 60}:{seconds % 60:00}";
            }
        }

        public void ShowSection(string section, string instruction)
        {
            sectionLabel.text = section;
            instructionLabel.text = instruction;
        }

        public void ShowCheckpoint()
        {
            checkpointLabel.gameObject.SetActive(true);
            hideCheckpointAt = Time.time + 2.2f;
        }

        public void SetVolume(float amount)
        {
            NeonAudio.Instance?.SetVolume(amount);
        }

        public void SetMuted(bool muted)
        {
            NeonAudio.Instance?.SetMuted(muted);
            UpdateAudioLabels();
        }
        public void ToggleAudio()
        {
            if (NeonAudio.Instance == null) return;
            SetMuted(!NeonAudio.Instance.Muted);
            if (mute != null) mute.SetIsOnWithoutNotify(NeonAudio.Instance.Muted);
        }

        private void UpdateAudioLabels()
        {
            string label = NeonAudio.Instance != null && NeonAudio.Instance.Muted ? "MUSIC OFF" : "MUSIC ON";
            if (titleAudioLabel != null) titleAudioLabel.text = label;
            if (hudAudioLabel != null) hudAudioLabel.text = label;
            if (pauseAudioLabel != null) pauseAudioLabel.text = label;
        }    }
}
