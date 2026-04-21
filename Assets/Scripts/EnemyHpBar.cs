using UnityEngine;
using UnityEngine.UI;

public class EnemyHpBar : MonoBehaviour
{
    [SerializeField] Image hpFill;

    EnemyController _enemy;

    void Awake() => _enemy = GetComponentInParent<EnemyController>();

    void LateUpdate()
    {
        if (_enemy == null) return;
        hpFill.fillAmount = _enemy.currentHp / _enemy.maxHp;
        transform.rotation = Camera.main.transform.rotation;
    }
}
