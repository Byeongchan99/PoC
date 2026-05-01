using System.Collections.Generic;
using UnityEngine;

namespace POC6
{
    /// <summary>
    /// 우주선의 그리드 데이터를 관리합니다.
    /// 어느 셀에 어떤 노드가 배치되어 있는지를 추적하고,
    /// 노드 배치 가능 여부를 검증하며, 그리드 좌표와 월드 좌표 변환을 담당합니다.
    /// </summary>
    public class ShipGrid : MonoBehaviour
    {
        [Header("그리드 설정")]
        [Tooltip("그리드 가로 셀 수")]
        [SerializeField] private int _width = 9;

        [Tooltip("그리드 세로 셀 수")]
        [SerializeField] private int _height = 9;

        [Tooltip("셀 하나의 월드 크기 (유닛)")]
        [SerializeField] private float _cellSize = 1f;

        // 셀 좌표 -> 배치된 노드 매핑 (한 셀에 여러 노드가 겹칠 수 없음)
        private Dictionary<Vector2Int, PlacedNode> _cellMap = new();

        // 배치된 모든 노드 목록 (체력 합산 등 전체 순회에 사용)
        private List<PlacedNode> _placedNodes = new();

        // NodeGraph와 PowerGraph는 배치/제거 이벤트를 구독해서 자동으로 갱신됩니다
        private NodeGraph _nodeGraph;
        private PowerGraph _powerGraph;

        /// <summary>배치된 모든 노드의 읽기 전용 목록</summary>
        public IReadOnlyList<PlacedNode> PlacedNodes => _placedNodes;

        /// <summary>그리드 가로 크기</summary>
        public int Width => _width;

        /// <summary>그리드 세로 크기</summary>
        public int Height => _height;

        /// <summary>셀 크기 (월드 유닛)</summary>
        public float CellSize => _cellSize;

        private void Awake()
        {
            _nodeGraph = GetComponent<NodeGraph>();
            _powerGraph = GetComponent<PowerGraph>();
        }

        // ────────────────────────────────────────────────
        // 외부에서 GameConfig 값을 주입할 때 사용
        // ────────────────────────────────────────────────

        /// <summary>
        /// GameConfig ScriptableObject의 값으로 그리드 크기와 셀 크기를 초기화합니다.
        /// </summary>
        public void Initialize(GameConfig config)
        {
            _width = config.GridWidth;
            _height = config.GridHeight;
            _cellSize = config.CellSize;
        }

        // ────────────────────────────────────────────────
        // 노드 배치 / 제거
        // ────────────────────────────────────────────────

        /// <summary>
        /// 노드를 그리드에 배치합니다.
        /// 배치 가능 여부는 사전에 CanPlace()로 확인한 뒤 호출해야 합니다.
        /// </summary>
        public void PlaceNode(PlacedNode node)
        {
            foreach (var cell in node.GetOccupiedCells())
            {
                _cellMap[cell] = node;
            }

            _placedNodes.Add(node);

            // 인접 그래프와 동력 그래프에 새 노드 등록
            _nodeGraph?.OnNodePlaced(node);
        }

        /// <summary>
        /// 그리드에서 노드를 제거합니다.
        /// 씬에서 오브젝트를 파괴하는 것은 호출하는 쪽에서 담당합니다.
        /// </summary>
        public void RemoveNode(PlacedNode node)
        {
            foreach (var cell in node.GetOccupiedCells())
            {
                _cellMap.Remove(cell);
            }

            _placedNodes.Remove(node);

            // 그래프들에서도 제거
            _nodeGraph?.OnNodeRemoved(node);
            _powerGraph?.OnNodeRemoved(node);
        }

        /// <summary>
        /// 모든 노드를 제거하고 그리드를 초기화합니다.
        /// </summary>
        public void Clear()
        {
            // 씬 오브젝트 파괴
            foreach (var node in _placedNodes)
            {
                if (node.WorldInstance != null)
                    Destroy(node.WorldInstance);
            }

            _cellMap.Clear();
            _placedNodes.Clear();
            _nodeGraph?.Clear();
            _powerGraph?.Clear();
        }

        // ────────────────────────────────────────────────
        // 배치 유효성 검증
        // ────────────────────────────────────────────────

        /// <summary>
        /// 주어진 위치와 회전으로 노드를 배치할 수 있는지 검사합니다.
        /// 그리드 범위 초과, 다른 노드와의 겹침, 인접 노드 없음 세 가지를 확인합니다.
        /// </summary>
        public bool CanPlace(NodeData data, Vector2Int gridPos, int rotationStep)
        {
            var tempNode = new PlacedNode(data, gridPos, rotationStep);
            var cells = tempNode.GetOccupiedCells();

            foreach (var cell in cells)
            {
                // 그리드 범위 초과 검사
                if (!IsInBounds(cell))
                    return false;

                // 이미 점유된 셀 검사
                if (_cellMap.ContainsKey(cell))
                    return false;
            }

            // 첫 번째 노드(코어)는 인접 조건 없이 배치 가능
            if (_placedNodes.Count == 0)
                return true;

            // 이미 배치된 다른 노드와 한 면 이상 인접해야 배치 가능
            return HasAdjacentNode(cells);
        }

        /// <summary>
        /// 셀 목록 중 하나라도 기존 배치 노드와 인접한 면이 있는지 확인합니다.
        /// </summary>
        private bool HasAdjacentNode(List<Vector2Int> cells)
        {
            // 4방향 오프셋
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            foreach (var cell in cells)
            {
                foreach (var dir in directions)
                {
                    if (_cellMap.ContainsKey(cell + dir))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 해당 셀 좌표가 그리드 범위 안에 있는지 확인합니다.
        /// </summary>
        public bool IsInBounds(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < _width && cell.y >= 0 && cell.y < _height;
        }

        // ────────────────────────────────────────────────
        // 조회
        // ────────────────────────────────────────────────

        /// <summary>
        /// 해당 셀 좌표에 배치된 노드를 반환합니다. 없으면 null을 반환합니다.
        /// </summary>
        public PlacedNode GetNodeAt(Vector2Int cell)
        {
            _cellMap.TryGetValue(cell, out var node);
            return node;
        }

        /// <summary>
        /// 모든 배치 노드의 체력 기여도를 합산하여 반환합니다.
        /// HealthSystem이 최대 체력을 계산할 때 사용합니다.
        /// </summary>
        public int CalculateTotalHealth()
        {
            int total = 0;
            foreach (var node in _placedNodes)
                total += node.Data.HealthContribution;
            return total;
        }

        // ────────────────────────────────────────────────
        // 좌표 변환
        // ────────────────────────────────────────────────

        /// <summary>
        /// 그리드 셀 좌표를 우주선 로컬 좌표로 변환합니다.
        /// 그리드 (0,0)이 우주선 중앙이 되도록 오프셋을 적용합니다.
        /// </summary>
        public Vector2 GridToLocal(Vector2Int cell)
        {
            // 그리드 중앙을 (0,0)으로 맞추는 오프셋
            float offsetX = (_width - 1) * _cellSize * 0.5f;
            float offsetY = (_height - 1) * _cellSize * 0.5f;

            return new Vector2(
                cell.x * _cellSize - offsetX,
                cell.y * _cellSize - offsetY
            );
        }

        /// <summary>
        /// 그리드 셀 좌표를 월드 좌표로 변환합니다.
        /// 우주선의 Transform(회전 + 위치)을 적용합니다.
        /// 적 AI가 가장 가까운 노드를 찾거나, 발사 위치를 계산할 때 사용합니다.
        /// </summary>
        public Vector3 GridToWorld(Vector2Int cell)
        {
            Vector2 local = GridToLocal(cell);
            return transform.TransformPoint(new Vector3(local.x, local.y, 0f));
        }

        /// <summary>
        /// 노드 중심의 그리드 셀 좌표를 월드 좌표로 변환합니다.
        /// 가변 크기 노드는 차지하는 셀들의 중앙을 계산합니다.
        /// </summary>
        public Vector3 NodeCenterToWorld(PlacedNode node)
        {
            Vector2Int size = node.GetRotatedSize();
            // 중앙 셀 좌표 (소수점 가능)
            float centerX = node.GridPosition.x + (size.x - 1) * 0.5f;
            float centerY = node.GridPosition.y + (size.y - 1) * 0.5f;

            float offsetX = (_width - 1) * _cellSize * 0.5f;
            float offsetY = (_height - 1) * _cellSize * 0.5f;

            Vector2 local = new Vector2(
                centerX * _cellSize - offsetX,
                centerY * _cellSize - offsetY
            );

            return transform.TransformPoint(new Vector3(local.x, local.y, 0f));
        }

        /// <summary>
        /// 월드 좌표를 그리드 셀 좌표로 변환합니다.
        /// 마우스 클릭 위치에서 어느 셀에 배치할지 계산할 때 사용합니다.
        /// </summary>
        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            // 우주선 Transform의 역변환으로 로컬 좌표로 변환
            Vector3 local = transform.InverseTransformPoint(worldPos);

            float offsetX = (_width - 1) * _cellSize * 0.5f;
            float offsetY = (_height - 1) * _cellSize * 0.5f;

            int gridX = Mathf.RoundToInt((local.x + offsetX) / _cellSize);
            int gridY = Mathf.RoundToInt((local.y + offsetY) / _cellSize);

            return new Vector2Int(gridX, gridY);
        }

        // ────────────────────────────────────────────────
        // 스냅샷 관련
        // ────────────────────────────────────────────────

        /// <summary>
        /// 현재 배치 상태를 직렬화 가능한 데이터 목록으로 변환해서 반환합니다.
        /// WaveSnapshot 저장 시 사용합니다.
        /// </summary>
        public List<PlacedNodeData> SerializeNodes()
        {
            var result = new List<PlacedNodeData>();

            foreach (var node in _placedNodes)
            {
                result.Add(new PlacedNodeData
                {
                    nodeDataName = node.Data.name,
                    gridX = node.GridPosition.x,
                    gridY = node.GridPosition.y,
                    rotationStep = node.RotationStep,
                    upgradeLevel = node.CurrentUpgradeLevel
                });
            }

            return result;
        }

        // ────────────────────────────────────────────────
        // Gizmos (에디터 시각화)
        // ────────────────────────────────────────────────

#if UNITY_EDITOR
        /// <summary>
        /// 에디터에서 그리드 범위와 점유된 셀을 시각화합니다.
        /// </summary>
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 0.8f, 0.3f, 0.3f);

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    Vector3 worldPos = GridToWorld(new Vector2Int(x, y));
                    var cell = new Vector2Int(x, y);

                    // 점유된 셀은 다른 색상으로 표시
                    if (_cellMap.ContainsKey(cell))
                        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
                    else
                        Gizmos.color = new Color(0.3f, 0.8f, 0.3f, 0.2f);

                    Gizmos.DrawWireCube(worldPos, Vector3.one * (_cellSize * 0.95f));
                }
            }
        }
#endif
    }
}
