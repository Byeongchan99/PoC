using System;
using UnityEngine;

namespace POC5.Data
{
    /// <summary>
    /// 설비의 종류를 식별하는 열거형.
    /// 런타임에서 설비 종류별로 동작을 분기할 때 사용한다.
    /// </summary>
    public enum FacilityType
    {
        Pump,       // 양수기 - 물 스피릿 필요, 물 생산
        Cultivator, // 재배기 - 풀 스피릿 필요, 씨앗 생산
        Farm,       // 농장 - 씨앗 + 물 입력 → 작물 생산
        Warehouse,  // 창고 - 자원 저장
        Market,     // 시장 - 자원 → 돈 변환
        Kitchen     // 주방 - 불 스피릿 필요, 작물 → 식량 생산
    }

    /// <summary>
    /// 설비의 입력 또는 출력 포트 하나를 정의하는 직렬화 가능 구조체.
    /// 포트가 처리하는 자원 종류와 최대 저장 용량을 담는다.
    /// </summary>
    [Serializable]
    public struct PortDefinition
    {
        [Tooltip("이 포트가 처리하는 자원의 종류.")]
        public ResourceType resourceType;

        [Tooltip("이 포트에 최대 저장할 수 있는 자원 수량.")]
        public int capacity;
    }

    /// <summary>
    /// 설비 종류별 정적 메타데이터를 담는 ScriptableObject.
    /// 설비의 입출력 포트 구성, 생산 속도, 스피릿 요구 조건, 구매 가격 등을
    /// 코드 수정 없이 인스펙터에서 조정할 수 있다.
    ///
    /// 에셋 생성: 프로젝트 창 우클릭 → Create → POC5 → Data → FacilityData
    ///
    /// 실무 팁: 설비 종류마다 SO 에셋을 하나씩 만들어 두면(PumpData, FarmData 등)
    /// 디자이너가 코드 없이 설비를 추가하거나 수치를 조정할 수 있다.
    /// </summary>
    [CreateAssetMenu(fileName = "FacilityData_New", menuName = "POC5/Data/FacilityData")]
    public class FacilityData : ScriptableObject
    {
        [Header("기본 정보")]
        [Tooltip("설비의 종류 식별자.")]
        [SerializeField] private FacilityType _facilityType;

        [Tooltip("UI에 표시할 설비 이름 (예: 양수기, 농장).")]
        [SerializeField] private string _displayName;

        [Tooltip("UI에 표시할 설비 아이콘 스프라이트.")]
        [SerializeField] private Sprite _icon;

        [Header("포트 구성")]
        [Tooltip("입력 포트 목록. 배열 크기가 입력 포트의 개수가 된다.")]
        [SerializeField] private PortDefinition[] _inputPorts;

        [Tooltip("출력 포트 목록. 배열 크기가 출력 포트의 개수가 된다.")]
        [SerializeField] private PortDefinition[] _outputPorts;

        [Header("생산 설정")]
        [Tooltip("틱 1회당 기본 생산량. 스피릿의 WorkPower가 곱해져 실제 생산량이 결정된다.")]
        [SerializeField] private float _baseProductionPerTick = 1f;

        [Header("스피릿 배치 설정")]
        [Tooltip("이 설비가 작동하려면 스피릿이 배치되어야 하는지 여부.\n" +
                 "양수기/재배기/주방은 true, 농장/창고/시장은 false.")]
        [SerializeField] private bool _requiresSpirit = false;

        [Tooltip("배치 가능한 스피릿 속성. RequiresSpirit이 true일 때만 유효하다.")]
        [SerializeField] private SpiritElement _requiredSpiritElement;

        [Header("상점 설정")]
        [Tooltip("상점에서 이 설비를 구매할 때 필요한 돈.")]
        [SerializeField] private int _purchasePrice = 100;

        /// <summary>설비의 종류 식별자.</summary>
        public FacilityType FacilityType => _facilityType;

        /// <summary>UI에 표시할 설비 이름.</summary>
        public string DisplayName => _displayName;

        /// <summary>UI에 표시할 아이콘 스프라이트.</summary>
        public Sprite Icon => _icon;

        /// <summary>입력 포트 정의 배열. 배열 길이가 입력 포트 개수와 같다.</summary>
        public PortDefinition[] InputPorts => _inputPorts;

        /// <summary>출력 포트 정의 배열. 배열 길이가 출력 포트 개수와 같다.</summary>
        public PortDefinition[] OutputPorts => _outputPorts;

        /// <summary>틱 1회당 기본 생산량. 스피릿의 WorkPower와 곱해서 실제 생산량이 결정된다.</summary>
        public float BaseProductionPerTick => _baseProductionPerTick;

        /// <summary>스피릿 배치가 있어야 작동하는 설비인지 여부.</summary>
        public bool RequiresSpirit => _requiresSpirit;

        /// <summary>배치 가능한 스피릿 속성 (RequiresSpirit이 true일 때만 유효).</summary>
        public SpiritElement RequiredSpiritElement => _requiredSpiritElement;

        /// <summary>상점 구매 가격.</summary>
        public int PurchasePrice => _purchasePrice;
    }
}
