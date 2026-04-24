using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense
{
    /// <summary>
    /// A* 알고리즘으로 그리드 위의 최단 경로를 계산하는 클래스.
    /// 상하좌우 4방향 이동만 허용하며 벽 셀은 통과하지 않는다.
    /// 성능을 위해 매 프레임이 아닌 벽 설치 시에만 호출해야 한다.
    /// </summary>
    public class PathFinder : MonoBehaviour
    {
        // -------------------------------------------------------
        // 내부 클래스: A* 노드
        // -------------------------------------------------------

        /// <summary>
        /// A* 알고리즘에서 각 셀을 나타내는 노드.
        /// G: 시작점에서 이 노드까지의 실제 이동 비용
        /// H: 이 노드에서 목표점까지의 예상 비용 (휴리스틱)
        /// F = G + H
        /// </summary>
        private class Node
        {
            public Vector2Int Position;
            public Node Parent;
            public int G; // 시작점에서 현재 노드까지의 비용
            public int H; // 현재 노드에서 목표까지의 예상 비용
            public int F => G + H;

            public Node(Vector2Int position, Node parent, int g, int h)
            {
                Position = position;
                Parent = parent;
                G = g;
                H = h;
            }
        }

        // -------------------------------------------------------
        // 이동 방향 정의 (4방향: 상, 하, 좌, 우)
        // -------------------------------------------------------

        private static readonly Vector2Int[] Directions = new Vector2Int[]
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        // -------------------------------------------------------
        // 내부 참조
        // -------------------------------------------------------

        private GridSystem _gridSystem;

        // -------------------------------------------------------
        // 유니티 생명주기
        // -------------------------------------------------------

        private void Awake()
        {
            _gridSystem = GetComponent<GridSystem>();

            if (_gridSystem == null)
            {
                Debug.LogError("[PathFinder] GridSystem 컴포넌트를 찾을 수 없습니다. 같은 GameObject에 추가해주세요.");
            }
        }

        // -------------------------------------------------------
        // 경로 탐색
        // -------------------------------------------------------

        /// <summary>
        /// 시작점에서 목표점까지 A*로 최단 경로를 계산한다.
        /// 반환값: 경로 좌표 리스트 (시작점 포함, 목표점 포함).
        /// 경로가 없으면 null 반환.
        /// </summary>
        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
        {
            // 오픈 리스트: 아직 탐색하지 않은 노드 (F 비용 기준 정렬)
            List<Node> openList = new List<Node>();

            // 클로즈드 셋: 이미 탐색 완료된 위치들
            HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();

            Node startNode = new Node(start, null, 0, CalculateHeuristic(start, goal));
            openList.Add(startNode);

            while (openList.Count > 0)
            {
                // F 비용이 가장 낮은 노드 선택
                Node current = GetLowestFNode(openList);

                // 목표 도달
                if (current.Position == goal)
                {
                    return BuildPath(current);
                }

                openList.Remove(current);
                closedSet.Add(current.Position);

                // 4방향 이웃 탐색
                foreach (Vector2Int direction in Directions)
                {
                    Vector2Int neighborPos = current.Position + direction;

                    // 범위 밖이거나 벽이거나 이미 탐색한 셀이면 건너뜀
                    if (!_gridSystem.IsInBounds(neighborPos)) continue;
                    if (_gridSystem.IsWall(neighborPos)) continue;
                    if (closedSet.Contains(neighborPos)) continue;

                    int newG = current.G + 1; // 4방향 이동 비용은 모두 1
                    int newH = CalculateHeuristic(neighborPos, goal);
                    Node neighborNode = new Node(neighborPos, current, newG, newH);

                    // 같은 위치가 오픈 리스트에 있고 더 낮은 G 비용을 갖는다면 업데이트
                    Node existingNode = FindNodeInOpenList(openList, neighborPos);
                    if (existingNode != null)
                    {
                        if (newG < existingNode.G)
                        {
                            existingNode.G = newG;
                            existingNode.Parent = current;
                        }
                    }
                    else
                    {
                        openList.Add(neighborNode);
                    }
                }
            }

            // 경로 없음
            return null;
        }

        /// <summary>
        /// GridSystem의 기본 SpawnPoint -> GoalPoint 경로를 계산하는 편의 메서드.
        /// </summary>
        public List<Vector2Int> FindDefaultPath()
        {
            return FindPath(_gridSystem.SpawnPoint, _gridSystem.GoalPoint);
        }

        // -------------------------------------------------------
        // 경로 차단 여부 확인
        // -------------------------------------------------------

        /// <summary>
        /// 현재 그리드 상태에서 시작점에서 목표점까지의 경로가 존재하는지 확인한다.
        /// 벽 설치 전 경로 검증에 사용.
        /// </summary>
        public bool HasPath(Vector2Int start, Vector2Int goal)
        {
            return FindPath(start, goal) != null;
        }

        // -------------------------------------------------------
        // 내부 헬퍼 메서드
        // -------------------------------------------------------

        /// <summary>
        /// 맨해튼 거리를 휴리스틱으로 사용한다.
        /// 4방향 이동만 허용하므로 맨해튼 거리가 가장 적합.
        /// </summary>
        private int CalculateHeuristic(Vector2Int from, Vector2Int to)
        {
            return Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y);
        }

        /// <summary>
        /// 오픈 리스트에서 F 값이 가장 낮은 노드를 반환한다.
        /// F 값이 같으면 H 값이 낮은 것 (목표에 더 가까운 것)을 선택.
        /// </summary>
        private Node GetLowestFNode(List<Node> openList)
        {
            Node lowest = openList[0];
            foreach (Node node in openList)
            {
                if (node.F < lowest.F || (node.F == lowest.F && node.H < lowest.H))
                {
                    lowest = node;
                }
            }
            return lowest;
        }

        /// <summary>
        /// 오픈 리스트에서 특정 위치를 가진 노드를 찾아 반환한다.
        /// 없으면 null 반환.
        /// </summary>
        private Node FindNodeInOpenList(List<Node> openList, Vector2Int position)
        {
            foreach (Node node in openList)
            {
                if (node.Position == position) return node;
            }
            return null;
        }

        /// <summary>
        /// 목표 노드에서 부모를 따라 역추적하여 시작점부터 목표점까지의 경로 리스트를 만든다.
        /// </summary>
        private List<Vector2Int> BuildPath(Node goalNode)
        {
            List<Vector2Int> path = new List<Vector2Int>();
            Node current = goalNode;

            while (current != null)
            {
                path.Add(current.Position);
                current = current.Parent;
            }

            // 역추적했으므로 뒤집어서 시작점 -> 목표점 순서로 반환
            path.Reverse();
            return path;
        }
    }
}
