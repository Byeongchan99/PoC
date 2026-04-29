using System;
using System.Collections.Generic;
using UnityEngine;
using POC5.Data;
using POC5.Graph;
using POC5.Runtime;

namespace POC5.UI
{
    /// <summary>
    /// 씬 초기화를 담당하는 매니저.
    /// Inspector에서 지정한 FacilityData SO와 SpiritData SO를 이용해
    /// FacilityNodeCard 프리팹을 인스턴스화하고 자원 흐름 연결을 사전 설정한다.
    ///
    /// 사용법:
    ///   씬에 빈 GameObject를 만들고 이 컴포넌트를 붙인다.
    ///   Inspector에서 FacilityData SO 5개, SpiritData SO 2개,
    ///   FacilityNodeCard 프리팹, ResourceFlowSystem, Canvas를 연결한다.
    /// </summary>
    public class GameSceneManager : MonoBehaviour
    {
        /// <summary>
        /// 정령 카드 하나의 설정을 묶는 구조체.
        /// 정령별로 다른 프리팹(색상·디자인)을 사용할 수 있도록
        /// 데이터, 프리팹, 시작 위치를 함께 지정한다.
        /// </summary>
        [Serializable]
        public struct SpiritCardEntry
        {
            [Tooltip("이 카드에 바인딩할 스피릿 데이터 SO.")]
            public SpiritData data;

            [Tooltip("정령별 전용 프리팹. SpiritCardView와 SpiritDragHandler가 붙어 있어야 한다.")]
            public SpiritCardView prefab;

            [Tooltip("씬 시작 시 카드가 놓일 Canvas 기준 좌표 (픽셀).")]
            public Vector2 startPosition;
        }

        [Header("카드 프리팹")]
        [Tooltip("FacilityNodeView가 붙은 카드 프리팹. 모든 설비에 동일한 프리팹을 사용한다.")]
        [SerializeField] private FacilityNodeView _cardPrefab;

        [Header("스피릿 카드 목록")]
        [Tooltip("정령마다 별도 프리팹과 시작 위치를 설정한다. 원소 추가 시 항목을 늘린다.")]
        [SerializeField] private SpiritCardEntry[] _spiritCards;

        [Header("연결 시스템")]
        [Tooltip("포트 드래그 연결을 처리하는 핸들러.")]
        [SerializeField] private PortConnectHandler _portConnectHandler;

        [Header("재화 시스템")]
        [Tooltip("골드 잔액을 관리하는 CurrencySystem.")]
        [SerializeField] private CurrencySystem _currencySystem;

        [Tooltip("시장 판매 수익을 처리하는 MarketSalesHandler.")]
        [SerializeField] private MarketSalesHandler _marketSalesHandler;

        [Tooltip("활성화하면 씬 시작 시 연결선을 자동으로 설정한다. 4단계 UI 테스트 전 그래프 동작 확인용.")]
        [SerializeField] private bool _preWireConnections = false;

        [Tooltip("활성화하면 씬 시작 시 스피릿을 자동으로 배치한다. 5단계 드래그 테스트 전 동작 확인용.")]
        [SerializeField] private bool _preAssignSpirits = false;

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

        // 스피릿 사전 배치 시 FacilityNodeView 갱신에 사용하는 매핑
        private readonly Dictionary<FacilityNode, FacilityNodeView> _facilityViewMap
            = new Dictionary<FacilityNode, FacilityNodeView>();

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
            if (_cardPrefab == null)         { Debug.LogError("[GameSceneManager] _cardPrefab 없음");         ok = false; }
            if (_portConnectHandler == null) { Debug.LogError("[GameSceneManager] _portConnectHandler 없음"); ok = false; }
            if (_spiritCards == null || _spiritCards.Length == 0)
            {
                Debug.LogWarning("[GameSceneManager] _spiritCards가 비어 있습니다. 스피릿 카드가 생성되지 않습니다.");
            }
            if (_pumpData == null)       { Debug.LogError("[GameSceneManager] _pumpData 없음");       ok = false; }
            if (_cultivatorData == null) { Debug.LogError("[GameSceneManager] _cultivatorData 없음"); ok = false; }
            if (_farmData == null)       { Debug.LogError("[GameSceneManager] _farmData 없음");       ok = false; }
            if (_warehouseData == null)  { Debug.LogError("[GameSceneManager] _warehouseData 없음");  ok = false; }
            if (_marketData == null)     { Debug.LogError("[GameSceneManager] _marketData 없음");     ok = false; }
            if (_waterSpiritData == null){ Debug.LogError("[GameSceneManager] _waterSpiritData 없음");ok = false; }
            if (_grassSpiritData == null){ Debug.LogError("[GameSceneManager] _grassSpiritData 없음");ok = false; }
            if (_flowSystem == null)      { Debug.LogError("[GameSceneManager] _flowSystem 없음");      ok = false; }
            if (_canvas == null)          { Debug.LogError("[GameSceneManager] _canvas 없음");          ok = false; }
            if (_currencySystem == null)      { Debug.LogError("[GameSceneManager] _currencySystem 없음");      ok = false; }
            if (_marketSalesHandler == null)  { Debug.LogError("[GameSceneManager] _marketSalesHandler 없음");  ok = false; }
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

            // 정령 카드 생성. _spiritCards 배열에 등록된 순서대로 인스턴스화한다
            if (_spiritCards != null)
                foreach (var entry in _spiritCards)
                    CreateSpiritCard(entry);

            // _preAssignSpirits가 true일 때만 스피릿을 자동 배치한다
            // 5단계 드래그로 직접 배치하려면 false로 둔다
            if (_preAssignSpirits)
                PreAssignSpirits(pump, cultivator);

            // _preWireConnections가 true일 때만 자동으로 연결선을 설정한다
            // 4단계 UI로 연결선을 직접 만들려면 false로 둔다
            if (_preWireConnections)
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
        /// 상점에서 설비를 구매했을 때 ShopPanel이 호출해 설비 카드를 씬에 생성한다.
        /// </summary>
        public FacilityNode SpawnFacility(FacilityData data, Vector2 canvasPosition)
        {
            return CreateFacility(data, canvasPosition);
        }

        /// <summary>
        /// FacilityNodeCard 프리팹을 인스턴스화하고 Canvas에 배치한다.
        /// 프리팹에 이미 FacilityNode, FacilityNodeView, NodeDragHandler가 붙어 있어야 한다.
        /// </summary>
        private FacilityNode CreateFacility(FacilityData data, Vector2 canvasPosition)
        {
            var view = Instantiate(_cardPrefab, _canvas.transform);
            view.name = data.DisplayName;

            // Canvas 중심 기준으로 카드 초기 위치를 설정한다
            var rt = view.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = canvasPosition;

            // 프리팹에 FacilityNode가 붙어 있으므로 GetComponent로 가져온다
            var facilityNode = view.GetComponent<FacilityNode>();
            if (facilityNode == null)
            {
                Debug.LogError($"[GameSceneManager] {_cardPrefab.name} 프리팹에 FacilityNode 컴포넌트가 없습니다.");
                return null;
            }

            facilityNode.Initialize(data);
            view.Initialize(facilityNode);
            view.SetupUpgradeButton(facilityNode, _currencySystem);

            _flowSystem.RegisterFacility(facilityNode);
            _facilityViewMap[facilityNode] = view;

            // Market 타입이면 판매 이벤트를 MarketSalesHandler에 연결한다
            if (data.FacilityType == FacilityType.Market)
                _marketSalesHandler.RegisterMarketNode(facilityNode.GraphNode);

            // 이 카드의 모든 포트 뷰를 PortConnectHandler에 등록한다
            foreach (var portView in view.PortViews)
                _portConnectHandler.RegisterPortView(portView);

            return facilityNode;
        }

        /// <summary>
        /// _spiritCards 배열의 설정을 읽어 스피릿을 양수기·재배기에 사전 배치한다.
        /// Water 원소 → 양수기, Grass 원소 → 재배기에 매핑된다.
        /// </summary>
        private void PreAssignSpirits(FacilityNode pump, FacilityNode cultivator)
        {
            if (_spiritCards == null) return;
            foreach (var entry in _spiritCards)
            {
                if (entry.data == null) continue;
                switch (entry.data.Element)
                {
                    case POC5.Data.SpiritElement.Water:
                        pump.GraphNode.AssignSpirit(entry.data);
                        _facilityViewMap[pump].UpdateSpiritDisplay(entry.data);
                        break;
                    case POC5.Data.SpiritElement.Grass:
                        cultivator.GraphNode.AssignSpirit(entry.data);
                        _facilityViewMap[cultivator].UpdateSpiritDisplay(entry.data);
                        break;
                }
            }
        }

        /// <summary>
        /// SpiritCardEntry 설정에 따라 스피릿 카드 프리팹을 인스턴스화하고 Canvas에 배치한다.
        /// </summary>
        private void CreateSpiritCard(SpiritCardEntry entry)
        {
            if (entry.data == null || entry.prefab == null)
            {
                Debug.LogWarning("[GameSceneManager] SpiritCardEntry에 data 또는 prefab이 없습니다.");
                return;
            }

            var card = Instantiate(entry.prefab, _canvas.transform);
            card.name = entry.data.DisplayName;

            var rt = card.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = entry.startPosition;

            card.Initialize(entry.data);
        }
    }
}
