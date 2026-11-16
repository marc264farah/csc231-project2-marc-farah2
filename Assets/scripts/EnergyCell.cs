using UnityEngine;

namespace NeonVeil
{
    [RequireComponent(typeof(CircleCollider2D))]
    public class EnergyCell : MonoBehaviour
    {
        [SerializeField] private ChapterGame game;
        [SerializeField] private Transform visual;

        private Vector3 startPosition;
        private Vector3 startScale;
        private bool collected;

        private void Start()
        {
            startPosition = visual.localPosition;
            startScale = visual.localScale;
        }

        private void Update()
        {
            if (!game.CanPlay) return;
            float pulse = Mathf.Sin(Time.time * 4f);
            visual.localPosition = startPosition + Vector3.up * pulse * 0.12f;
            visual.localScale = startScale * (1f + pulse * 0.045f);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Subject09Controller player = other.GetComponent<Subject09Controller>();
            if (collected || player == null || !game.CollectCell())
                return;

            collected = true;
            player.GetComponent<Subject09Presentation>()?.PlayPickup(transform.position);
            gameObject.SetActive(false);
        }
    }
}
