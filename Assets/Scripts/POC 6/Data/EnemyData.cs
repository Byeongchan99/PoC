using UnityEngine;

namespace POC6
{
    /// <summary>
    /// 적 한 종류의 설정 데이터를 담는 ScriptableObject입니다.
    /// 인스펙터에서 일반/엘리트/보스 스탯을 직접 조정할 수 있습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Enemy_New", menuName = "POC6/Data/EnemyData")]
    public class EnemyData : ScriptableObject
    {
        [Header("기본 정보")]
        [Tooltip("적 이름 (예: 전투기, 중형 전함)")]
        [SerializeField] private string _enemyName;

        [Tooltip("적 등급 (Normal / Elite / Boss)")]
        [SerializeField] private EnemyTier _tier;

        [Header("전투 스탯")]
        [Tooltip("최대 체력")]
        [Min(1)]
        [SerializeField] private int _maxHealth = 30;

        [Tooltip("공격 사거리 (월드 유닛)")]
        [Min(0.1f)]
        [SerializeField] private float _attackRange = 5f;

        [Tooltip("발사체 한 발의 데미지")]
        [Min(0.1f)]
        [SerializeField] private float _attackDamage = 5f;

        [Tooltip("공격 간격 (초). 낮을수록 빠르게 공격합니다.")]
        [Min(0.1f)]
        [SerializeField] private float _attackInterval = 1.5f;

        [Tooltip("이동 속도 (초당 유닛)")]
        [Min(0.1f)]
        [SerializeField] private float _moveSpeed = 3f;

        [Tooltip("발사체 이동 속도")]
        [Min(1f)]
        [SerializeField] private float _projectileSpeed = 8f;

        [Header("보상")]
        [Tooltip("처치 시 드롭하는 골드 양")]
        [Min(0)]
        [SerializeField] private int _goldDropAmount = 5;

        [Header("비주얼")]
        [Tooltip("씬에 배치될 적 게임오브젝트 프리팹")]
        [SerializeField] private GameObject _visualPrefab;

        // 읽기 전용 프로퍼티들
        public string EnemyName => _enemyName;
        public EnemyTier Tier => _tier;
        public int MaxHealth => _maxHealth;
        public float AttackRange => _attackRange;
        public float AttackDamage => _attackDamage;
        public float AttackInterval => _attackInterval;
        public float MoveSpeed => _moveSpeed;
        public float ProjectileSpeed => _projectileSpeed;
        public int GoldDropAmount => _goldDropAmount;
        public GameObject VisualPrefab => _visualPrefab;
    }
}
