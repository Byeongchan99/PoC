using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using POC5.Graph;
using POC5.Runtime;

namespace POC5.UI
{
    /// <summary>
    /// 포트 드래그-드롭 연결 로직을 담당한다.
    ///
    /// 동작 흐름:
    ///   1. 포트 원 위에서 드래그 시작 → 마우스를 따라다니는 프리뷰 선 표시
    ///   2. 드래그 중 → 프리뷰 선이 마우스 위치까지 실시간으로 업데이트
    ///   3. 다른 포트 원에 드롭 → 연결 검증 후 성공 시 영구 연결선 고정
    ///   4. 빈 곳에 드롭 → 취소, 프리뷰 선 제거
    ///
    /// 연결 검증은 NodeGraph.TryConnect()가 처리한다:
    ///   출력 포트 → 입력 포트 방향, 같은 자원 타입만 연결 가능.
    ///
    /// 실무 팁: 연결선이 많아지면 오브젝트 풀(Object Pool)을 써서
    ///   ConnectionLineView 인스턴스를 재사용하면 GC 부담을 줄일 수 있다.
    /// </summary>
    public class PortConnectHandler : MonoBehaviour
    {
        [Header("씬 참조")]
        [Tooltip("NodeGraph를 보유한 ResourceFlowSystem.")]
        [SerializeField] private ResourceFlowSystem _flowSystem;

        [Tooltip("GraphicRaycaster가 붙어 있는 Canvas.")]
        [SerializeField] private Canvas _canvas;

        [Header("연결선 시각 설정")]
        [Tooltip("드래그 중 표시되는 프리뷰 선의 색상.")]
        [SerializeField] private Color _previewColor = new Color(1f, 1f, 1f, 0.45f);

        [Tooltip("연결이 완성됐을 때 표시되는 선의 색상.")]
        [SerializeField] private Color _connectionColor = new Color(0.4f, 0.9f, 1f, 1f);

        [Tooltip("연결선의 굵기 (픽셀).")]
        [SerializeField] private float _lineWidth = 3f;

        // 연결선들을 담는 Canvas 자식 오브젝트. Awake에서 자동 생성된다
        private Transform _connectionLayer;

        // 드래그 중인 출발 포트 (드래그가 없으면 null)
        private PortView _pendingSource;

        // 드래그 중 마우스를 따라다니는 임시 선
        private ConnectionLineView _previewLine;

        // GraphicRaycaster: 마우스 위치의 UI 오브젝트를 찾는 데 사용한다
        private GraphicRaycaster _raycaster;

        // 완성된 연결 목록. 카드 이동 시 선 끝점을 매 프레임 갱신하는 데 필요하다
        private readonly List<ConnectionEntry> _connections = new List<ConnectionEntry>();

        /// <summary>연결 하나를 그래프 데이터와 선 뷰를 묶어 저장하는 구조체.</summary>
        private struct ConnectionEntry
        {
            public ConnectionLineView LineView;
            public PortView           OutputPortView;
            public PortView           InputPortView;
        }

        private void Awake()
        {
            _raycaster = _canvas.GetComponent<GraphicRaycaster>();

            // 연결선 전용 레이어를 Canvas 아래 첫 번째 자식으로 생성한다
            // 카드보다 먼저 렌더링되어 카드 아래에 선이 그려진다
            var layerGo = new GameObject("ConnectionLayer", typeof(RectTransform));
            layerGo.transform.SetParent(_canvas.transform, false);
            layerGo.transform.SetAsLastSibling();

            var rt = layerGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            _connectionLayer = layerGo.transform;
        }

        private void Update()
        {
            // 카드가 드래그로 이동하면 연결선 양쪽 끝점도 같이 업데이트해야 한다
            foreach (var entry in _connections)
            {
                entry.LineView.SetWorldPoints(
                    entry.OutputPortView.PortWorldPosition,
                    entry.InputPortView.PortWorldPosition);
            }
        }

        /// <summary>
        /// PortView를 등록하고 드래그 이벤트를 구독한다.
        /// GameSceneManager가 설비를 생성한 직후 호출한다.
        /// </summary>
        public void RegisterPortView(PortView portView)
        {
            portView.OnPortDragBegin  += HandleDragBegin;
            portView.OnPortDragUpdate += HandleDragUpdate;
            portView.OnPortDragEnd    += HandleDragEnd;
        }

        /// <summary>
        /// 드래그 시작: 출발 포트를 기억하고 프리뷰 선을 생성한다.
        /// </summary>
        private void HandleDragBegin(PortView source, PointerEventData data)
        {
            _pendingSource = source;
            _previewLine   = CreateLine(_previewColor);
            _previewLine.SetWorldPoints(source.PortWorldPosition, source.PortWorldPosition);
        }

        /// <summary>
        /// 드래그 중: 프리뷰 선의 끝점을 마우스 위치로 갱신한다.
        /// Screen Space - Overlay Canvas에서 화면 좌표 = 세계 좌표이다.
        /// </summary>
        private void HandleDragUpdate(PortView source, PointerEventData data)
        {
            if (_previewLine == null) return;

            Vector3 mouseWorld = new Vector3(data.position.x, data.position.y, 0f);
            _previewLine.SetWorldPoints(source.PortWorldPosition, mouseWorld);
        }

        /// <summary>
        /// 드래그 종료: 마우스 아래 포트를 찾아 연결을 시도한다.
        /// 성공하면 영구 연결선을 생성하고, 실패하거나 빈 곳이면 취소한다.
        /// </summary>
        private void HandleDragEnd(PortView source, PointerEventData data)
        {
            if (_pendingSource == null) return;

            PortView target = FindPortViewAtScreenPoint(data.position);

            if (target != null)
                TryCreateConnection(source, target);

            // 드래그 상태 초기화
            if (_previewLine != null)
            {
                Destroy(_previewLine.gameObject);
                _previewLine = null;
            }
            _pendingSource = null;
        }

        /// <summary>
        /// 두 포트 사이의 연결을 시도한다.
        /// 방향(출력→입력)은 자동으로 판별하며, 타입 검증은 NodeGraph.TryConnect가 처리한다.
        /// </summary>
        private void TryCreateConnection(PortView a, PortView b)
        {
            // 출력 포트와 입력 포트를 방향에 맞게 분류한다
            PortView outputView = a.Port.Direction == PortDirection.Output ? a : b;
            PortView inputView  = a.Port.Direction == PortDirection.Input  ? a : b;

            // 두 포트가 모두 같은 방향이면 연결 불가
            if (outputView.Port.Direction != PortDirection.Output ||
                inputView.Port.Direction  != PortDirection.Input)
            {
                Debug.LogWarning("[PortConnectHandler] 연결 실패: 출력 포트와 입력 포트가 필요합니다.");
                return;
            }

            if (!_flowSystem.Graph.TryConnect(outputView.Port, inputView.Port, out _))
                return; // TryConnect가 내부에서 경고를 출력한다

            // 영구 연결선 생성 및 등록
            var line = CreateLine(_connectionColor);
            line.LineWidth = _lineWidth;
            line.SetWorldPoints(outputView.PortWorldPosition, inputView.PortWorldPosition);

            _connections.Add(new ConnectionEntry
            {
                LineView       = line,
                OutputPortView = outputView,
                InputPortView  = inputView
            });
        }

        /// <summary>
        /// 화면 좌표 아래에 있는 PortView를 반환한다.
        /// GraphicRaycaster로 UI 오브젝트를 감지하고, 계층에서 PortView를 찾는다.
        /// </summary>
        private PortView FindPortViewAtScreenPoint(Vector2 screenPoint)
        {
            var results   = new List<RaycastResult>();
            var eventData = new PointerEventData(EventSystem.current) { position = screenPoint };
            _raycaster.Raycast(eventData, results);

            foreach (var result in results)
            {
                // 레이캐스트 대상의 GO 또는 부모에서 PortView를 찾는다
                var portView = result.gameObject.GetComponentInParent<PortView>();
                // 자기 자신은 제외한다
                if (portView != null && portView != _pendingSource)
                    return portView;
            }
            return null;
        }

        /// <summary>
        /// ConnectionLayer 아래에 ConnectionLineView GameObject를 생성한다.
        /// </summary>
        private ConnectionLineView CreateLine(Color lineColor)
        {
            var go = new GameObject("ConnectionLine",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(ConnectionLineView));
            go.transform.SetParent(_connectionLayer, false);

            // Canvas 전체를 덮도록 스트레치 설정
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var line = go.GetComponent<ConnectionLineView>();
            line.color     = lineColor;
            line.LineWidth = _lineWidth;
            return line;
        }
    }
}
