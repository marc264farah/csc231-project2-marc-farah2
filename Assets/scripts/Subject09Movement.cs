using UnityEngine;
using UnityEngine.InputSystem;

namespace NeonVeil
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public class Subject09Movement : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float acceleration = 45f;
        [SerializeField] private float jumpSpeed = 12f;
        [SerializeField] private float coyoteTime = 0.1f;
        [SerializeField] private float jumpBuffer = 0.12f;
        [SerializeField] private float maximumFallSpeed = 20f;
        [SerializeField] private LayerMask groundLayers = 1;
        [SerializeField] private Transform visual;

        private Rigidbody2D body;
        private CapsuleCollider2D bodyCollider;
        private InputAction moveAction;
        private InputAction jumpAction;
        private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
        private float groundedUntil = -1f;
        private float jumpQueuedUntil = -1f;
        private bool jumpHeld;
        private float moveInput;
        private Vector3 spawnPosition;

        public bool IsGrounded { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<CapsuleCollider2D>();
            spawnPosition = transform.position;
            moveAction = new InputAction("Move", InputActionType.Value);
            moveAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/a").With("Positive", "<Keyboard>/d");
            moveAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/leftArrow").With("Positive", "<Keyboard>/rightArrow");
            jumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
        }

        private void OnEnable()
        {
            moveAction.Enable();
            jumpAction.Enable();
        }

        private void OnDisable()
        {
            moveAction.Disable();
            jumpAction.Disable();
        }

        private void OnDestroy()
        {
            moveAction.Dispose();
            jumpAction.Dispose();
        }

        private void Update()
        {
            moveInput = moveAction.ReadValue<float>();
            jumpHeld = jumpAction.IsPressed();
            if (jumpAction.WasPressedThisFrame()) jumpQueuedUntil = Time.time + jumpBuffer;
            if (visual != null && Mathf.Abs(moveInput) > 0.01f)
                visual.localScale = new Vector3(Mathf.Sign(moveInput), 1f, 1f);
        }

        private void FixedUpdate()
        {
            CheckGround();
            if (IsGrounded) groundedUntil = Time.time + coyoteTime;
            Vector2 velocity = body.linearVelocity;
            velocity.x = Mathf.MoveTowards(velocity.x, moveInput * moveSpeed, acceleration * Time.fixedDeltaTime);
            if (jumpQueuedUntil >= Time.time && groundedUntil >= Time.time)
            {
                velocity.y = jumpSpeed;
                jumpQueuedUntil = -1f;
                groundedUntil = -1f;
                IsGrounded = false;
            }
            // Releasing Space early shortens the jump; falling has a firm speed limit.
            if (!jumpHeld && velocity.y > 0f)
                velocity.y = Mathf.MoveTowards(velocity.y, 0f, 35f * Time.fixedDeltaTime);
            velocity.y = Mathf.Max(velocity.y, -maximumFallSpeed);
            body.linearVelocity = velocity;
            if (transform.position.y < -8f) Respawn();
        }

        private void CheckGround()
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(groundLayers);
            filter.useTriggers = false;
            int count = bodyCollider.Cast(Vector2.down, filter, groundHits, 0.08f);
            IsGrounded = false;
            for (int i = 0; i < count; i++)
                if (groundHits[i].normal.y > 0.65f && body.linearVelocity.y <= 0.1f)
                    IsGrounded = true;
        }

        public void Respawn()
        {
            body.position = spawnPosition;
            body.linearVelocity = Vector2.zero;
            groundedUntil = -1f;
            jumpQueuedUntil = -1f;
        }
    }
}
