using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public float maxHp      = 30f;
    public float currentHp  = 30f;
    public int   goldReward = 10;

    [Tooltip("피격 간격 (초)")]
    public float damageCooldown = 0.5f;

    float _nextDamageTime;

    public void Init(float hp)
    {
        maxHp = currentHp = hp;
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
