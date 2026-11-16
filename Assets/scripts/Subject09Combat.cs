using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeonVeil
{
    [RequireComponent(typeof(Subject09Controller))]
    public class Subject09Combat : MonoBehaviour
    {
        [SerializeField] private Subject09Presentation presentation;
        [SerializeField] private LayerMask enemyLayers = 1;
        [SerializeField] private LayerMask obstacleLayers = 1;

        [Header("Blade")]
        [SerializeField, Min(1)] private int bladeDamage = 1;
        [SerializeField, Min(0.1f)] private float bladeReach = 2.4f;
        [SerializeField, Min(0.1f)] private float bladeDelay = 0.42f;

        [Header("Phase Strike")]
        [SerializeField, Min(1)] private int phaseDamage = 2;
        [SerializeField, Min(1f)] private float phaseSpeed = 19f;
        [SerializeField, Min(0.02f)] private float phaseDuration = 0.22f;
        [SerializeField, Min(0.25f)] private float phaseCooldown = 2.5f;

        private Subject09Controller player;
        private Rigidbody2D body;
        private readonly HashSet<EnemyHealth> damagedEnemies = new HashSet<EnemyHealth>();
        private readonly Collider2D[] enemies = new Collider2D[16];
        private readonly RaycastHit2D[] obstacles = new RaycastHit2D[8];
        private float nextBlade;
        private float nextPhase;
        private Coroutine bladeSwing;

        public float PhaseRemaining => Mathf.Max(0f, nextPhase - Time.time);

        private void Awake()
        {
            player = GetComponent<Subject09Controller>();
            body = GetComponent<Rigidbody2D>();
        }

        public void TryAttack()
        {
            if (player.IsDead || player.IsPhasing || Time.time < nextBlade) return;
            nextBlade = Time.time + bladeDelay;
            damagedEnemies.Clear();
            presentation?.PlayAttack();
            NeonAudio.Instance?.Play(NeonAudio.Cue.Attack);
            bladeSwing = StartCoroutine(ResolveBladeSwing());
        }

        private IEnumerator ResolveBladeSwing()
        {
            yield return new WaitForSeconds(0.14f);
            if (!player.IsDead && !player.IsPhasing)
                DamageEnemies(bladeReach, 1.5f, bladeDamage);
            bladeSwing = null;
        }

        public void TryPhase()
        {
            if (Time.time < nextPhase || !player.BeginPhase(phaseSpeed, phaseDuration)) return;
            nextPhase = Time.time + phaseCooldown;
            damagedEnemies.Clear();
            presentation?.PlayPhase();
            NeonAudio.Instance?.Play(NeonAudio.Cue.Phase);
        }

        public void CheckPhaseHit(float travel)
        {
            DamageEnemies(travel + 0.55f, 1.45f, phaseDamage);
        }

        private void DamageEnemies(float reach, float height, int damage)
        {
            Vector2 origin = body.position;
            Vector2 center = origin + Vector2.right * (reach * 0.5f * player.Facing);
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(enemyLayers);
            filter.useTriggers = true;
            int count = Physics2D.OverlapBox(center, new Vector2(reach, height), 0f, filter, enemies);
            for (int i = 0; i < count; i++)
            {
                EnemyHealth enemy = enemies[i].GetComponentInParent<EnemyHealth>();
                if (enemy == null || enemy.IsDefeated || damagedEnemies.Contains(enemy))
                    continue;

                Vector2 target = enemies[i].bounds.center;
                if (IsBlocked(origin, target)) continue;
                damagedEnemies.Add(enemy);
                enemy.TakeDamage(damage);
                presentation?.PlayImpact(target);
            }
        }

        private bool IsBlocked(Vector2 origin, Vector2 target)
        {
            Vector2 direction = target - origin;
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(obstacleLayers);
            filter.useTriggers = false;
            return Physics2D.Raycast(origin, direction.normalized, filter, obstacles, direction.magnitude) > 0;
        }

        public void ResetCombat()
        {
            if (bladeSwing != null)
            {
                StopCoroutine(bladeSwing);
                bladeSwing = null;
            }
            nextBlade = 0f;
            nextPhase = 0f;
            damagedEnemies.Clear();
        }
    }
}
