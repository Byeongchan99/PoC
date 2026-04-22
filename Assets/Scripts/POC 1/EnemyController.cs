using UnityEngine;

namespace POC1
{
    public class EnemyController : MonoBehaviour
    {
        public float maxHp      = 30f;
        public float currentHp  = 30f;
        public int   goldReward = 10;

        [Tooltip("피격 간격 (초)")]
        public float damageCooldown = 0.5f;

        [Header("비주얼 스케일")]
        public float minScale = 0.5f;
        public float maxScale = 2.0f;

        [Header("비주얼 색상")]
        public Color weakColor   = Color.white;
        public Color strongColor = new Color(0.8f, 0.1f, 0.1f);

        const float MinAlpha = 100f / 255f;

        float _nextDamageTime;
        SpriteRenderer _sr;
        Color _baseColor;

        public void Init(float hp, float strengthRatio = 0f)
        {
            maxHp = currentHp = hp;

            float t = Mathf.Clamp01(strengthRatio);
            transform.localScale = Vector3.one * Mathf.Lerp(minScale, maxScale, t);

            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null)
            {
                _baseColor = Color.Lerp(weakColor, strongColor, t);
                _baseColor.a = 1f;
                _sr.color = _baseColor;
            }
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (!other.CompareTag("Sword")) return;
            if (Time.time < _nextDamageTime) return;

            _nextDamageTime = Time.time + damageCooldown;

            SwordStats stats = other.GetComponent<SwordStats>();
            float dmg = stats != null ? stats.attackDamage : 10f;
            TakeDamage(dmg);
        }

        void TakeDamage(float dmg)
        {
            currentHp -= dmg;
            if (currentHp <= 0) { Die(); return; }

            if (_sr != null)
            {
                float alpha = Mathf.Lerp(MinAlpha, 1f, currentHp / maxHp);
                _sr.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, alpha);
            }
        }

        void Die()
        {
            GameManager.Instance.AddGold(goldReward);
            Destroy(gameObject);
        }
    }
}
