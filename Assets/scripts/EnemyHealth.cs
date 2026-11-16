using System.Collections;
using UnityEngine;

namespace NeonVeil
{
    public class EnemyHealth : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maximumHealth = 3;
        [SerializeField] private SpriteRenderer bodySprite;
        [SerializeField] private Transform healthFill;

        private int currentHealth;
        private Vector3 fullHealthScale;
        private Color normalColor;
        private Coroutine hitFlash;
        private ChapterEnemy enemy;

        public int CurrentHealth => currentHealth;
        public bool IsDefeated => currentHealth <= 0;

        private void Awake()
        {
            enemy = GetComponent<ChapterEnemy>();
            currentHealth = maximumHealth;
            if (bodySprite != null) normalColor = bodySprite.color;
            if (healthFill != null) fullHealthScale = healthFill.localScale;
        }

        public void TakeDamage(int damage)
        {
            if (IsDefeated || damage <= 0) return;
            currentHealth = Mathf.Max(0, currentHealth - damage);
            if (healthFill != null)
            {
                Vector3 scale = fullHealthScale;
                scale.x *= (float)currentHealth / maximumHealth;
                healthFill.localScale = scale;
            }

            if (IsDefeated)
            {
                if (hitFlash != null) StopCoroutine(hitFlash);
                enemy?.OnDefeated();
                GetComponent<Collider2D>().enabled = false;
                GetComponent<Rigidbody2D>().simulated = false;
                if (healthFill != null) healthFill.gameObject.SetActive(false);
                StartCoroutine(Defeat());
                return;
            }

            enemy?.OnHit();
            if (bodySprite == null) return;
            if (hitFlash != null) StopCoroutine(hitFlash);
            hitFlash = StartCoroutine(FlashOnHit());
        }

        private IEnumerator Defeat()
        {
            if (bodySprite != null) bodySprite.color = Color.white;
            yield return new WaitForSeconds(0.32f);
            gameObject.SetActive(false);
        }

        private IEnumerator FlashOnHit()
        {
            bodySprite.color = Color.white;
            yield return new WaitForSeconds(0.13f);
            bodySprite.color = normalColor;
            hitFlash = null;
        }
    }
}
