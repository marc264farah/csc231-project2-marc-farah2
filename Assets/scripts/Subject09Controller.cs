using UnityEngine;
using UnityEngine.InputSystem;

namespace NeonVeil
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public class Subject09Controller : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private ChapterGame game;
        [SerializeField] private Transform visual;
        [SerializeField] private Subject09Presentation presentation;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 7.2f;
        [SerializeField, Min(0f)] private float acceleration = 45f;
        [SerializeField, Min(0f)] private float jumpSpeed = 12f;
        [SerializeField, Min(0f)] private float coyoteTime = 0.1f;
        [SerializeField, Min(0f)] private float jumpBuffer = 0.12f;
        [SerializeField, Min(0f)] private float maximumFallSpeed = 20f;
        [SerializeField] private LayerMask groundLayers = 1;
        [SerializeField] private float fallLimit = -9f;

        [Header("Health")]
        [SerializeField, Min(1)] private int maximumHealth = 4;
        [SerializeField, Min(0f)] private float immunityTime = 0.9f;
        [SerializeField, Min(0f)] private float knockbackSpeed = 5f;

        private Rigidbody2D body;
        private CapsuleCollider2D bodyCollider;
        private Subject09Combat combat;
        private InputActionMap playerActions;
        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction attackAction;
        private InputAction phaseAction;
        private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
        private float groundedUntil = -1f;
        private float jumpQueuedUntil = -1f;
        private float invulnerableUntil = -1f;
        private float knockbackUntil = -1f;
        private float phaseUntil = -1f;
        private float phaseSpeed;
        private float knockbackDirection;
        private float moveInput;
        private float previousVerticalVelocity;
        private bool jumpHeld;

        public bool IsGrounded { get; private set; }
        public int Facing { get; private set; } = 1;
        public int CurrentHealth { get; private set; }
        public int MaximumHealth => maximumHealth;
        public bool IsDead => CurrentHealth == 0;
        public bool IsPhasing => Time.time < phaseUntil;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<CapsuleCollider2D>();
            combat = GetComponent<Subject09Combat>();
            CurrentHealth = maximumHealth;
            if (fallLimit < -30f)
            {
                Debug.LogWarning("Fall boundary is outside this level; using -9.", this);
                fallLimit = -9f;
            }
            if (inputActions == null)
            {
                Debug.LogError("Assign the chapter input actions to Subject 09.", this);
                enabled = false;
                return;
            }

            playerActions = inputActions.FindActionMap("Player", true);
            moveAction = playerActions.FindAction("Move", true);
            jumpAction = playerActions.FindAction("Jump", true);
            attackAction = playerActions.FindAction("Attack", true);
            phaseAction = playerActions.FindAction("Phase", true);
        }

        private void OnEnable()
        {
            playerActions?.Enable();
        }

        private void OnDisable()
        {
            playerActions?.Disable();
            ClearInput();
        }

        private void Update()
        {
            if (!game.CanPlay || IsDead)
            {
                ClearInput();
                return;
            }

            moveInput = moveAction.ReadValue<float>();
            jumpHeld = jumpAction.IsPressed();
            if (jumpAction.WasPressedThisFrame())
                jumpQueuedUntil = Time.time + jumpBuffer;

            if (!IsPhasing && Mathf.Abs(moveInput) > 0.01f)
                Face(moveInput > 0 ? 1 : -1);

            if (phaseAction.WasPressedThisFrame())
                combat.TryPhase();
            if (attackAction.WasPressedThisFrame())
                combat.TryAttack();

            bool groundFarBelow = !Physics2D.Raycast(body.position, Vector2.down, 2.6f, groundLayers);
            presentation?.SetMovement(body.linearVelocity.x, IsGrounded, body.position.y, body.linearVelocity.y, groundFarBelow);
        }

        private void FixedUpdate()
        {
            if (!game.CanPlay || IsDead)
                return;

            if (body.position.y < fallLimit)
            {
                Die();
                return;
            }

            bool wasGrounded = IsGrounded;
            CheckGround();
            if (IsGrounded)
            {
                groundedUntil = Time.time + coyoteTime;
                if (!wasGrounded && previousVerticalVelocity < -2f)
                {
                    NeonAudio.Instance?.Play(NeonAudio.Cue.Land);
                    presentation?.PlayLanding(body.position);
                }
            }

            Vector2 velocity = body.linearVelocity;
            previousVerticalVelocity = velocity.y;
            if (IsPhasing)
            {
                MovePhase();
                return;
            }

            if (Time.time < knockbackUntil)
            {
                body.linearVelocity = new Vector2(knockbackDirection * knockbackSpeed, velocity.y);
                return;
            }

            velocity.x = Mathf.MoveTowards(velocity.x, moveInput * moveSpeed, acceleration * Time.fixedDeltaTime);
            if (jumpQueuedUntil >= Time.time && groundedUntil >= Time.time)
            {
                velocity.y = jumpSpeed;
                jumpQueuedUntil = -1f;
                groundedUntil = -1f;
                IsGrounded = false;
                presentation?.BeginJump(body.position.y);
                NeonAudio.Instance?.Play(NeonAudio.Cue.Jump);
            }

            if (!jumpHeld && velocity.y > 0f)
                velocity.y = Mathf.MoveTowards(velocity.y, 0f, 35f * Time.fixedDeltaTime);

            velocity.y = Mathf.Max(velocity.y, -maximumFallSpeed);
            body.linearVelocity = velocity;
        }

        private void Face(int direction)
        {
            Facing = direction;
            if (visual != null)
                visual.localScale = new Vector3(Facing, 1f, 1f);
        }

        private void CheckGround()
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(groundLayers);
            filter.useTriggers = false;
            int count = bodyCollider.Cast(Vector2.down, filter, groundHits, 0.08f);
            IsGrounded = false;
            for (int i = 0; i < count; i++)
            {
                if (groundHits[i].normal.y > 0.65f && body.linearVelocity.y <= 0.1f)
                    IsGrounded = true;
            }
        }

        public bool BeginPhase(float speed, float duration)
        {
            if (!game.CanPlay || IsDead || IsPhasing || Time.time < knockbackUntil)
                return false;

            phaseSpeed = speed;
            phaseUntil = Time.time + duration;
            jumpQueuedUntil = -1f;
            return true;
        }

        private void MovePhase()
        {
            float distance = phaseSpeed * Time.fixedDeltaTime;
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(groundLayers);
            filter.useTriggers = false;
            int count = bodyCollider.Cast(Vector2.right * Facing, filter, groundHits, distance + 0.025f);
            for (int i = 0; i < count; i++)
            {
                if (groundHits[i].normal.x * Facing < -0.2f)
                    distance = Mathf.Min(distance, Mathf.Max(0f, groundHits[i].distance - 0.025f));
            }

            combat.CheckPhaseHit(distance);
            if (distance < 0.005f)
                phaseUntil = -1f;

            body.linearVelocity = new Vector2(Facing * distance / Time.fixedDeltaTime, 0f);
        }

        public void TakeDamage(int damage, Vector2 source)
        {
            if (!game.CanPlay || IsDead || IsPhasing || Time.time < invulnerableUntil || damage <= 0)
                return;

            CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
            if (IsDead)
            {
                Die();
                return;
            }
            NeonAudio.Instance?.Play(NeonAudio.Cue.Hurt);

            invulnerableUntil = Time.time + immunityTime;
            knockbackUntil = Time.time + 0.14f;
            knockbackDirection = body.position.x < source.x ? -1 : 1;
            body.linearVelocity = new Vector2(0f, 3.5f);
            ClearInput();
            presentation?.PlayHurt();
        }

        private void Die()
        {
            CurrentHealth = 0;
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
            ClearInput();
            combat.ResetCombat();
            presentation?.PlayDeath();
            NeonAudio.Instance?.Play(NeonAudio.Cue.Death);
            game.ShowDeath();
        }

        public void Revive(Vector2 position)
        {
            body.position = position;
            transform.position = position;
            body.linearVelocity = Vector2.zero;
            body.simulated = true;
            CurrentHealth = maximumHealth;
            if (fallLimit < -30f)
            {
                Debug.LogWarning("Fall boundary is outside this level; using -9.", this);
                fallLimit = -9f;
            }
            invulnerableUntil = Time.time + immunityTime;
            knockbackUntil = -1f;
            phaseUntil = -1f;
            IsGrounded = false;
            groundedUntil = -1f;
            previousVerticalVelocity = 0f;
            ClearInput();
            Face(1);
            combat.ResetCombat();
            presentation?.ResetPresentation();
        }

        private void ClearInput()
        {
            moveInput = 0f;
            jumpHeld = false;
            jumpQueuedUntil = -1f;
        }
    }
}
