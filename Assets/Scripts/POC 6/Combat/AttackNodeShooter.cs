using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace POC6
{
    /// <summary>
    /// [사용 중단] 각 Attack 노드의 AttackNodeBehaviour로 대체되었습니다.
    /// 노드가 자신의 Update()에서 직접 타겟 탐색과 발사를 처리합니다.
    /// </summary>
    [System.Obsolete("AttackNodeBehaviour로 대체됨. 이 컴포넌트는 씬에서 제거하세요.")]
    public class AttackNodeShooter : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private ShipGrid _shipGrid;
        [SerializeField] private PowerGraph _powerGraph;

        [Header("발사 설정")]
        [Tooltip("발사 가능한 각도 범위 (노드 전방 기준 좌우 각도). 90이면 전방 180도 전체.")]
        [Range(10f, 90f)]
        [SerializeField] private float _firingHalfAngle = 90f;

        // 노드별 마지막 발사 시간 추적 (공격속도 제어)
        private Dictionary<PlacedNode, float> _lastFireTimes = new();

        // 현재 씬의 모든 적 목록 (EnemySpawner가 유지)
        private List<Enemy> _enemies = new();

        private bool _isActive = false;

        // ────────────────────────────────────────────────
        // 공개 API
        // ────────────────────────────────────────────────

        /// <summary>
        /// 자동 발사 시스템을 활성화합니다. Combat Phase 진입 시 GameManager에서 호출합니다.
        /// </summary>
        public void Activate()
        {
            _isActive = true;
            _lastFireTimes.Clear();
        }

        /// <summary>
        /// 자동 발사 시스템을 비활성화합니다. Build Phase 진입 시 호출합니다.
        /// </summary>
        public void Deactivate()
        {
            _isActive = false;
        }

        /// <summary>
        /// 적 목록을 갱신합니다. EnemySpawner에서 적 등록/해제 시 호출합니다.
        /// </summary>
        public void SetEnemies(List<Enemy> enemies)
        {
            _enemies = enemies;
        }

        // ────────────────────────────────────────────────
        // 발사 루프
        // ────────────────────────────────────────────────

        private void Update()
        {
            if (!_isActive) return;

            foreach (var node in _shipGrid.PlacedNodes)
            {
                if (node.Data.NodeType != NodeType.Attack) continue;

                AttackStats stats = _powerGraph.GetEffectiveStats(node);

                // 동력이 없으면(데미지 0이면) 발사하지 않음
                if (stats.Damage <= 0f) continue;

                TryFire(node, stats);
            }
        }

        /// <summary>
        /// 단일 공격 노드의 발사를 시도합니다.
        /// 공격 속도 쿨다운, 사거리, 각도 조건을 모두 통과해야 발사합니다.
        /// </summary>
        private void TryFire(PlacedNode node, AttackStats stats)
        {
            // 공격속도 쿨다운 체크 (FireRate = 초당 발사 횟수)
            float interval = stats.FireRate > 0f ? 1f / stats.FireRate : float.MaxValue;
            if (_lastFireTimes.TryGetValue(node, out float lastTime) && Time.time - lastTime < interval)
                return;

            // 발사 방향 계산 (노드 면 방향 + 노드 회전 + 우주선 회전)
            Vector2 localFireDir = node.GetLocalFireDirection();
            Vector2 worldFireDir = transform.TransformDirection(localFireDir).normalized;

            // 사거리 + 발사 각도 내의 가장 가까운 적 탐색
            Enemy target = FindClosestEnemyInRange(node, worldFireDir, stats.AttackRange);
            if (target == null) return;

            // 발사
            FireAt(node, target, worldFireDir, stats);
            _lastFireTimes[node] = Time.time;
        }

        /// <summary>
        /// 사거리 내에 있고, 발사 각도 안에 들어오는 가장 가까운 적을 반환합니다.
        /// </summary>
        private Enemy FindClosestEnemyInRange(PlacedNode node, Vector2 fireDir, float range)
        {
            Vector3 nodeWorldPos = _shipGrid.NodeCenterToWorld(node);

            Enemy closest = null;
            float closestDist = float.MaxValue;

            foreach (var enemy in _enemies)
            {
                if (enemy == null || !enemy.gameObject.activeSelf) continue;

                float dist = Vector3.Distance(nodeWorldPos, enemy.transform.position);
                if (dist > range) continue;

                // 발사 각도 범위 내에 있는지 확인
                Vector2 toEnemy = ((Vector2)enemy.transform.position - (Vector2)nodeWorldPos).normalized;
                float angle = Vector2.Angle(fireDir, toEnemy);

                if (angle <= _firingHalfAngle && dist < closestDist)
                {
                    closestDist = dist;
                    closest = enemy;
                }
            }

            return closest;
        }

        /// <summary>
        /// 대상 적을 향해 발사체를 발사합니다.
        /// 멀티샷 설정에 따라 여러 발을 동시에 발사할 수 있습니다.
        /// </summary>
        private void FireAt(PlacedNode node, Enemy target, Vector2 baseFireDir, AttackStats stats)
        {
            Vector3 firePos = _shipGrid.NodeCenterToWorld(node);
            Vector2 aimDir = ((Vector2)target.transform.position - (Vector2)firePos).normalized;

            int count = Mathf.Max(1, stats.ProjectileCount);

            // 멀티샷: 여러 발을 부채꼴 형태로 발사
            float spreadAngle = count > 1 ? 10f : 0f;
            float startAngle = -(count - 1) * spreadAngle * 0.5f;

            for (int i = 0; i < count; i++)
            {
                float angle = startAngle + i * spreadAngle;
                Vector2 dir = RotateVector(aimDir, angle);

                ProjectilePool.Instance?.Get(
                    firePos,
                    dir,
                    stats.ProjectileSpeed,
                    stats.Damage,
                    stats.AttackRange * 1.2f,  // 사거리보다 약간 긴 비행거리
                    stats.PierceCount,
                    "Player"
                );
            }
        }

        /// <summary>
        /// 2D 벡터를 주어진 각도(도)만큼 회전시킵니다.
        /// </summary>
        private Vector2 RotateVector(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }
    }
}
