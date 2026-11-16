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
        public bool IsActive => cycleTime >= safeTime + warningTime;

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
            beam.color = IsActive ? new Color(1f, 0.08f, 0.32f, 0.92f) :
                cycleTime >= safeTime ? new Color(1f, 0.64f, 0.18f, 0.48f) :
                new Color(0.21f, 0.47f, 0.59f, 0.12f);
        }
    }
}
