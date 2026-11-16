using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace NeonVeil
{
    public class ChapterGame : MonoBehaviour
    {
        public enum GameState { Title, Intro, Playing, Paused, Dying, GameOver, Complete, Loading }

        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private Subject09Controller player;
        [SerializeField] private FollowCamera2D cameraFollow;
        [SerializeField] private ChapterUI ui;
        [SerializeField] private Transform startPoint;
        [SerializeField, Min(1)] private int requiredDefeats = 7;

        private InputAction pauseAction;
        private ChapterEnemy[] enemies;
        private SecurityLaser[] lasers;
        private Vector2 checkpoint;
        private int currentSection = 1;
        private int checkpointSection = 1;
        private string currentSectionName = "01  AWAKENING";
        private string currentInstruction = "A / D or arrows  •  Space jump  •  Left click attack";
        private string checkpointSectionName = "01  AWAKENING";
        private string checkpointInstruction = "A / D or arrows  •  Space jump  •  Left click attack";
        private float runStartedAt;
        private float pausedAt;
        private float totalPausedTime;
        private float completedTime;

        public GameState State { get; private set; }
        public bool CanPlay => State == GameState.Playing;
        public int CellCount { get; private set; }
        public int CellTotal { get; private set; }
        public int EnemyDefeats { get; private set; }
        public int RequiredDefeats => requiredDefeats;
        public int CurrentSection => currentSection;
        public int CheckpointNumber { get; private set; }
        public float CompletionTime => completedTime;

        private void Awake()
        {
            pauseAction = inputActions.FindAction("Game/Pause", true);
            checkpoint = startPoint.position;
            State = GameState.Title;
            Time.timeScale = 0f;
        }

        private void Start()
        {
            enemies = FindObjectsByType<ChapterEnemy>();
            lasers = FindObjectsByType<SecurityLaser>();
            CellTotal = FindObjectsByType<EnergyCell>().Length;
            ui.Show(State);
        }

        private void OnEnable()
        {
            pauseAction?.Enable();
        }

        private void OnDisable()
        {
            pauseAction?.Disable();
            Time.timeScale = 1f;
        }

        private void Update()
        {
            if (pauseAction.WasPressedThisFrame())
            {
                if (State == GameState.Playing) Pause();
                else if (State == GameState.Paused) Resume();
            }
        }

        public void StartChapter()
        {
            if (State != GameState.Title) return;
            SetState(GameState.Intro, 0f);
        }

        public void BeginLevel()
        {
            if (State != GameState.Intro) return;
            runStartedAt = Time.unscaledTime;
            SetState(GameState.Playing, 1f);
        }
        public void Pause()
        {
            if (!CanPlay) return;
            pausedAt = Time.unscaledTime;
            SetState(GameState.Paused, 0f);
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            totalPausedTime += Time.unscaledTime - pausedAt;
            SetState(GameState.Playing, 1f);
        }

        public void SetSection(int number, string section, string instruction)
        {
            if (!CanPlay || number == currentSection) return;
            currentSection = number;
            currentSectionName = section;
            currentInstruction = instruction;
            ui.ShowSection(section, instruction);
        }

        public void ActivateCheckpoint(int number, Vector2 position)
        {
            if (!CanPlay || number <= CheckpointNumber) return;
            CheckpointNumber = number;
            checkpoint = position;
            checkpointSection = currentSection;
            checkpointSectionName = currentSectionName;
            checkpointInstruction = currentInstruction;
            ui.ShowCheckpoint();
        }

        public bool CollectCell()
        {
            if (!CanPlay) return false;
            CellCount++;
            NeonAudio.Instance?.Play(NeonAudio.Cue.Collect);
            return true;
        }

        public void RecordEnemyDefeat(ChapterEnemy enemy)
        {
            if (!CanPlay || enemy == null || !enemy.CountsForExtraction) return;
            EnemyDefeats++;
        }

        public void ShowDeath()
        {
            if (!CanPlay) return;
            SetState(GameState.Dying, 1f);
            StartCoroutine(ShowGameOver());
        }

        private IEnumerator ShowGameOver()
        {
            yield return new WaitForSeconds(0.55f);
            pausedAt = Time.unscaledTime;
            SetState(GameState.GameOver, 0f);
        }

        public void RespawnAtCheckpoint()
        {
            if (State != GameState.GameOver) return;
            totalPausedTime += Time.unscaledTime - pausedAt;
            foreach (ChapterEnemy enemy in enemies)
                if (enemy != null && !enemy.IsDefeated)
                    enemy.ResetEncounter();
            foreach (SecurityLaser laser in lasers)
                laser.ResetCycle();

            player.Revive(checkpoint);
            cameraFollow.SnapToTarget();
            currentSection = checkpointSection;
            currentSectionName = checkpointSectionName;
            currentInstruction = checkpointInstruction;
            ui.ShowSection(currentSectionName, currentInstruction);
            SetState(GameState.Playing, 1f);
        }

        public bool CompleteChapter()
        {
            if (!CanPlay) return false;
            if (EnemyDefeats < requiredDefeats)
            {
                int remaining = requiredDefeats - EnemyDefeats;
                ui.ShowSection(currentSectionName, $"EXTRACTION LOCKED  •  Defeat {remaining} more enem{(remaining == 1 ? "y" : "ies")}");
                return false;
            }
            completedTime = Time.unscaledTime - runStartedAt - totalPausedTime;
            NeonAudio.Instance?.Play(NeonAudio.Cue.Win);
            SetState(GameState.Complete, 0f);
            return true;
        }

        public void RestartChapter()
        {
            if (State == GameState.Loading) return;
            SetState(GameState.Loading, 1f);
            Scene scene = gameObject.scene;
#if UNITY_EDITOR
            if (scene.buildIndex < 0)
            {
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                    scene.path, new LoadSceneParameters(LoadSceneMode.Single));
                return;
            }
#endif
            SceneManager.LoadSceneAsync(scene.buildIndex, LoadSceneMode.Single);
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SetState(GameState next, float timeScale)
        {
            State = next;
            Time.timeScale = timeScale;
            ui.Show(next);
        }
    }
}
