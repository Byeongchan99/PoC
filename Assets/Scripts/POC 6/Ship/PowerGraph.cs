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

        [Header("설정")]
        [Tooltip("동력 비율 계산에 사용하는 기준 설정. 없으면 기준 동력 100을 사용합니다.")]
        [SerializeField] private GameConfig _config;

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

        // 공격 노드 -> 수신 동력량 캐시 (UI 표시용)
        private Dictionary<PlacedNode, float> _receivedPowerCache = new();
        private Dictionary<PlacedNode, float> _totalPowerCache = new();

        /// <summary>
        /// 전체 동력 연결 그래프를 순회해서 각 공격 노드의 스탯을 재계산합니다.
        /// 2단계로 처리합니다.
        /// 1단계: 모든 코어에서 각 공격 노드로 전달되는 동력과 특수 효과를 수집합니다.
        /// 2단계: 수집된 데이터를 토대로 업그레이드 레벨을 반영한 최종 스탯을 계산합니다.
        /// </summary>
        public void RecalculatePowerDistribution()
        {
            _effectiveStatsCache.Clear();
            _receivedPowerCache.Clear();
            _totalPowerCache.Clear();

            // 1단계 수집용 임시 딕셔너리
            // 공격 노드 -> 수신 총 동력량
            var rawPower = new Dictionary<PlacedNode, float>();
            // 공격 노드 -> 적용할 특수 효과 (처음 발견한 것 사용, POC 기준)
            var specialEffects = new Dictionary<PlacedNode, (SpecialEffectType effect, float magnitude)>();

            // 1단계: 모든 코어에서 동력 수집
            foreach (var node in _grid.PlacedNodes)
            {
                if (node.Data.NodeType == NodeType.Core)
                    CollectPowerFromCore(node, rawPower, specialEffects);
            }

            // 2단계: 수집된 동력으로 최종 스탯 계산
            float basePower = _config != null ? _config.BasePowerCapacity : 100f;
            float upgradeBonus = _config != null ? _config.UpgradeStatBonus : 0.2f;

            foreach (var kvp in rawPower)
            {
                PlacedNode attackNode = kvp.Key;
                float received = kvp.Value;

                _receivedPowerCache[attackNode] = received;
                // UI에서 received / basePower 비율(%)로 표시하기 위한 기준값 저장
                _totalPowerCache[attackNode] = basePower;

                // 동력 비율: 기준 동력 대비 수신량 (1.0 = 기준치, 1.5 = 50% 초과)
                float ratio = received / basePower;

                SpecialEffectType? effect = null;
                float effectMag = 0f;
                if (specialEffects.TryGetValue(attackNode, out var se))
                {
                    effect = se.effect;
                    effectMag = se.magnitude;
                }

                // 업그레이드 레벨이 반영된 기본 스탯으로 동력과 특수 효과 적용
                AttackStats baseStats = attackNode.GetUpgradedBaseStats(upgradeBonus);
                _effectiveStatsCache[attackNode] = baseStats.WithPowerAndEffects(ratio, effect, effectMag);
            }
        }

        /// <summary>
        /// 단일 코어에서 연결된 모든 공격 노드로의 동력과 특수 효과를 수집합니다.
        /// 코어의 동력을 연결된 공격 노드 수에 따라 균등 분배합니다.
        /// 코어 -> 공격 (직접) 또는 코어 -> 특수 -> 공격 (특수 경유) 경로를 처리합니다.
        /// </summary>
        private void CollectPowerFromCore(
            PlacedNode coreNode,
            Dictionary<PlacedNode, float> powerAcc,
            Dictionary<PlacedNode, (SpecialEffectType, float)> specialAcc)
        {
            if (!_outgoing.ContainsKey(coreNode)) return;

            var attackPaths = new List<(PlacedNode attackNode, PlacedNode specialNode)>();

            foreach (var target in _outgoing[coreNode])
            {
                if (target.Data.NodeType == NodeType.Attack)
                {
                    attackPaths.Add((target, null));
                }
                else if (target.Data.NodeType == NodeType.Special && _outgoing.ContainsKey(target))
                {
                    foreach (var chainTarget in _outgoing[target])
                    {
                        if (chainTarget.Data.NodeType == NodeType.Attack)
                            attackPaths.Add((chainTarget, target));
                    }
                }
            }

            if (attackPaths.Count == 0) return;

            // 이 코어의 동력을 연결된 공격 노드 수로 균등 분배
            float powerPerAttack = (float)coreNode.Data.PowerCapacity / attackPaths.Count;

            foreach (var (attackNode, specialNode) in attackPaths)
            {
                // 동력 누적: 여러 코어에서 받으면 합산
                powerAcc[attackNode] = powerAcc.TryGetValue(attackNode, out var prev)
                    ? prev + powerPerAttack
                    : powerPerAttack;

                // 특수 효과: 처음 발견한 것을 적용 (POC 단순화)
                if (specialNode != null && !specialAcc.ContainsKey(attackNode))
                    specialAcc[attackNode] = (specialNode.Data.SpecialEffect, specialNode.Data.EffectMagnitude);
            }
        }

        /// <summary>
        /// 이 노드가 받고 있는 동력량을 반환합니다. NodeInfoUI의 동력 표시에 사용합니다.
        /// </summary>
        public float GetReceivedPower(PlacedNode node) =>
            _receivedPowerCache.TryGetValue(node, out var p) ? p : 0f;

        /// <summary>
        /// 이 노드에 동력을 공급하는 코어의 총 동력량을 반환합니다.
        /// </summary>
        public float GetTotalPower(PlacedNode node) =>
            _totalPowerCache.TryGetValue(node, out var p) ? p : 0f;

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
        /// useWorldSpace = false로 설정해서 우주선(부모 Transform)을 따라 함께 이동합니다.
        /// </summary>
        private void CreateConnectionLine(PlacedNode from, PlacedNode to)
        {
            var lineObj = new GameObject($"PowerLine_{from.GridPosition}->{to.GridPosition}");
            lineObj.transform.SetParent(transform);
            lineObj.transform.localPosition = Vector3.zero;
            lineObj.transform.localRotation = Quaternion.identity;

            var lr = lineObj.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = _lineWidth;
            lr.endWidth = _lineWidth;

            if (_lineMaterial != null)
                lr.material = _lineMaterial;

            lr.startColor = _lineColor;
            lr.endColor = _lineColor;

            // useWorldSpace = false: 위치를 부모(우주선) 로컬 좌표로 지정.
            // 우주선이 이동/회전하면 라인도 함께 움직입니다.
            lr.useWorldSpace = false;
            lr.sortingLayerName = _sortingLayerName;
            lr.sortingOrder = _sortingOrder;

            // 월드 좌표를 우주선 로컬 좌표로 변환
            Vector3 fromWorld = _grid.NodeCenterToWorld(from);
            Vector3 toWorld = _grid.NodeCenterToWorld(to);

            Vector3 fromLocal = transform.InverseTransformPoint(fromWorld);
            Vector3 toLocal = transform.InverseTransformPoint(toWorld);
            fromLocal.z = _lineZOffset;
            toLocal.z = _lineZOffset;

            lr.SetPosition(0, fromLocal);
            lr.SetPosition(1, toLocal);

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
