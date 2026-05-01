using System.Collections.Generic;
using UnityEngine;

namespace POC6
{
    /// <summary>
    /// 플레이어가 드래그로 만든 동력 연결 그래프를 관리합니다.
    /// 물리 인접 그래프(NodeGraph)와 완전히 별개입니다.
    /// 허용 연결: 코어->공격, 코어->특수, 특수->공격
    /// </summary>
    public class PowerGraph : MonoBehaviour
    {
        // 출발 노드 -> 도착 노드 목록 (단방향)
        private Dictionary<PlacedNode, List<PlacedNode>> _outgoing = new();

        // 도착 노드 -> 출발 노드 목록 (역방향 조회용)
        private Dictionary<PlacedNode, List<PlacedNode>> _incoming = new();

        // 연결선 시각화 오브젝트 목록 (LineRenderer)
        private List<GameObject> _connectionLines = new();

        [Header("연결선 비주얼")]
        [Tooltip("동력 연결선에 사용할 머터리얼 (LineRenderer)")]
        [SerializeField] private Material _lineMaterial;

        [Tooltip("연결선 두께")]
        [SerializeField] private float _lineWidth = 0.05f;

        [Tooltip("정상 연결선 색상")]
        [SerializeField] private Color _lineColor = new Color(1f, 0.8f, 0f, 0.8f);

        [Tooltip("연결선 Sorting Layer 이름. 노드보다 앞에 표시하려면 노드와 같은 레이어에서 Order를 높게 설정합니다.")]
        [SerializeField] private string _sortingLayerName = "Default";

        [Tooltip("연결선 Sorting Order. 노드 오브젝트보다 높은 값으로 설정하면 앞에 표시됩니다.")]
        [SerializeField] private int _sortingOrder = 1;

        [Tooltip("라인의 Z 오프셋. 노드(Z=0)보다 카메라 쪽으로 당겨서 앞에 표시합니다.")]
        [SerializeField] private float _lineZOffset = -0.1f;

        private ShipGrid _grid;

        private void Awake()
        {
            _grid = GetComponent<ShipGrid>();
        }

        // ────────────────────────────────────────────────
        // 연결 추가 / 제거
        // ────────────────────────────────────────────────

        /// <summary>
        /// 두 노드 사이에 동력 연결을 추가합니다.
        /// 연결 규칙 위반 시 실패하고 false를 반환합니다.
        /// 성공 시 동력 분배를 재계산하고 시각적 연결선을 업데이트합니다.
        /// </summary>
        public bool TryAddConnection(PlacedNode from, PlacedNode to)
        {
            if (!IsValidConnection(from, to))
                return false;

            // 이미 연결되어 있으면 중복 추가 안 함
            if (IsConnected(from, to))
                return false;

            EnsureNodeExists(from);
            EnsureNodeExists(to);

            _outgoing[from].Add(to);
            _incoming[to].Add(from);

            RecalculatePowerDistribution();
            RefreshConnectionLines();

            return true;
        }

        /// <summary>
        /// 두 노드 사이의 동력 연결을 제거합니다.
        /// </summary>
        public void RemoveConnection(PlacedNode from, PlacedNode to)
        {
            if (_outgoing.ContainsKey(from))
                _outgoing[from].Remove(to);

            if (_incoming.ContainsKey(to))
                _incoming[to].Remove(from);

            RecalculatePowerDistribution();
            RefreshConnectionLines();
        }

        /// <summary>
        /// 특정 노드와 관련된 모든 연결을 제거합니다.
        /// 노드가 그리드에서 제거될 때 ShipGrid에서 호출합니다.
        /// </summary>
        public void OnNodeRemoved(PlacedNode node)
        {
            // 이 노드에서 나가는 연결 제거
            if (_outgoing.ContainsKey(node))
            {
                foreach (var to in new List<PlacedNode>(_outgoing[node]))
                {
                    if (_incoming.ContainsKey(to))
                        _incoming[to].Remove(node);
                }
                _outgoing.Remove(node);
            }

            // 이 노드로 들어오는 연결 제거
            if (_incoming.ContainsKey(node))
            {
                foreach (var from in new List<PlacedNode>(_incoming[node]))
                {
                    if (_outgoing.ContainsKey(from))
                        _outgoing[from].Remove(node);
                }
                _incoming.Remove(node);
            }

            RecalculatePowerDistribution();
            RefreshConnectionLines();
        }

        /// <summary>
        /// 그래프 전체를 초기화합니다.
        /// </summary>
        public void Clear()
        {
            _outgoing.Clear();
            _incoming.Clear();
            ClearConnectionLines();
        }

        // ────────────────────────────────────────────────
        // 동력 분배 계산
        // ────────────────────────────────────────────────

        // 공격 노드 -> 최종 계산된 스탯 캐시
        private Dictionary<PlacedNode, AttackStats> _effectiveStatsCache = new();

        /// <summary>
        /// 전체 동력 연결 그래프를 BFS로 탐색해서 각 공격 노드가 받는
        /// 실제 동력량과 특수 효과를 계산합니다.
        /// </summary>
        public void RecalculatePowerDistribution()
        {
            _effectiveStatsCache.Clear();

            // 모든 코어 노드를 시작점으로 BFS 시작
            foreach (var node in _grid.PlacedNodes)
            {
                if (node.Data.NodeType == NodeType.Core)
                    ProcessCoreNode(node);
            }
        }

        /// <summary>
        /// 단일 코어에서 연결된 공격 노드들까지 BFS로 탐색합니다.
        /// 코어의 동력을 연결된 공격 노드들에 균등 분배합니다.
        /// </summary>
        private void ProcessCoreNode(PlacedNode coreNode)
        {
            int powerCapacity = coreNode.Data.PowerCapacity;

            // 이 코어와 연결된 공격 노드와 경로상 특수 노드를 수집
            var attackNodePaths = new List<(PlacedNode attackNode, PlacedNode specialNode)>();

            if (!_outgoing.ContainsKey(coreNode)) return;

            foreach (var directTarget in _outgoing[coreNode])
            {
                if (directTarget.Data.NodeType == NodeType.Attack)
                {
                    // 코어 -> 공격 직접 연결
                    attackNodePaths.Add((directTarget, null));
                }
                else if (directTarget.Data.NodeType == NodeType.Special)
                {
                    // 코어 -> 특수 -> 공격 체인
                    if (_outgoing.ContainsKey(directTarget))
                    {
                        foreach (var chainTarget in _outgoing[directTarget])
                        {
                            if (chainTarget.Data.NodeType == NodeType.Attack)
                            {
                                attackNodePaths.Add((chainTarget, directTarget));
                            }
                        }
                    }
                }
            }

            if (attackNodePaths.Count == 0) return;

            // 균등 분배: 코어 동력 / 연결된 공격 노드 수
            float powerPerAttack = (float)powerCapacity / attackNodePaths.Count;
            // 최대 동력 비율 계산을 위한 최대값 (코어 전체 용량 = 100%)
            float powerRatio = powerPerAttack / powerCapacity;

            foreach (var (attackNode, specialNode) in attackNodePaths)
            {
                // 특수 효과 정보
                SpecialEffectType? effectType = null;
                float effectMagnitude = 0f;

                if (specialNode != null)
                {
                    effectType = specialNode.Data.SpecialEffect;
                    effectMagnitude = specialNode.Data.EffectMagnitude;
                }

                // 기존 스탯이 있으면 누적 (여러 코어의 동력이 한 공격 노드에 모이는 경우)
                AttackStats effective = attackNode.Data.BaseAttackStats
                    .WithPowerAndEffects(powerRatio, effectType, effectMagnitude);

                if (_effectiveStatsCache.ContainsKey(attackNode))
                {
                    // 이미 다른 코어에서 계산된 스탯이 있으면 데미지와 공속 합산
                    var existing = _effectiveStatsCache[attackNode];
                    // 단순화: 더 높은 값 채택 (POC 기준)
                    effective = new AttackStats(
                        existing.Damage + effective.Damage,
                        Mathf.Max(existing.FireRate, effective.FireRate),
                        Mathf.Max(existing.AttackRange, effective.AttackRange),
                        existing.ProjectileSpeed,
                        Mathf.Max(existing.ProjectileCount, effective.ProjectileCount),
                        Mathf.Max(existing.PierceCount, effective.PierceCount)
                    );
                }

                _effectiveStatsCache[attackNode] = effective;
            }
        }

        /// <summary>
        /// 공격 노드의 최종 전투 스탯을 반환합니다.
        /// RecalculatePowerDistribution() 이후 호출해야 최신값을 얻습니다.
        /// 동력 연결이 없으면 베이스 스탯에 동력 0으로 적용한 값을 반환합니다.
        /// </summary>
        public AttackStats GetEffectiveStats(PlacedNode attackNode)
        {
            if (_effectiveStatsCache.TryGetValue(attackNode, out var stats))
                return stats;

            // 연결 없음 = 동력 없음 = 스탯 0
            return attackNode.Data.BaseAttackStats.WithPowerAndEffects(0f, null, 0f);
        }

        // ────────────────────────────────────────────────
        // 연결 유효성 검사
        // ────────────────────────────────────────────────

        /// <summary>
        /// 연결 규칙에 따라 두 노드 간 연결이 유효한지 확인합니다.
        /// 허용: 코어->공격, 코어->특수, 특수->공격
        /// </summary>
        public bool IsValidConnection(PlacedNode from, PlacedNode to)
        {
            if (from == null || to == null) return false;
            if (from == to) return false;

            var fromType = from.Data.NodeType;
            var toType = to.Data.NodeType;

            return (fromType == NodeType.Core && toType == NodeType.Attack) ||
                   (fromType == NodeType.Core && toType == NodeType.Special) ||
                   (fromType == NodeType.Special && toType == NodeType.Attack);
        }

        /// <summary>
        /// 두 노드 사이에 이미 연결이 있는지 확인합니다.
        /// </summary>
        public bool IsConnected(PlacedNode from, PlacedNode to)
        {
            return _outgoing.ContainsKey(from) && _outgoing[from].Contains(to);
        }

        // ────────────────────────────────────────────────
        // 스냅샷 직렬화
        // ────────────────────────────────────────────────

        /// <summary>
        /// 현재 동력 연결 상태를 직렬화 가능한 데이터 목록으로 변환합니다.
        /// </summary>
        public List<PowerConnectionData> SerializeConnections()
        {
            var result = new List<PowerConnectionData>();

            foreach (var kvp in _outgoing)
            {
                foreach (var to in kvp.Value)
                {
                    result.Add(new PowerConnectionData
                    {
                        fromGridX = kvp.Key.GridPosition.x,
                        fromGridY = kvp.Key.GridPosition.y,
                        toGridX = to.GridPosition.x,
                        toGridY = to.GridPosition.y
                    });
                }
            }

            return result;
        }

        // ────────────────────────────────────────────────
        // 연결선 시각화
        // ────────────────────────────────────────────────

        /// <summary>
        /// 모든 연결선 게임오브젝트를 삭제하고 현재 연결 상태 기반으로 다시 생성합니다.
        /// </summary>
        private void RefreshConnectionLines()
        {
            ClearConnectionLines();

            foreach (var kvp in _outgoing)
            {
                foreach (var to in kvp.Value)
                {
                    CreateConnectionLine(kvp.Key, to);
                }
            }
        }

        /// <summary>
        /// 두 노드 사이에 LineRenderer로 연결선을 생성합니다.
        /// </summary>
        private void CreateConnectionLine(PlacedNode from, PlacedNode to)
        {
            var lineObj = new GameObject($"PowerLine_{from.GridPosition}->{to.GridPosition}");
            lineObj.transform.SetParent(transform);

            var lr = lineObj.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = _lineWidth;
            lr.endWidth = _lineWidth;

            if (_lineMaterial != null)
                lr.material = _lineMaterial;

            lr.startColor = _lineColor;
            lr.endColor = _lineColor;
            lr.useWorldSpace = true;
            lr.sortingLayerName = _sortingLayerName;
            lr.sortingOrder = _sortingOrder;

            Vector3 fromPos = _grid.NodeCenterToWorld(from);
            Vector3 toPos = _grid.NodeCenterToWorld(to);
            fromPos.z += _lineZOffset;
            toPos.z += _lineZOffset;
            lr.SetPosition(0, fromPos);
            lr.SetPosition(1, toPos);

            _connectionLines.Add(lineObj);
        }

        /// <summary>
        /// 모든 연결선 게임오브젝트를 제거합니다.
        /// </summary>
        private void ClearConnectionLines()
        {
            foreach (var line in _connectionLines)
            {
                if (line != null) Destroy(line);
            }
            _connectionLines.Clear();
        }

        // ────────────────────────────────────────────────
        // 헬퍼
        // ────────────────────────────────────────────────

        private void EnsureNodeExists(PlacedNode node)
        {
            if (!_outgoing.ContainsKey(node)) _outgoing[node] = new List<PlacedNode>();
            if (!_incoming.ContainsKey(node)) _incoming[node] = new List<PlacedNode>();
        }
    }
}
