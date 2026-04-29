using UnityEngine;

namespace POC5.Data
{
    /// <summary>
    /// 스피릿 종족별 정적 메타데이터를 담는 ScriptableObject.
    /// 스피릿의 이름, 속성, 기본 스탯 등을 인스펙터에서 설정한다.
    ///
    /// 에셋 생성: 프로젝트 창 우클릭 → Create → POC5 → Data → SpiritData
    /// </summary>
    [CreateAssetMenu(fileName = "SpiritData_New", menuName = "POC5/Data/SpiritData")]
    public class SpiritData : ScriptableObject
    {
        [Header("기본 정보")]
        [Tooltip("UI에 표시할 스피릿 이름 (예: 물의 정령).")]
        [SerializeField] private string _displayName;

        [Tooltip("UI에 표시할 스피릿 아이콘 스프라이트.")]
        [SerializeField] private Sprite _icon;

        [Tooltip("스피릿의 속성. 배치 가능한 설비 종류가 이 속성에 따라 결정된다.")]
        [SerializeField] private SpiritElement _element;

        [Header("스탯")]
        [Tooltip("작업 능력 수치. 배치된 설비의 baseProductionPerTick에 곱해서 실제 생산량을 결정한다.")]
        [SerializeField] private float _workPower = 1f;

        [Tooltip("공격력. POC에서는 UI 표시 전용이며 실제 로직에는 사용하지 않는다.")]
        [SerializeField] private int _attackPower = 10;

        [Tooltip("최대 체력. POC에서는 UI 표시 전용이며 감소 로직이 없다.")]
        [SerializeField] private int _maxHp = 100;

        [Tooltip("최대 포만감. POC에서는 UI 표시 전용이며 감소 로직이 없다.")]
        [SerializeField] private int _maxSatiety = 100;

        /// <summary>UI에 표시할 스피릿 이름.</summary>
        public string DisplayName => _displayName;

        /// <summary>UI에 표시할 아이콘 스프라이트.</summary>
        public Sprite Icon => _icon;

        /// <summary>스피릿의 속성 (Water / Grass / Fire).</summary>
        public SpiritElement Element => _element;

        /// <summary>작업 능력 수치. 설비의 생산량 계산에 사용된다.</summary>
        public float WorkPower => _workPower;

        /// <summary>공격력 (POC에서는 표시 전용).</summary>
        public int AttackPower => _attackPower;

        /// <summary>최대 체력 (POC에서는 표시 전용).</summary>
        public int MaxHp => _maxHp;

        /// <summary>최대 포만감 (POC에서는 표시 전용).</summary>
        public int MaxSatiety => _maxSatiety;
    }
}
