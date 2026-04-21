using UnityEngine;

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
    public Color strongColor = new Color(0.8f, 0.1f, 0.1f); // 짙은 빨강

    float _nextDamageTime;

    // strengthRatio: 0 = 시작(약함), 1 = 최대(강함)
    public void Init(float hp, float strengthRatio = 0f)
    {
        maxHp = currentHp = hp;

        float t = Mathf.Clamp01(strengthRatio);
        transform.localScale = Vector3.one * Mathf.Lerp(minScale, maxScale, t);

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = Color.Lerp(weakColor, strongColor, t);
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
        if (currentHp <= 0)
            Die();
    }

    void Die()
    {
        GameManager.Instance.AddGold(goldReward);
        Destroy(gameObject);
    }
}
