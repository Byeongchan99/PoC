using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
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
    ///   5. 연결된 포트 원 우클릭 → 해당 포트의 모든 연결 해제
    ///
    /// 연결 검증은 NodeGraph.TryConnect()가 처리한다:
    ///   출력 포트 → 입력 포트 방향, 같은 자원 타입만 연결 가능.
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

        private Transform _connectionLayer;
        private PortView _pendingSource;
        private ConnectionLineView _previewLine;
        private GraphicRaycaster _raycaster;

        private readonly List<ConnectionEntry> _connections = new List<ConnectionEntry>();

        /// <summary>연결 하나의 그래프 데이터, 선 뷰, 포트 뷰를 묶어 저장하는 구조체.</summary>
        private struct ConnectionEntry
        {
            public Connection         GraphConnection;
            public ConnectionLineView LineView;
            public PortView           OutputPortView;
            public PortView           InputPortView;
        }

        private void Awake()
        {
            _raycaster = _canvas.GetComponent<GraphicRaycaster>();

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
            // 카드가 드래그로 이동하면 연결선 끝점도 같이 갱신한다
            foreach (var entry in _connections)
            {
                entry.LineView.SetWorldPoints(
                    entry.OutputPortView.PortWorldPosition,
                    entry.InputPortView.PortWorldPosition);
            }

            // 우클릭 시 마우스 아래 포트의 연결을 모두 해제한다
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
                TryDisconnectAtMousePosition();
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

        /// <summary>드래그 시작: 출발 포트를 기억하고 프리뷰 선을 생성한다.</summary>
        private void HandleDragBegin(PortView source, PointerEventData data)
        {
            _pendingSource = source;
            _previewLine   = CreateLine(_previewColor);
            _previewLine.SetWorldPoints(source.PortWorldPosition, source.PortWorldPosition);
        }

        /// <summary>드래그 중: 프리뷰 선의 끝점을 마우스 위치로 갱신한다.</summary>
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

            if (_previewLine != null)
            {
                Destroy(_previewLine.gameObject);
                _previewLine = null;
            }
            _pendingSource = null;
        }

        /// <summary>
        /// 두 포트 사이의 연결을 시도한다.
        /// 방향은 자동 판별하며, 타입 검증은 NodeGraph.TryConnect가 처리한다.
        /// </summary>
        private void TryCreateConnection(PortView a, PortView b)
        {
            PortView outputView = a.Port.Direction == PortDirection.Output ? a : b;
            PortView inputView  = a.Port.Direction == PortDirection.Input  ? a : b;

            if (outputView.Port.Direction != PortDirection.Output ||
                inputView.Port.Direction  != PortDirection.Input)
            {
                Debug.LogWarning("[PortConnectHandler] 연결 실패: 출력 포트와 입력 포트가 필요합니다.");
                return;
            }

            if (!_flowSystem.Graph.TryConnect(outputView.Port, inputView.Port, out var connection))
                return;

            var line = CreateLine(_connectionColor);
            line.LineWidth = _lineWidth;
            line.SetWorldPoints(outputView.PortWorldPosition, inputView.PortWorldPosition);

            _connections.Add(new ConnectionEntry
            {
                GraphConnection = connection,
                LineView        = line,
                OutputPortView  = outputView,
                InputPortView   = inputView
            });
        }

        /// <summary>
        /// 우클릭한 위치 아래에 있는 포트를 찾아 그 포트의 연결을 모두 해제한다.
        /// </summary>
        private void TryDisconnectAtMousePosition()
        {
            var results   = new List<RaycastResult>();
            var eventData = new PointerEventData(EventSystem.current)
                { position = Mouse.current.position.ReadValue() };
            _raycaster.Raycast(eventData, results);

            foreach (var result in results)
            {
                var portView = result.gameObject.GetComponentInParent<PortView>();
                if (portView != null)
                {
                    DisconnectAllFromPort(portView);
                    return;
                }
            }
        }

        /// <summary>
        /// 해당 포트와 연결된 모든 연결선을 그래프와 UI에서 제거한다.
        /// 뒤에서부터 순회해 인덱스 오류 없이 삭제한다.
        /// </summary>
        private void DisconnectAllFromPort(PortView portView)
        {
            for (int i = _connections.Count - 1; i >= 0; i--)
            {
                var entry = _connections[i];
                if (entry.OutputPortView != portView && entry.InputPortView != portView)
                    continue;

                _flowSystem.Graph.RemoveConnection(entry.GraphConnection);
                Destroy(entry.LineView.gameObject);
                _connections.RemoveAt(i);

                Debug.Log($"[PortConnectHandler] {portView.Port.ResourceType} 포트 연결 해제");
            }
        }

        /// <summary>
        /// 화면 좌표 아래에 있는 PortView를 반환한다. 드래그 출발 포트는 제외한다.
        /// </summary>
        private PortView FindPortViewAtScreenPoint(Vector2 screenPoint)
        {
            var results   = new List<RaycastResult>();
            var eventData = new PointerEventData(EventSystem.current) { position = screenPoint };
            _raycaster.Raycast(eventData, results);

            foreach (var result in results)
            {
                var portView = result.gameObject.GetComponentInParent<PortView>();
                if (portView != null && portView != _pendingSource)
                    return portView;
            }
            return null;
        }

        /// <summary>ConnectionLayer 아래에 ConnectionLineView GameObject를 생성한다.</summary>
        private ConnectionLineView CreateLine(Color lineColor)
        {
            var go = new GameObject("ConnectionLine",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(ConnectionLineView));
            go.transform.SetParent(_connectionLayer, false);

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
