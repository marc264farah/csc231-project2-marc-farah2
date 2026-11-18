using System.Collections;
using UnityEngine;

namespace NeonVeil
{
    public class Subject09Presentation : MonoBehaviour
    {
        // These values match the Pose parameter in the Subject 09 Animator Controller.
        public enum Pose { Idle, Run, Jump, Fall, Hurt, Death, Attack, Phase }

        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer armor;
        [SerializeField] private SpriteRenderer eye;
        [SerializeField] private Sprite effectSquare;
        [SerializeField] private Sprite bladeArc;
        [SerializeField] private Sprite landingArt;
        [SerializeField] private Sprite pickupArt;
        [SerializeField] private Sprite hitArt;

        private static readonly int PoseParameter = Animator.StringToHash("Pose");
        private static readonly Color HurtColor = new Color(1f, 0.23f, 0.33f);
        private static readonly Color PhaseColor = new Color(0.32f, 0.72f, 1f);
        private Color armorColor;
        private Color eyeColor;
        private Vector3 armorScale;
        private Vector3 armorPosition;
        private Pose movementPose;
        private Pose actionPose;
        private bool jumpInProgress;
        private float jumpTakeoffHeight;
        private float lastGroundHeight;
        private float actionEnds;
        private bool dead;
        private Coroutine phaseTrail;
        private GameObject activeBlade;

        public void Setup(Animator playerAnimator, SpriteRenderer armorRenderer,
            SpriteRenderer eyeRenderer, Sprite square, Sprite arc = null)
        {
            animator = playerAnimator;
            armor = armorRenderer;
            eye = eyeRenderer;
            effectSquare = square;
            bladeArc = arc;
        }

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (armor == null) armor = GetComponentInChildren<SpriteRenderer>();
            if (armor != null)
            {
                armorColor = armor.color;
                if (armorColor.a < 0.05f)
                {
                    armorColor = Color.white;
                    armor.color = armorColor;
                }
                armorScale = armor.transform.localScale;
                armorPosition = armor.transform.localPosition;
            }
            if (eye != null) eyeColor = eye.color;
        }

        public void BeginJump(float takeoffHeight)
        {
            jumpInProgress = true;
            jumpTakeoffHeight = takeoffHeight;
        }

        public void SetMovement(float horizontal, bool grounded, float height, float verticalSpeed, bool groundFarBelow)
        {
            if (grounded)
            {
                jumpInProgress = false;
                lastGroundHeight = height;
                movementPose = Mathf.Abs(horizontal) > 0.1f ? Pose.Run : Pose.Idle;
            }
            else
            {
                    // The ground probe keeps short drops in the movement pose; open shafts use Fall.
                if (jumpInProgress && groundFarBelow && verticalSpeed < -0.25f && height < jumpTakeoffHeight - 0.4f)
                    jumpInProgress = false;
                if (jumpInProgress)
                    movementPose = Pose.Jump;
                else if (groundFarBelow && verticalSpeed < -0.25f && height < lastGroundHeight - 0.1f)
                    movementPose = Pose.Fall;
                else
                    movementPose = Mathf.Abs(horizontal) > 0.1f ? Pose.Run : Pose.Idle;
            }
        }

        private void Update()
        {
            Pose pose = movementPose;
            if (dead) pose = Pose.Death;
            else if (Time.time < actionEnds) pose = actionPose;

            if (animator != null && animator.runtimeAnimatorController != null)
                animator.SetInteger(PoseParameter, (int)pose);

            if (armor != null)
            {
                float visualScale = 1f;
                float visualHeight = 0f;
                if (pose == Pose.Idle)
                {
                    visualScale = 1.16f;
                    visualHeight = 0.06f;
                }
                else if (pose == Pose.Jump)
                {
                    visualScale = 1.25f;
                    visualHeight = 0.27f;
                }

                armor.transform.localScale = armorScale * visualScale;
                armor.transform.localPosition = armorPosition + Vector3.up * visualHeight;
                armor.color = armorColor;
                if (pose == Pose.Hurt) armor.color = Color.Lerp(armorColor, HurtColor, 0.72f);
                if (pose == Pose.Death) armor.color = Color.Lerp(armorColor, Color.black, 0.58f);
                if (pose == Pose.Phase) armor.color = Color.Lerp(armorColor, PhaseColor, 0.60f);
            }

            if (eye != null)
                eye.color = pose == Pose.Death ? Color.Lerp(eyeColor, Color.black, 0.88f) : eyeColor;
        }

        public void PlayHurt()
        {
            if (dead) return;
            ShowAction(Pose.Hurt, 0.29f);
            Burst(transform.position, HurtColor);
        }

        public void PlayDeath()
        {
            if (dead) return;
            dead = true;
            Burst(transform.position, HurtColor);
        }

        public void PlayAttack()
        {
            if (dead) return;
            ShowAction(Pose.Attack, 0.32f);
            // The Attack clip now contains the complete blade swing and energy trail.
            if (animator != null && animator.runtimeAnimatorController != null) return;
            Sprite sprite = bladeArc != null ? bladeArc : effectSquare;
            if (sprite == null) return;

            if (activeBlade != null) Destroy(activeBlade);
            SpriteRenderer arc = CreateEffect("Energy Blade Arc", sprite);
            activeBlade = arc.gameObject;
            StartCoroutine(AnimateArc(arc));
        }

        public void PlayPhase()
        {
            if (dead) return;
            ShowAction(Pose.Phase, 0.22f);
            if (phaseTrail != null) StopCoroutine(phaseTrail);
            phaseTrail = StartCoroutine(LeavePhaseTrail());
        }

        public void PlayImpact(Vector2 position)
        {
            Burst(position, new Color(1f, 0.75f, 0.30f));
        }

        public void PlayLanding(Vector2 position)
        {
            Sprite sprite = landingArt != null ? landingArt : effectSquare;
            if (sprite == null) return;
            SpriteRenderer burst = CreateEffect("Painted Landing Burst", sprite);
            burst.transform.position = position + Vector2.down * 0.68f;
            burst.transform.localScale = landingArt != null
                ? new Vector3(0.28f, 0.17f, 1f) : new Vector3(0.5f, 0.12f, 1f);
            StartCoroutine(FadeEffect(burst, new Color(1f, 1f, 1f, 0.88f),
                0.26f, Vector2.zero));
        }
        public void PlayPickup(Vector2 position)
        {
            Sprite sprite = pickupArt != null ? pickupArt : effectSquare;
            if (sprite == null) return;
            SpriteRenderer burst = CreateEffect("Painted Pickup Burst", sprite);
            burst.transform.position = position;
            burst.transform.localScale = Vector3.one * (pickupArt != null ? 0.36f : 0.4f);
            StartCoroutine(FadeEffect(burst, new Color(1f, 1f, 1f, 0.94f),
                0.42f, Vector2.up * 0.5f));
        }
        public void ResetPresentation()
        {
            dead = false;
            actionEnds = 0f;
            jumpInProgress = false;
            lastGroundHeight = transform.position.y;
            movementPose = Pose.Idle;
            if (activeBlade != null) Destroy(activeBlade);
            if (phaseTrail != null)
            {
                StopCoroutine(phaseTrail);
                phaseTrail = null;
            }
            if (armor != null) armor.color = armorColor;
            if (eye != null) eye.color = eyeColor;
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetInteger(PoseParameter, (int)Pose.Idle);
                animator.Play(nameof(Pose.Idle), 0, 0f);
            }
        }

        private void ShowAction(Pose pose, float duration)
        {
            actionPose = pose;
            actionEnds = Time.time + duration;
        }

        private IEnumerator AnimateArc(SpriteRenderer arc)
        {
            float facing = animator != null && animator.transform.lossyScale.x < 0f ? -1f : 1f;
            float size = arc.sprite.bounds.size.x;
            float duration = 0.23f;
            Destroy(arc.gameObject, duration);
            for (float time = 0f; time < duration && arc != null; time += Time.deltaTime)
            {
                float progress = time / duration;
                arc.transform.position = transform.position + new Vector3(facing * 0.66f, 0.18f, 0f);
                arc.transform.rotation = Quaternion.Euler(0f, 0f, facing * Mathf.Lerp(31f, -30f, progress));
                arc.transform.localScale = Vector3.one * Mathf.Lerp(1.5f, 2.3f, progress) / size;
                arc.flipX = facing < 0f;
                arc.color = new Color(0.32f, 0.96f, 1f, Mathf.Sin(progress * 2.7f + 0.4f));
                yield return null;
            }
        }

        private IEnumerator LeavePhaseTrail()
        {
            if (armor == null || armor.sprite == null) yield break;
            for (int i = 0; i < 5; i++)
            {
                SpriteRenderer ghost = CreateEffect("Phase Afterimage", armor.sprite);
                ghost.sortingOrder = armor.sortingOrder - 1;
                ghost.transform.SetPositionAndRotation(armor.transform.position, armor.transform.rotation);
                ghost.transform.localScale = armor.transform.lossyScale;
                Color color = new Color(0.24f, 0.69f, 1f, 0.58f);
                StartCoroutine(FadeEffect(ghost, color, 0.20f, Vector2.zero));
                yield return new WaitForSeconds(0.033f);
            }
            phaseTrail = null;
        }

        private void Burst(Vector2 position, Color color)
        {
            Sprite sprite = hitArt != null ? hitArt : effectSquare;
            if (sprite == null) return;
            SpriteRenderer burst = CreateEffect("Painted Hit Burst", sprite);
            burst.transform.position = position;
            burst.transform.localScale = Vector3.one * (hitArt != null ? 0.34f : 0.5f);
            Color tint = hitArt != null ? Color.Lerp(Color.white, color, 0.15f) : color;
            tint.a = 0.93f;
            StartCoroutine(FadeEffect(burst, tint, 0.26f, Vector2.zero));
        }
        private SpriteRenderer CreateEffect(string effectName, Sprite sprite)
        {
            SpriteRenderer effect = new GameObject(effectName).AddComponent<SpriteRenderer>();
            effect.sprite = sprite;
            if (armor != null)
            {
                effect.sortingLayerID = armor.sortingLayerID;
                effect.sortingOrder = armor.sortingOrder + 8;
            }
            return effect;
        }

        private static IEnumerator FadeEffect(SpriteRenderer effect, Color color, float duration, Vector2 velocity)
        {
            Destroy(effect.gameObject, duration);
            float opacity = color.a;
            for (float time = 0f; time < duration && effect != null; time += Time.deltaTime)
            {
                effect.transform.position += (Vector3)velocity * Time.deltaTime;
                color.a = opacity * (1f - time / duration);
                effect.color = color;
                yield return null;
            }
        }
    }
}

