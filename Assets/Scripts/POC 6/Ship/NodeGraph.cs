using System.Collections.Generic;
using UnityEngine;

namespace POC6
{
    /// <summary>
    /// 우주선 노드들 간의 물리적 인접 관계를 관리합니다.
    /// 그리드에 노드가 배치될 때마다 인접 관계를 자동으로 계산합니다.
    /// 이 그래프는 동력 연결(PowerGraph)과 완전히 별개입니다.
    /// 주 역할: 디버그 시각화, 인접 여부 확인, 체력 합산 등.
    /// </summary>
    public class NodeGraph : MonoBehaviour
    {
        // 노드 -> 인접한 노드 목록 매핑
        private Dictionary<PlacedNode, List<PlacedNode>> _adjacency = new();

        // ShipGrid 참조 (인접 셀 계산에 필요)
        private ShipGrid _grid;

        private void Awake()
        {
            _grid = GetComponent<ShipGrid>();
        }

        /// <summary>
        /// 노드가 그리드에 새로 배치될 때 호출됩니다.
        /// 새 노드와 기존 노드들의 인접 관계를 계산해서 그래프에 추가합니다.
        /// </summary>
        public void OnNodePlaced(PlacedNode newNode)
        {
            if (!_adjacency.ContainsKey(newNode))
                _adjacency[newNode] = new List<PlacedNode>();

            // 기존 모든 노드를 순회하며 새 노드와 인접한지 확인
            foreach (var existingNode in _grid.PlacedNodes)
            {
                if (existingNode == newNode) continue;

                if (IsAdjacent(newNode, existingNode))
                {
                    // 양방향으로 인접 관계 추가
                    _adjacency[newNode].Add(existingNode);

                    if (!_adjacency.ContainsKey(existingNode))
                        _adjacency[existingNode] = new List<PlacedNode>();

                    _adjacency[existingNode].Add(newNode);
                }
            }
        }

        /// <summary>
        /// 노드가 그리드에서 제거될 때 호출됩니다.
        /// 이 노드와 연결된 모든 인접 관계를 제거합니다.
        /// </summary>
        public void OnNodeRemoved(PlacedNode node)
        {
            if (!_adjacency.ContainsKey(node)) return;

            // 이 노드를 참조하는 다른 노드들의 인접 목록에서 제거
            foreach (var neighbor in _adjacency[node])
            {
                if (_adjacency.ContainsKey(neighbor))
                    _adjacency[neighbor].Remove(node);
            }

            _adjacency.Remove(node);
        }

        /// <summary>
        /// 그래프 전체를 초기화합니다.
        /// </summary>
        public void Clear()
        {
            _adjacency.Clear();
        }

        /// <summary>
        /// 두 노드가 물리적으로 인접한지(한 면 이상 맞닿음) 확인합니다.
        /// 가변 크기 노드를 지원하기 위해 셀 단위로 검사합니다.
        /// </summary>
        public bool IsAdjacent(PlacedNode a, PlacedNode b)
        {
            var cellsA = a.GetOccupiedCells();
            var cellsB = b.GetOccupiedCells();

            // 셀 A와 셀 B의 맨해튼 거리가 1이면 인접 (대각선은 인접 아님)
            foreach (var cellA in cellsA)
            {
                foreach (var cellB in cellsB)
                {
                    int dx = Mathf.Abs(cellA.x - cellB.x);
                    int dy = Mathf.Abs(cellA.y - cellB.y);
                    if (dx + dy == 1)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 특정 노드와 인접한 노드 목록을 반환합니다.
        /// </summary>
        public IReadOnlyList<PlacedNode> GetNeighbors(PlacedNode node)
        {
            if (_adjacency.TryGetValue(node, out var neighbors))
                return neighbors;

            return new List<PlacedNode>();
        }

#if UNITY_EDITOR
        /// <summary>
        /// 에디터에서 인접 관계를 선으로 시각화합니다.
        /// </summary>
        private void OnDrawGizmos()
        {
            if (_grid == null) return;

            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.5f);

            foreach (var kvp in _adjacency)
            {
                Vector3 fromPos = _grid.NodeCenterToWorld(kvp.Key);

                foreach (var neighbor in kvp.Value)
                {
                    Vector3 toPos = _grid.NodeCenterToWorld(neighbor);
                    Gizmos.DrawLine(fromPos, toPos);
                }
            }
        }
#endif
    }
}
