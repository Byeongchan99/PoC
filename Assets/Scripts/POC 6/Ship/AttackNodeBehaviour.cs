using UnityEngine;

namespace POC6
{
    /// <summary>
    /// 공격 노드의 자동 조준 및 발사를 담당합니다.
    /// NodeVisualFactory가 Attack 타입 노드 생성 시 자동으로 부착합니다.
    /// 이전의 중앙집중식 AttackNodeShooter와 달리 노드 스스로 Update에서 처리합니다.
    /// </summary>
    public class AttackNodeBehaviour : MonoBehaviour
    {
        [Header("발사 설정")]
        [Tooltip("발사 가능 각도 범위 (노드 전방 기준 좌우). 90이면 전방 180도 전체.")]
        [Range(10f, 90f)]
        [SerializeField] private float _firingHalfAngle = 90f;

        // NodeVisualFactory에서 Initialize()로 주입되는 참조들
        private PlacedNode _placedNode;
        private ShipGrid _shipGrid;
        private PowerGraph _powerGraph;

        // 마지막 발사 시간 (공격 속도 쿨다운 제어)
        private float _lastFireTime;

        /// <summary>
        /// 노드 생성 시 NodeVisualFactory에서 호출합니다.
        /// </summary>
        public void Initialize(PlacedNode node, ShipGrid grid, PowerGraph powerGraph)
        {
            _placedNode = node;
            _shipGrid = grid;
            _powerGraph = powerGraph;
        }

        private void Update()
        {
            // 적이 없거나 초기화 전이면 처리 안 함
            if (_placedNode == null || Enemy.AllActive.Count == 0) return;

            AttackStats stats = _powerGraph.GetEffectiveStats(_placedNode);

            // 동력이 없으면 (Damage == 0) 발사 안 함
            if (stats.Damage <= 0f) return;

            TryFire(stats);
        }

        // ────────────────────────────────────────────────
        // 발사 로직
        // ────────────────────────────────────────────────

        /// <summary>
        /// 공격 속도 쿨다운, 사거리, 각도 조건을 확인하고 조건 충족 시 발사합니다.
        /// </summary>
        private void TryFire(AttackStats stats)
        {
            float interval = stats.FireRate > 0f ? 1f / stats.FireRate : float.MaxValue;
            if (Time.time - _lastFireTime < interval) return;

            // 노드 발사 방향을 월드 좌표로 변환
            // GetLocalFireDirection()은 우주선 로컬 기준이므로 ShipGrid의 Transform으로 변환
            Vector2 localFireDir = _placedNode.GetLocalFireDirection();
            Vector2 worldFireDir = _shipGrid.transform.TransformDirection(localFireDir).normalized;

            Enemy target = FindClosestEnemyInRange(worldFireDir, stats.AttackRange);
            if (target == null) return;

            FireAt(target, stats);
            _lastFireTime = Time.time;
        }

        /// <summary>
        /// 사거리 내에 있고 발사 각도 안에 들어오는 가장 가까운 적을 반환합니다.
        /// </summary>
        private Enemy FindClosestEnemyInRange(Vector2 fireDir, float range)
        {
            Vector3 nodeWorldPos = _shipGrid.NodeCenterToWorld(_placedNode);

            Enemy closest = null;
            float closestDist = float.MaxValue;

            foreach (var enemy in Enemy.AllActive)
            {
                if (enemy == null || !enemy.gameObject.activeSelf) continue;

                float dist = Vector3.Distance(nodeWorldPos, enemy.transform.position);
                if (dist > range) continue;

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
        /// ProjectileCount에 따라 부채꼴 멀티샷도 처리합니다.
        /// </summary>
        private void FireAt(Enemy target, AttackStats stats)
        {
            Vector3 firePos = _shipGrid.NodeCenterToWorld(_placedNode);
            Vector2 aimDir = ((Vector2)target.transform.position - (Vector2)firePos).normalized;

            int count = Mathf.Max(1, stats.ProjectileCount);
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
                    stats.AttackRange * 1.2f,
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
