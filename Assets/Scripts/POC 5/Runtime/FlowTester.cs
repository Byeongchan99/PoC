using UnityEngine;
using POC5.Data;
using POC5.Graph;

namespace POC5.Runtime
{
    /// <summary>
    /// 2단계 콘솔 검증용 테스트 스크립트.
    /// 씬에 빈 게임 오브젝트를 만들고 이 컴포넌트를 붙인 뒤,
    /// Inspector에서 SO 데이터와 ResourceFlowSystem을 연결하면
    /// Start()에서 자동으로 그래프를 구성해 자원 흐름을 시작한다.
    ///
    /// 기대 콘솔 로그 (틱마다):
    ///   [양수기] Water +1 생산 (1/5)
    ///   [재배기] Seed  +1 생산 (1/5)
    ///   [농장]   Crop  +1 생산 (1/5)  ← 물 + 씨앗이 모두 도달한 틱부터
    ///   [시장]   Crop 판매 → 돈 획득  ← 창고를 거쳐 전달된 뒤
    ///
    /// 검증 완료 후 이 컴포넌트와 게임 오브젝트는 삭제해도 무방하다.
    /// </summary>
    public class FlowTester : MonoBehaviour
    {
        [Header("설비 데이터 (FacilityData SO)")]
        [SerializeField] private FacilityData _pumpData;
        [SerializeField] private FacilityData _cultivatorData;
        [SerializeField] private FacilityData _farmData;
        [SerializeField] private FacilityData _warehouseData;
        [SerializeField] private FacilityData _marketData;

        [Header("스피릿 데이터 (SpiritData SO)")]
        [SerializeField] private SpiritData _waterSpiritData;
        [SerializeField] private SpiritData _grassSpiritData;

        [Header("씬의 ResourceFlowSystem")]
        [SerializeField] private ResourceFlowSystem _flowSystem;

        private void Start()
        {
            if (!ValidateReferences()) return;
            BuildTestGraph();
        }

        /// <summary>
        /// Inspector 참조가 모두 설정됐는지 확인한다.
        /// 비어 있는 항목이 있으면 에러를 출력하고 false를 반환한다.
        /// </summary>
        private bool ValidateReferences()
        {
            bool valid = true;
            if (_pumpData == null)       { Debug.LogError("[FlowTester] _pumpData가 비어 있습니다.");       valid = false; }
            if (_cultivatorData == null) { Debug.LogError("[FlowTester] _cultivatorData가 비어 있습니다."); valid = false; }
            if (_farmData == null)       { Debug.LogError("[FlowTester] _farmData가 비어 있습니다.");       valid = false; }
            if (_warehouseData == null)  { Debug.LogError("[FlowTester] _warehouseData가 비어 있습니다.");  valid = false; }
            if (_marketData == null)     { Debug.LogError("[FlowTester] _marketData가 비어 있습니다.");     valid = false; }
            if (_waterSpiritData == null){ Debug.LogError("[FlowTester] _waterSpiritData가 비어 있습니다.");valid = false; }
            if (_grassSpiritData == null){ Debug.LogError("[FlowTester] _grassSpiritData가 비어 있습니다.");valid = false; }
            if (_flowSystem == null)     { Debug.LogError("[FlowTester] _flowSystem이 비어 있습니다.");     valid = false; }
            return valid;
        }

        /// <summary>
        /// POC 검증 시나리오 그래프를 구성한다.
        ///   양수기(물 스피릿) --물--→ 농장 입력(물)
        ///   재배기(풀 스피릿) --씨앗--→ 농장 입력(씨앗)
        ///   농장 출력(작물)   --작물--→ 창고 입력
        ///   창고 출력         --작물--→ 시장 입력
        /// </summary>
        private void BuildTestGraph()
        {
            // 설비 노드 생성 및 그래프 등록
            var pump       = CreateFacility("Pump_Test",       _pumpData);
            var cultivator = CreateFacility("Cultivator_Test", _cultivatorData);
            var farm       = CreateFacility("Farm_Test",       _farmData);
            var warehouse  = CreateFacility("Warehouse_Test",  _warehouseData);
            var market     = CreateFacility("Market_Test",     _marketData);

            // 스피릿 배치
            pump.GraphNode.AssignSpirit(_waterSpiritData);
            cultivator.GraphNode.AssignSpirit(_grassSpiritData);

            // 연결 생성
            NodeGraph graph = _flowSystem.Graph;

            // 양수기(물) → 농장 물 입력 포트
            graph.TryConnect(
                pump.GraphNode.GetOutputPort(ResourceType.Water),
                farm.GraphNode.GetInputPort(ResourceType.Water),
                out _);

            // 재배기(씨앗) → 농장 씨앗 입력 포트
            graph.TryConnect(
                cultivator.GraphNode.GetOutputPort(ResourceType.Seed),
                farm.GraphNode.GetInputPort(ResourceType.Seed),
                out _);

            // 농장(작물) → 창고 입력 포트
            graph.TryConnect(
                farm.GraphNode.GetOutputPort(ResourceType.Crop),
                warehouse.GraphNode.InputPorts[0],
                out _);

            // 창고 출력 → 시장 입력 포트
            graph.TryConnect(
                warehouse.GraphNode.OutputPorts[0],
                market.GraphNode.InputPorts[0],
                out _);

            Debug.Log("[FlowTester] 테스트 그래프 구성 완료: 양수기 + 재배기 → 농장 → 창고 → 시장");
        }

        /// <summary>
        /// 빈 게임 오브젝트에 FacilityNode를 붙이고 ResourceFlowSystem에 등록한다.
        /// </summary>
        private FacilityNode CreateFacility(string goName, FacilityData data)
        {
            var go = new GameObject(goName);
            go.transform.SetParent(transform);

            var facilityNode = go.AddComponent<FacilityNode>();
            facilityNode.Initialize(data);

            _flowSystem.RegisterFacility(facilityNode);
            return facilityNode;
        }
    }
}
