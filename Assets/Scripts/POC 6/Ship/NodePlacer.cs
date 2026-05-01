using UnityEngine;
using UnityEngine.InputSystem;

namespace POC6
{
    /// <summary>
    /// Build Phase에서 마우스 입력을 받아 노드 배치를 처리합니다.
    /// 마우스 위치에 반투명 미리보기를 표시하고, 클릭 시 실제 배치를 수행합니다.
    /// R 키로 배치 전 노드를 90도 회전시킬 수 있습니다.
    /// </summary>
    public class NodePlacer : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private ShipGrid _shipGrid;
        [SerializeField] private Camera _mainCamera;

        [Header("미리보기 설정")]
        [Tooltip("배치 가능 상태 미리보기 색상 (반투명 초록)")]
        [SerializeField] private Color _validColor = new Color(0f, 1f, 0f, 0.5f);

        [Tooltip("배치 불가 상태 미리보기 색상 (반투명 빨강)")]
        [SerializeField] private Color _invalidColor = new Color(1f, 0f, 0f, 0.5f);

        // 현재 배치 대기 중인 노드 데이터 (null이면 배치 모드 비활성)
        private NodeData _pendingNode;

        // 현재 회전 단계
        private int _rotationStep = 0;

        // 미리보기 게임오브젝트 (마우스를 따라다니는 반투명 비주얼)
        private GameObject _previewInstance;

        // 미리보기 렌더러들 (색상 변경에 사용)
        private Renderer[] _previewRenderers;

        // 배치 모드 활성 여부
        private bool _isPlacing = false;

        private void Awake()
        {
            if (_mainCamera == null)
                _mainCamera = Camera.main;
        }

        private void Update()
        {
            if (!_isPlacing || _pendingNode == null) return;

            // R 키로 회전
            if (Keyboard.current.rKey.wasPressedThisFrame)
                RotatePreview();

            // 마우스 위치에 맞춰 미리보기 이동
            UpdatePreviewPosition();

            // 마우스 클릭으로 배치
            if (Mouse.current.leftButton.wasPressedThisFrame)
                TryPlaceNode();

            // 우클릭 또는 Escape로 배치 취소
            if (Mouse.current.rightButton.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
                CancelPlacement();
        }

        // ────────────────────────────────────────────────
        // 공개 API (DeckManager, NodeUpgradeUI 등에서 호출)
        // ────────────────────────────────────────────────

        /// <summary>
        /// 배치 모드를 시작합니다. 이 메서드 호출 후 마우스 클릭으로 배치합니다.
        /// 덱에서 카드를 선택하거나 인벤토리에서 노드를 꺼낼 때 사용합니다.
        /// </summary>
        public void BeginPlacement(NodeData nodeData)
        {
            // 이전 미리보기 정리
            CancelPlacement();

            _pendingNode = nodeData;
            _rotationStep = 0;
            _isPlacing = true;

            CreatePreview(nodeData);
        }

        /// <summary>
        /// 진행 중인 배치를 취소하고 미리보기를 제거합니다.
        /// </summary>
        public void CancelPlacement()
        {
            if (_previewInstance != null)
            {
                Destroy(_previewInstance);
                _previewInstance = null;
                _previewRenderers = null;
            }

            _pendingNode = null;
            _isPlacing = false;
            _rotationStep = 0;
        }

        /// <summary>
        /// 현재 배치 모드가 활성화되어 있는지 반환합니다.
        /// </summary>
        public bool IsPlacing => _isPlacing;

        // ────────────────────────────────────────────────
        // 내부 로직
        // ────────────────────────────────────────────────

        /// <summary>
        /// 미리보기 게임오브젝트를 생성합니다.
        /// 노드 비주얼 프리팹이 있으면 인스턴스화하고, 없으면 기본 큐브를 사용합니다.
        /// </summary>
        private void CreatePreview(NodeData nodeData)
        {
            GameObject source = nodeData.VisualPrefab;

            if (source != null)
            {
                _previewInstance = Instantiate(source);
            }
            else
            {
                // 비주얼 프리팹이 없을 때 임시 큐브 생성
                _previewInstance = GameObject.CreatePrimitive(PrimitiveType.Cube);
                // POC: 노드 크기에 맞춰 스케일 조정
                float cellSize = _shipGrid.CellSize;
                _previewInstance.transform.localScale = new Vector3(
                    nodeData.Size.x * cellSize * 0.9f,
                    nodeData.Size.y * cellSize * 0.9f,
                    0.1f
                );

                // 콜라이더 제거 (클릭 이벤트 방해 방지)
                var col = _previewInstance.GetComponent<Collider>();
                if (col != null) Destroy(col);
            }

            // 렌더러 수집
            _previewRenderers = _previewInstance.GetComponentsInChildren<Renderer>();
            SetPreviewColor(_validColor);
        }

        /// <summary>
        /// 마우스 위치 기준으로 미리보기의 월드 좌표와 색상을 갱신합니다.
        /// </summary>
        private void UpdatePreviewPosition()
        {
            if (_previewInstance == null) return;

            Vector3 mouseWorld = GetMouseWorldPosition();
            Vector2Int gridPos = _shipGrid.WorldToGrid(mouseWorld);

            // 그리드 좌표를 월드 좌표로 다시 변환하여 스냅 효과
            Vector3 snappedWorld = _shipGrid.GridToWorld(gridPos);
            _previewInstance.transform.position = snappedWorld;
            _previewInstance.transform.rotation = _shipGrid.transform.rotation
                * Quaternion.Euler(0f, 0f, _rotationStep * 90f);

            // 배치 가능 여부에 따라 색상 변경
            bool canPlace = _shipGrid.CanPlace(_pendingNode, gridPos, _rotationStep);
            SetPreviewColor(canPlace ? _validColor : _invalidColor);
        }

        /// <summary>
        /// 현재 마우스 위치에 노드 배치를 시도합니다.
        /// </summary>
        private void TryPlaceNode()
        {
            Vector3 mouseWorld = GetMouseWorldPosition();
            Vector2Int gridPos = _shipGrid.WorldToGrid(mouseWorld);

            if (!_shipGrid.CanPlace(_pendingNode, gridPos, _rotationStep))
                return;

            // PlacedNode 인스턴스 생성
            var placedNode = new PlacedNode(_pendingNode, gridPos, _rotationStep);

            // 씬에 노드 비주얼 생성
            GameObject worldObj = SpawnNodeVisual(placedNode);
            placedNode.WorldInstance = worldObj;

            // 그리드에 등록
            _shipGrid.PlaceNode(placedNode);

            // 배치 후 미리보기 제거 및 배치 모드 종료
            CancelPlacement();
        }

        /// <summary>
        /// 미리보기를 90도 회전시킵니다.
        /// </summary>
        private void RotatePreview()
        {
            _rotationStep = (_rotationStep + 1) % 4;
        }

        /// <summary>
        /// PlacedNode에 맞는 씬 오브젝트를 생성하고 배치합니다.
        /// </summary>
        private GameObject SpawnNodeVisual(PlacedNode node)
        {
            Vector3 worldPos = _shipGrid.NodeCenterToWorld(node);
            Quaternion rotation = _shipGrid.transform.rotation
                * Quaternion.Euler(0f, 0f, node.RotationStep * 90f);

            GameObject obj;

            if (node.Data.VisualPrefab != null)
            {
                obj = Instantiate(node.Data.VisualPrefab, worldPos, rotation, _shipGrid.transform);
            }
            else
            {
                // 프리팹 없으면 기본 큐브 생성
                obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.transform.SetParent(_shipGrid.transform);
                obj.transform.position = worldPos;
                obj.transform.rotation = rotation;

                float cellSize = _shipGrid.CellSize;
                Vector2Int size = node.GetRotatedSize();
                obj.transform.localScale = new Vector3(
                    size.x * cellSize * 0.9f,
                    size.y * cellSize * 0.9f,
                    0.1f
                );

                // 노드 색상 적용
                var renderer = obj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    renderer.material.color = node.Data.TintColor;
                }
            }

            obj.name = $"Node_{node.Data.NodeName}_{node.GridPosition}";
            return obj;
        }

        /// <summary>
        /// 미리보기의 모든 렌더러 색상을 변경합니다.
        /// </summary>
        private void SetPreviewColor(Color color)
        {
            if (_previewRenderers == null) return;

            foreach (var r in _previewRenderers)
            {
                if (r.material != null)
                    r.material.color = color;
            }
        }

        /// <summary>
        /// 현재 마우스 위치를 월드 좌표로 변환합니다.
        /// 탑다운 2D이므로 Z는 0으로 처리합니다.
        /// </summary>
        private Vector3 GetMouseWorldPosition()
        {
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector3 pos = new Vector3(mouseScreenPos.x, mouseScreenPos.y, Mathf.Abs(_mainCamera.transform.position.z));
            return _mainCamera.ScreenToWorldPoint(pos);
        }
    }
}
