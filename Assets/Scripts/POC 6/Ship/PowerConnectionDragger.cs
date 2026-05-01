using UnityEngine;
using UnityEngine.InputSystem;

namespace POC6
{
    /// <summary>
    /// Build Phase에서 마우스 드래그로 동력 연결을 생성하는 UI 컨트롤러입니다.
    /// 노드를 클릭해서 드래그를 시작하고, 연결 가능한 노드 위에서 마우스를 놓으면 연결됩니다.
    /// 드래그 중에는 임시 미리보기 선이 마우스까지 표시됩니다.
    /// </summary>
    public class PowerConnectionDragger : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private ShipGrid _shipGrid;
        [SerializeField] private PowerGraph _powerGraph;
        [SerializeField] private Camera _mainCamera;

        [Header("드래그 선 설정")]
        [SerializeField] private Material _previewLineMaterial;
        [SerializeField] private float _lineWidth = 0.05f;

        [Tooltip("연결 가능한 노드 위에 있을 때 미리보기 선 색상")]
        [SerializeField] private Color _validDragColor = new Color(0f, 1f, 0f, 0.8f);

        [Tooltip("연결 불가 상태일 때 미리보기 선 색상")]
        [SerializeField] private Color _invalidDragColor = new Color(1f, 0f, 0f, 0.8f);

        [Tooltip("드래그 미리보기 선 Sorting Layer 이름")]
        [SerializeField] private string _sortingLayerName = "Default";

        [Tooltip("드래그 미리보기 선 Sorting Order. 노드보다 앞에 표시되도록 높게 설정합니다.")]
        [SerializeField] private int _sortingOrder = 2;

        // 드래그 시작 노드
        private PlacedNode _dragFromNode;

        // 드래그 중 마우스 위치의 미리보기 선
        private LineRenderer _previewLine;

        // 드래그 활성 여부
        private bool _isDragging = false;

        private void Awake()
        {
            if (_mainCamera == null)
                _mainCamera = Camera.main;

            CreatePreviewLine();
        }

        private void Update()
        {
            if (!_isDragging) return;

            // 드래그 중: 미리보기 선을 마우스까지 업데이트
            UpdatePreviewLine();

            // 마우스 버튼 놓으면 연결 시도
            if (Mouse.current.leftButton.wasReleasedThisFrame)
                TryFinishDrag();
        }

        /// <summary>
        /// 드래그 시작 요청. 노드 클릭 감지는 NodeClickDetector에서 이 메서드를 호출합니다.
        /// </summary>
        public void BeginDrag(PlacedNode fromNode)
        {
            if (fromNode == null) return;

            // 일반 노드는 동력 연결의 출발점이 될 수 없음
            if (fromNode.Data.NodeType == NodeType.Normal) return;

            _dragFromNode = fromNode;
            _isDragging = true;

            // 미리보기 선 활성화
            _previewLine.enabled = true;
            _previewLine.SetPosition(0, _shipGrid.NodeCenterToWorld(fromNode));
        }

        /// <summary>
        /// 진행 중인 드래그를 취소합니다.
        /// </summary>
        public void CancelDrag()
        {
            _isDragging = false;
            _dragFromNode = null;
            _previewLine.enabled = false;
        }

        /// <summary>
        /// 마우스 버튼을 놓았을 때 연결 대상 노드를 탐지해서 연결을 시도합니다.
        /// </summary>
        private void TryFinishDrag()
        {
            Vector3 mouseWorld = GetMouseWorldPosition();
            PlacedNode toNode = GetNodeAtWorldPos(mouseWorld);

            if (toNode != null && toNode != _dragFromNode)
            {
                bool success = _powerGraph.TryAddConnection(_dragFromNode, toNode);

                if (!success)
                    Debug.Log($"[PowerConnectionDragger] 연결 실패: {_dragFromNode.Data.NodeName} -> {toNode.Data.NodeName}");
            }

            CancelDrag();
        }

        /// <summary>
        /// 드래그 중 미리보기 선의 끝점을 현재 마우스 위치로 업데이트합니다.
        /// 마우스 아래에 연결 가능한 노드가 있으면 선 색상을 초록으로, 아니면 빨강으로 표시합니다.
        /// </summary>
        private void UpdatePreviewLine()
        {
            Vector3 mouseWorld = GetMouseWorldPosition();
            _previewLine.SetPosition(1, mouseWorld);

            // 마우스 아래 노드 확인 후 색상 변경
            PlacedNode hovered = GetNodeAtWorldPos(mouseWorld);
            bool canConnect = hovered != null && _powerGraph.IsValidConnection(_dragFromNode, hovered);

            _previewLine.startColor = canConnect ? _validDragColor : _invalidDragColor;
            _previewLine.endColor = canConnect ? _validDragColor : _invalidDragColor;
        }

        /// <summary>
        /// 드래그 미리보기에 사용할 LineRenderer를 미리 생성해둡니다.
        /// </summary>
        private void CreatePreviewLine()
        {
            var obj = new GameObject("PowerDragPreviewLine");
            obj.transform.SetParent(transform);

            _previewLine = obj.AddComponent<LineRenderer>();
            _previewLine.positionCount = 2;
            _previewLine.startWidth = _lineWidth;
            _previewLine.endWidth = _lineWidth;

            if (_previewLineMaterial != null)
                _previewLine.material = _previewLineMaterial;

            _previewLine.useWorldSpace = true;
            _previewLine.sortingLayerName = _sortingLayerName;
            _previewLine.sortingOrder = _sortingOrder;
            _previewLine.enabled = false;
        }

        /// <summary>
        /// 월드 좌표에 있는 노드를 그리드를 통해 검색합니다.
        /// </summary>
        private PlacedNode GetNodeAtWorldPos(Vector3 worldPos)
        {
            Vector2Int cell = _shipGrid.WorldToGrid(worldPos);
            return _shipGrid.GetNodeAt(cell);
        }

        /// <summary>
        /// 현재 마우스 위치를 월드 좌표로 반환합니다.
        /// </summary>
        private Vector3 GetMouseWorldPosition()
        {
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector3 pos = new Vector3(mouseScreenPos.x, mouseScreenPos.y, Mathf.Abs(_mainCamera.transform.position.z));
            return _mainCamera.ScreenToWorldPoint(pos);
        }
    }
}
