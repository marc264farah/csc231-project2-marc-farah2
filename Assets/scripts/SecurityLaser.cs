using UnityEngine;

namespace NeonVeil
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class SecurityLaser : MonoBehaviour
    {
        [SerializeField] private ChapterGame game;
        [SerializeField] private SpriteRenderer beam;
        [SerializeField, Min(0.1f)] private float safeTime = 2f;
        [SerializeField, Min(0.1f)] private float warningTime = 0.8f;
        [SerializeField, Min(0.1f)] private float activeTime = 1.25f;

        private float cycleTime;
        private Vector3 beamScale;
        public bool IsActive => cycleTime >= safeTime + warningTime;

        private void Awake()
        {
            beamScale = beam.transform.localScale;
        }

        private void Start()
        {
            ResetCycle();
        }

        private void Update()
        {
            if (!game.CanPlay) return;
            cycleTime = Mathf.Repeat(cycleTime + Time.deltaTime, safeTime + warningTime + activeTime);
            UpdateBeam();
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!game.CanPlay || !IsActive) return;
            Subject09Controller player = other.GetComponent<Subject09Controller>();
            if (player != null) player.TakeDamage(1, transform.position);
        }

        public void ResetCycle()
        {
            cycleTime = 0f;
            UpdateBeam();
        }

        private void UpdateBeam()
        {
            float offset = transform.position.x * 0.37f;
            float flicker = Mathf.Sin(Time.time * 18f + offset) * 0.5f +
                Mathf.Sin(Time.time * 31f + offset) * 0.25f;
            float width = 1f + flicker * (IsActive ? 0.14f : 0.08f);
            beam.transform.localScale = new Vector3(
                beamScale.x * width, beamScale.y, beamScale.z);

            if (IsActive)
                beam.color = new Color(1f, 0.12f, 0.30f, 0.95f + flicker * 0.05f);
            else if (cycleTime >= safeTime)
                beam.color = new Color(1f, 0.75f, 0.16f, 0.78f + flicker * 0.08f);
            else
                beam.color = new Color(0.28f, 0.72f, 0.94f, 0.20f + flicker * 0.04f);
        }
    }
}