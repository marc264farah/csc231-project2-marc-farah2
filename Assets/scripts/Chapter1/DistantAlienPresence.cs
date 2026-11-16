using UnityEngine;

namespace NeonVeil
{
    public class DistantAlienPresence : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float frameDuration = 0.7f;
        [SerializeField] private float swayDistance = 0.08f;
        [SerializeField] private float phaseOffset;

        private SpriteRenderer visual;
        private Vector3 restingPosition;
        private Color restingColor;

        private void Awake()
        {
            visual = GetComponent<SpriteRenderer>();
            restingPosition = transform.localPosition;
            restingColor = visual.color;
        }

        private void Update()
        {
            float moment = Time.time + phaseOffset;
            if (frames != null && frames.Length > 0)
            {
                int frame = Mathf.FloorToInt(moment / frameDuration) % frames.Length;
                visual.sprite = frames[frame];
            }

            transform.localPosition = restingPosition +
                new Vector3(Mathf.Sin(moment * 0.55f) * swayDistance,
                    Mathf.Sin(moment * 0.8f) * swayDistance * 0.5f, 0f);
            Color tint = restingColor;
            tint.a = restingColor.a * (0.86f + Mathf.Sin(moment * 1.1f) * 0.14f);
            visual.color = tint;
        }
    }
}
