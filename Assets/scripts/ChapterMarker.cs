using UnityEngine;

namespace NeonVeil
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class ChapterMarker : MonoBehaviour
    {
        public enum MarkerKind { Section, Checkpoint, Extraction }

        [SerializeField] private MarkerKind kind;
        [SerializeField] private ChapterGame game;
        [SerializeField] private int number;
        [SerializeField] private string sectionName;
        [SerializeField] private string instruction;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private SpriteRenderer beacon;
        [SerializeField] private SpriteRenderer extractionGate;
        [SerializeField] private SpriteRenderer extractionGlow;

        private Vector3 gateScale;

        private void Awake()
        {
            if (kind != MarkerKind.Extraction) return;
            if (extractionGate == null)
                extractionGate = transform.Find("Painted extraction gate")?.GetComponent<SpriteRenderer>();
            if (extractionGlow == null)
                extractionGlow = transform.Find("Beacon glow")?.GetComponent<SpriteRenderer>();
            if (extractionGate != null) gateScale = extractionGate.transform.localScale;
        }

        private void Update()
        {
            if (kind != MarkerKind.Extraction || extractionGate == null || extractionGlow == null)
                return;

            bool unlocked = game.EnemyDefeats >= game.RequiredDefeats;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * (unlocked ? 4f : 2.2f));
            extractionGate.transform.localScale = gateScale * (1f + 0.045f * pulse);
            extractionGate.color = unlocked ? new Color(0.70f, 1f, 1f) : new Color(1f, 0.68f, 0.58f);
            extractionGlow.color = unlocked
                ? new Color(0.18f, 0.90f, 1f, Mathf.Lerp(0.20f, 0.48f, pulse))
                : new Color(1f, 0.36f, 0.25f, Mathf.Lerp(0.10f, 0.28f, pulse));
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<Subject09Controller>() == null || !game.CanPlay)
                return;

            switch (kind)
            {
                case MarkerKind.Section:
                    game.SetSection(number, sectionName, instruction);
                    break;
                case MarkerKind.Checkpoint:
                    game.ActivateCheckpoint(number, spawnPoint.position);
                    beacon.color = beacon.name == "Painted checkpoint terminal"
                        ? new Color(0.74f, 1f, 0.84f) : new Color(0.32f, 1f, 0.68f);
                    break;
                case MarkerKind.Extraction:
                    game.CompleteChapter();
                    break;
            }
        }
    }
}
