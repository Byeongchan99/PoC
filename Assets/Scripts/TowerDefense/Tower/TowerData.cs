using UnityEngine;

namespace POC4
{
    /// <summary>
    /// 타워 카드 하나의 데이터를 담는 ScriptableObject.
    /// 타워 종류, 스탯, 효과를 에디터에서 설정할 수 있다.
    /// Create 메뉴: POC4 > Tower Data
    /// </summary>
    [CreateAssetMenu(fileName = "TowerData", menuName = "POC4/Tower Data")]
    public class TowerData : ScriptableObject
    {
        // -------------------------------------------------------
        // 타워 종류 열거형
        // -------------------------------------------------------

        public enum TowerType
        {
            Arrow,  // 단일 타겟 투사체 (3단계 구현)
            Laser,  // 단일 타겟 지속 공격 (6단계 구현)
            Cannon  // 범위 피해 투사체 (6단계 구현)
        }

        // -------------------------------------------------------
        // 타워 효과 열거형 (6단계에서 적용)
        // -------------------------------------------------------

        public enum TowerEffectType
        {
            None,        // 효과 없음
            ExtraDamage, // 추가 피해 +5
            Slow,        // 이동 속도 50% 감소, 2초
            Stun         // 1초 기절
        }

        // -------------------------------------------------------
        // Inspector 노출 필드
        // -------------------------------------------------------

        [Header("Type")]
        [SerializeField] private TowerType _towerType = TowerType.Arrow;

        [Header("Stats (모두 Inspector 수정 가능)")]
        [SerializeField] private float _attackPower = 10f;
        [Tooltip("사거리: 월드 거리 단위 (실수값)")]
        [SerializeField] private float _range = 3f;
        [Tooltip("공격 속도: 초당 공격 횟수")]
        [SerializeField] private float _attackSpeed = 1f;

        [Header("Effect (6단계에서 구현)")]
        [SerializeField] private TowerEffectType _effectType = TowerEffectType.None;

        [Header("Effect Values (모두 Inspector 수정 가능)")]
        [SerializeField] private float _extraDamage = 5f;
        [SerializeField] private float _slowRatio = 0.5f;
        [SerializeField] private float _slowDuration = 2f;
        [SerializeField] private float _stunDuration = 1f;

        // -------------------------------------------------------
        // 런타임 초기화 (카드 제작 시스템 전용)
        // -------------------------------------------------------

        /// <summary>
        /// 카드 제작 시스템이 ScriptableObject.CreateInstance 로 생성한 인스턴스에 데이터를 설정한다.
        /// template의 스탯을 그대로 복사하고 효과 종류만 덮어쓴다.
        /// 에디터에서 만든 에셋(TowerData 파일)에는 절대 호출하지 말 것.
        /// </summary>
        public void Initialize(TowerData template, TowerEffectType effectType)
        {
            _towerType = template._towerType;
            _attackPower = template._attackPower;
            _range = template._range;
            _attackSpeed = template._attackSpeed;
            _effectType = effectType;
            _extraDamage = template._extraDamage;
            _slowRatio = template._slowRatio;
            _slowDuration = template._slowDuration;
            _stunDuration = template._stunDuration;
        }

        // -------------------------------------------------------
        // 프로퍼티
        // -------------------------------------------------------

        public TowerType Type => _towerType;
        public float AttackPower => _attackPower;
        public float Range => _range;
        public float AttackSpeed => _attackSpeed;
        public TowerEffectType EffectType => _effectType;
        public float ExtraDamage => _extraDamage;
        public float SlowRatio => _slowRatio;
        public float SlowDuration => _slowDuration;
        public float StunDuration => _stunDuration;
    }
}
