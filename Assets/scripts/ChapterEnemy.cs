using UnityEngine;

namespace NeonVeil
{
    [RequireComponent(typeof(Rigidbody2D), typeof(EnemyHealth))]
    public class ChapterEnemy : MonoBehaviour
    {
        public enum EnemyKind { TrainingDrone, Sentinel, Mimic }
        private enum EnemyState { Idle, Patrol, Waking, Windup, Strike, Recover, Defeated }

        [SerializeField] private EnemyKind kind;
        [SerializeField] private ChapterGame game;
        [SerializeField] private Subject09Controller player;
        [SerializeField] private Transform visual;
        [SerializeField] private SpriteRenderer warning;
        [SerializeField] private SpriteRenderer strikeEffect;
        [SerializeField] private SpriteRenderer crawlBody;
        [SerializeField] private Sprite[] crawlFrames;
        [SerializeField] private SpriteRenderer sentinelBody;
        [SerializeField] private Sprite[] sentinelFrames;
        [SerializeField] private LayerMask groundLayers = 1;
        [SerializeField, Min(1f)] private float patrolRange = 4f;
        [SerializeField, Min(1f)] private float detectionRange = 6f;

        private Rigidbody2D body;
        private EnemyHealth health;
        private Vector2 home;
        private EnemyState state;
        private float stateUntil;
        private float stateStartedAt;
        private float stateDuration;
        private float hitUntil;
        private int facing = 1;
        private bool struck;

        public bool IsDefeated => health.IsDefeated;
        public bool CountsForExtraction => kind != EnemyKind.TrainingDrone;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            health = GetComponent<EnemyHealth>();
            home = body.position;
            ResetEncounter();
        }

        private void Start()
        {
            Physics2D.IgnoreCollision(GetComponent<Collider2D>(), player.GetComponent<Collider2D>());
        }

        private void FixedUpdate()
        {
            if (!game.CanPlay || health.IsDefeated || kind == EnemyKind.TrainingDrone)
                return;

            Vector2 toPlayer = (Vector2)player.transform.position - body.position;
            switch (state)
            {
                case EnemyState.Idle:
                    if (CanSeePlayer(toPlayer, detectionRange))
                        SetState(EnemyState.Waking, 0.4f);
                    break;
                case EnemyState.Waking:
                    if (Time.time >= stateUntil) SetState(EnemyState.Patrol);
                    break;
                case EnemyState.Patrol:
                    Patrol(toPlayer);
                    break;
                case EnemyState.Windup:
                    if (Time.time >= stateUntil)
                    {
                        // Aim once more as the attack begins if the player crossed behind it.
                        if (kind != EnemyKind.TrainingDrone)
                            Face(toPlayer.x >= 0f ? 1 : -1);
                        SetState(EnemyState.Strike, 0.20f);
                    }
                    break;
                case EnemyState.Strike:
                    Strike(toPlayer);
                    break;
                case EnemyState.Recover:
                    if (Time.time >= stateUntil) SetState(EnemyState.Patrol);
                    break;
            }
        }

        private void Update()
        {
            if (visual == null) return;
            float progress = stateDuration > 0f ? Mathf.Clamp01((Time.time - stateStartedAt) / stateDuration) : 0f;
            float bob = kind == EnemyKind.Mimic ? 0f :
                Mathf.Sin(Time.time * (kind == EnemyKind.TrainingDrone ? 3f : 8f)) * 0.035f;
            float forward = 0f;
            float height = bob;
            float stretch = 1f;
            float squat = 1f;
            float tilt = 0f;

            switch (state)
            {
                case EnemyState.Waking:
                    height += Mathf.Sin(progress * Mathf.PI) * 0.10f;
                    break;
                case EnemyState.Windup:
                    forward = -0.18f * progress;
                    squat = Mathf.Lerp(1f, kind == EnemyKind.Mimic ? 0.76f : 0.87f, progress);
                    tilt = kind == EnemyKind.Mimic ? 7f * progress : -5f * progress;
                    break;
                case EnemyState.Strike:
                    forward = Mathf.Lerp(0.10f, kind == EnemyKind.Mimic ? 0.60f : 0.36f, Mathf.Min(progress * 3f, 1f));
                    stretch = kind == EnemyKind.Mimic ? 1.19f : 1.06f;
                    tilt = kind == EnemyKind.Mimic ? -12f : 5f;
                    break;
                case EnemyState.Recover:
                    forward = Mathf.Lerp(0.15f, 0f, progress);
                    squat = Mathf.Lerp(0.90f, 1f, progress);
                    break;
                case EnemyState.Defeated:
                    height = -0.20f * progress;
                    squat = Mathf.Lerp(1f, 0.2f, progress);
                    tilt = Mathf.Lerp(0f, 65f, progress);
                    break;
            }

            if (Time.time < hitUntil && state != EnemyState.Defeated)
                forward += Mathf.Sin(Time.time * 65f) * 0.10f;
            visual.localPosition = new Vector3(forward * facing, height, 0f);
            // Both rebuilt enemy sheets face right, so the whole visual follows facing.
            visual.localScale = new Vector3(facing * stretch, squat, 1f);
            visual.localRotation = Quaternion.Euler(0f, 0f, tilt * facing);
            if (kind == EnemyKind.Mimic && crawlBody != null && crawlFrames != null && crawlFrames.Length == 8)
            {
                int frame = state == EnemyState.Patrol ? 1 + Mathf.FloorToInt(Time.time * 9f) % 4 :
                    state == EnemyState.Windup ? 5 : state == EnemyState.Strike ? 6 :
                    state == EnemyState.Recover ? 7 : 0;
                crawlBody.sprite = crawlFrames[frame];
            }
            if (kind == EnemyKind.Sentinel && sentinelBody != null && sentinelFrames != null && sentinelFrames.Length == 8)
            {
                int frame = state == EnemyState.Patrol ? 1 + Mathf.FloorToInt(Time.time * 10f) % 3 :
                    state == EnemyState.Waking || state == EnemyState.Windup ? 4 :
                    state == EnemyState.Strike ? 5 : state == EnemyState.Recover ? 6 : 0;
                sentinelBody.sprite = sentinelFrames[frame];
            }

            if (strikeEffect != null)
            {
                strikeEffect.enabled = state == EnemyState.Strike && game.CanPlay;
                if (strikeEffect.enabled)
                {
                    Color color = strikeEffect.color;
                    color.a = Mathf.Clamp01((1f - progress) * 2f);
                    strikeEffect.color = color;
                }
            }
            if (warning != null && state == EnemyState.Windup && kind == EnemyKind.Sentinel)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 14f);
                warning.color = Color.Lerp(new Color(1f, 0.48f, 0.12f),
                    new Color(1f, 0.88f, 0.38f), pulse);
            }
        }

        private void Patrol(Vector2 toPlayer)
        {
            float attackRange = kind == EnemyKind.Sentinel ? 2.4f : 2f;
            if (CanSeePlayer(toPlayer, attackRange))
            {
                Face(toPlayer.x >= 0f ? 1 : -1);
                SetState(EnemyState.Windup, kind == EnemyKind.Sentinel ? 0.85f : 0.32f);
                return;
            }

            if (kind == EnemyKind.Mimic)
            {
                if (Mathf.Abs(toPlayer.x) > detectionRange + 5f)
                {
                    SetState(EnemyState.Idle);
                    return;
                }
                Face(toPlayer.x >= 0f ? 1 : -1);
                Move(4f);
                return;
            }

            bool reachedPatrolLimit = Mathf.Abs(body.position.x - home.x) >= patrolRange &&
                Mathf.Sign(body.position.x - home.x) == facing;
            if (reachedPatrolLimit || !CanMove())
                Face(-facing);

            Move(2.1f);
        }

        private void Strike(Vector2 toPlayer)
        {
            if (Time.time >= stateUntil)
            {
                SetState(EnemyState.Recover, kind == EnemyKind.Sentinel ? 0.65f : 0.45f);
                return;
            }

            if (kind == EnemyKind.Mimic)
            {
                // Leave enough space for the body and claws to remain on the attacking side.
                if (toPlayer.x * facing > 1.25f) Move(7.5f);
                else body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            }
            float reach = kind == EnemyKind.Sentinel ? 3.1f : 1.4f;
            if (!struck && Time.time - stateStartedAt >= 0.09f &&
                toPlayer.x * facing > 0.25f && CanSeePlayer(toPlayer, reach))
            {
                struck = true;
                player.TakeDamage(1, body.position);
            }
        }

        private bool CanSeePlayer(Vector2 direction, float distance)
        {
            return Mathf.Abs(direction.x) <= distance && Mathf.Abs(direction.y) <= 1.35f &&
                !Physics2D.Raycast(body.position, direction.normalized, direction.magnitude, groundLayers);
        }

        private bool CanMove()
        {
            Vector2 front = body.position + new Vector2(0.68f * facing, 0f);
            return Physics2D.Raycast(front, Vector2.down, 1.25f, groundLayers) &&
                !Physics2D.Raycast(body.position, Vector2.right * facing, 0.76f, groundLayers);
        }

        private void Move(float speed)
        {
            body.linearVelocity = new Vector2(CanMove() ? speed * facing : 0f, body.linearVelocity.y);
        }

        private void Face(int direction)
        {
            facing = direction;
            if (visual != null)
                visual.localScale = new Vector3(direction, 1f, 1f);
        }

        private void SetState(EnemyState next, float duration = 0f)
        {
            state = next;
            stateStartedAt = Time.time;
            stateDuration = duration;
            stateUntil = Time.time + duration;
            struck = false;
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            if (warning != null)
            {
                warning.enabled = next == EnemyState.Waking || next == EnemyState.Windup || next == EnemyState.Strike;
                warning.color = next == EnemyState.Strike ? new Color(1f, 0.25f, 0.38f) : new Color(1f, 0.75f, 0.25f);
            }
            if (strikeEffect != null) strikeEffect.enabled = false;
        }

        public void OnHit()
        {
            hitUntil = Time.time + 0.18f;
        }

        public void OnDefeated()
        {
            SetState(EnemyState.Defeated, 0.32f);
            game.RecordEnemyDefeat(this);
        }

        public void ResetEncounter()
        {
            if (health != null && health.IsDefeated) return;
            body.position = home;
            transform.position = home;
            body.linearVelocity = Vector2.zero;
            Face(1);
            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.identity;
            SetState(kind == EnemyKind.Sentinel ? EnemyState.Patrol : EnemyState.Idle);
        }
    }
}
