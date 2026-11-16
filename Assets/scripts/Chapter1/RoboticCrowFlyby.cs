using UnityEngine;

namespace NeonVeil
{
    public class RoboticCrowFlyby : MonoBehaviour
    {
        [SerializeField] private ChapterGame game;
        [SerializeField] private Camera chapterCamera;
        [SerializeField] private Sprite[] flightFrames;
        [SerializeField] private float minimumInterval = 5f;
        [SerializeField] private float maximumInterval = 8f;
        [SerializeField] private float flightSpeed = 3.1f;

        private SpriteRenderer crow;
        private float nextFlyby;
        private float frameTimer;
        private int frameIndex;
        private int direction;

        private void Awake()
        {
            GameObject visual = new GameObject("Flying robotic crow");
            visual.transform.SetParent(transform);
            crow = visual.AddComponent<SpriteRenderer>();
            crow.sortingOrder = 3;
            crow.enabled = false;
        }

        private void Start()
        {
            nextFlyby = Time.time + 1.3f;
        }

        private void Update()
        {
            if (game == null || chapterCamera == null || flightFrames == null || flightFrames.Length == 0)
                return;

            if (!game.CanPlay)
                return;

            if (!crow.enabled)
            {
                if (Time.time >= nextFlyby && IsOutdoorSection())
                    BeginFlyby();
                return;
            }

            crow.transform.position += Vector3.right * (direction * flightSpeed * Time.deltaTime);
            frameTimer += Time.deltaTime;
            if (frameTimer >= 0.14f)
            {
                frameTimer = 0f;
                frameIndex = (frameIndex + 1) % flightFrames.Length;
                crow.sprite = flightFrames[frameIndex];
            }

            float screenEdge = chapterCamera.orthographicSize * chapterCamera.aspect + 2f;
            if (Mathf.Abs(crow.transform.position.x - chapterCamera.transform.position.x) > screenEdge)
            {
                crow.enabled = false;
                ScheduleNextFlyby();
            }
        }

        private bool IsOutdoorSection()
        {
            return game.CurrentSection == 1 || game.CurrentSection == 4 || game.CurrentSection == 5;
        }

        private void BeginFlyby()
        {
            direction = Random.value < 0.5f ? 1 : -1;
            float edge = chapterCamera.orthographicSize * chapterCamera.aspect + 1.5f;
            float x = chapterCamera.transform.position.x - direction * edge;
            float y = chapterCamera.transform.position.y + Random.Range(0.8f, 1.65f);
            crow.transform.position = new Vector3(x, y, 0f);
            crow.flipX = direction < 0;
            crow.sprite = flightFrames[0];
            crow.color = new Color(0.88f, 0.97f, 1f, 1f);
            crow.transform.localScale = Vector3.one * 1.55f;
            crow.enabled = true;
            frameIndex = 0;
            frameTimer = 0f;
        }

        private void ScheduleNextFlyby()
        {
            nextFlyby = Time.time + Random.Range(minimumInterval, maximumInterval);
        }
    }
}




