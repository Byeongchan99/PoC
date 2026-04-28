using UnityEngine;
using POC5.Data;
using POC5.Graph;
using POC5.Runtime;

namespace POC5.UI
{
    /// <summary>
    /// 씬 초기화를 담당하는 매니저.
    /// Inspector에서 지정한 FacilityData SO와 SpiritData SO를 이용해
    /// 설비 노드와 UI 카드를 생성하고 자원 흐름 연결을 사전 설정한다.
    ///
    /// 사용법:
    ///   씬에 빈 GameObject를 만들고 이 컴포넌트를 붙인다.
    ///   Inspector에서 FacilityData SO 5개, SpiritData SO 2개,
    ///   ResourceFlowSystem, Canvas를 연결한다.
    ///
    /// 4단계에서 UI를 통해 연결선을 직접 생성할 수 있게 되면
    ///   WireConnections() 호출을 제거하거나 조건부로 유지한다.
    /// </summary>
    public class GameSceneManager : MonoBehaviour
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

        [Header("씬 참조")]
        [SerializeField] private ResourceFlowSystem _flowSystem;
        [SerializeField] private Canvas _canvas;

        private void Start()
        {
            if (!ValidateReferences()) return;
            SetupScene();
        }

        /// <summary>
        /// Inspector 참조가 모두 설정됐는지 확인한다.
        /// 빠진 항목이 있으면 에러를 출력하고 false를 반환한다.
        /// </summary>
        private bool ValidateReferences()
        {
            bool ok = true;
            if (_pumpData == null)       { Debug.LogError("[GameSceneManager] _pumpData 없음");       ok = false; }
            if (_cultivatorData == null) { Debug.LogError("[GameSceneManager] _cultivatorData 없음"); ok = false; }
            if (_farmData == null)       { Debug.LogError("[GameSceneManager] _farmData 없음");       ok = false; }
            if (_warehouseData == null)  { Debug.LogError("[GameSceneManager] _warehouseData 없음");  ok = false; }
            if (_marketData == null)     { Debug.LogError("[GameSceneManager] _marketData 없음");     ok = false; }
            if (_waterSpiritData == null){ Debug.LogError("[GameSceneManager] _waterSpiritData 없음");ok = false; }
            if (_grassSpiritData == null){ Debug.LogError("[GameSceneManager] _grassSpiritData 없음");ok = false; }
            if (_flowSystem == null)     { Debug.LogError("[GameSceneManager] _flowSystem 없음");     ok = false; }
            if (_canvas == null)         { Debug.LogError("[GameSceneManager] _canvas 없음");         ok = false; }
            return ok;
        }

        /// <summary>
        /// 설비 노드를 생성하고, 스피릿을 배치하고, 자원 흐름 연결을 설정한다.
        /// </summary>
        private void SetupScene()
        {
            // 설비 생성. 위치는 Canvas 중심(0,0) 기준 픽셀 좌표
            var pump       = CreateFacility(_pumpData,       new Vector2(-360f,  80f));
            var cultivator = CreateFacility(_cultivatorData, new Vector2(-360f, -80f));
            var farm       = CreateFacility(_farmData,       new Vector2(-100f,   0f));
            var warehouse  = CreateFacility(_warehouseData,  new Vector2( 160f,   0f));
            var market     = CreateFacility(_marketData,     new Vector2( 400f,   0f));

            // 스피릿 배치: 양수기 → 물 스피릿, 재배기 → 풀 스피릿
            pump.GraphNode.AssignSpirit(_waterSpiritData);
            cultivator.GraphNode.AssignSpirit(_grassSpiritData);

            // 그래프 연결 사전 설정 (4단계 UI 연결이 추가되기 전까지 동작 검증용)
            WireConnections(pump, cultivator, farm, warehouse, market);

            Debug.Log("[GameSceneManager] 씬 초기화 완료");
        }

        /// <summary>
        /// 설비 간 자원 흐름 연결을 설정한다.
        ///   양수기 (물) → 농장 입력 (물)
        ///   재배기 (씨앗) → 농장 입력 (씨앗)
        ///   농장 출력 (작물) → 창고 입력
        ///   창고 출력 → 시장 입력
        /// </summary>
        private void WireConnections(
            FacilityNode pump,
            FacilityNode cultivator,
            FacilityNode farm,
            FacilityNode warehouse,
            FacilityNode market)
        {
            NodeGraph graph = _flowSystem.Graph;

            TryWire(graph,
                pump.GraphNode.GetOutputPort(ResourceType.Water),
                farm.GraphNode.GetInputPort(ResourceType.Water));

            TryWire(graph,
                cultivator.GraphNode.GetOutputPort(ResourceType.Seed),
                farm.GraphNode.GetInputPort(ResourceType.Seed));

            TryWire(graph,
                farm.GraphNode.GetOutputPort(ResourceType.Crop),
                warehouse.GraphNode.InputPorts.Count > 0
                    ? warehouse.GraphNode.InputPorts[0] : null);

            TryWire(graph,
                warehouse.GraphNode.OutputPorts.Count > 0
                    ? warehouse.GraphNode.OutputPorts[0] : null,
                market.GraphNode.InputPorts.Count > 0
                    ? market.GraphNode.InputPorts[0] : null);
        }

        /// <summary>
        /// 포트 연결을 시도하고 실패하면 에러를 출력한다.
        /// TryConnect가 null 포트를 자체적으로 처리하므로 null 전달도 허용한다.
        /// </summary>
        private static void TryWire(NodeGraph graph, Port output, Port input)
        {
            if (!graph.TryConnect(output, input, out _))
                Debug.LogError(
                    $"[GameSceneManager] 연결 실패: " +
                    $"{output?.ResourceType.ToString() ?? "null"} → " +
                    $"{input?.ResourceType.ToString() ?? "null"}");
        }

        /// <summary>
        /// FacilityData를 받아 설비 노드 GameObject를 생성하고 Canvas에 배치한다.
        /// FacilityNode(로직) + FacilityNodeView(UI) + NodeDragHandler(드래그)를 조합한다.
        /// </summary>
        private FacilityNode CreateFacility(FacilityData data, Vector2 canvasPosition)
        {
            var go = new GameObject(data.DisplayName, typeof(RectTransform));
            go.transform.SetParent(_canvas.transform, false);

            // Canvas 중심 기준으로 카드 초기 위치를 설정한다
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = canvasPosition;

            // 로직 컴포넌트: 그래프 노드와 포트를 초기화한다
            var facilityNode = go.AddComponent<FacilityNode>();
            facilityNode.Initialize(data);

            // UI 컴포넌트: 카드 배경과 내부 요소(헤더, 아이콘, 포트, 버튼)를 생성한다
            var view = go.AddComponent<FacilityNodeView>();
            view.Initialize(facilityNode);

            // 드래그 핸들러: 카드 배경을 드래그하면 카드가 이동한다
            go.AddComponent<NodeDragHandler>();

            _flowSystem.RegisterFacility(facilityNode);
            return facilityNode;
        }
    }
}
