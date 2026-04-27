using System.Collections;
using UnityEngine;

namespace POC4
{
    /// <summary>
    /// 레이저 타워: 사거리 안의 적을 조준하고 지속적으로 피해를 입히는 타워.
    ///
    /// 공격 방식:
    ///   - 적이 사거리에 들어오는 즉시 레이저를 조준한다.
    ///   - 매 _damageTickInterval 초마다 (공격력 × _damageTickInterval) 피해를 적용한다.
    ///     → 초당 실질 피해량 = 공격력 (attackPower / s)
    ///   - 적이 죽거나 사거리 밖으로 나가면 레이저를 끄고 새 타겟을 탐색한다.
    ///
    /// 시각 효과:
    ///   LineRenderer로 타워와 타겟 사이에 빨간 선을 그린다.
    ///   Awake에서 생성하므로 Initialize 전에 준비된다.
    /// </summary>
    public class LaserTower : Tower
    {
        // -------------------------------------------------------
        // Inspector 노출 필드
        // -------------------------------------------------------

        [Header("Laser Tower Settings")]
        [Tooltip("피해 적용 간격 (초). 기본 0.1초마다 (공격력 × 0.1) 피해.")]
        [SerializeField] private float _damageTickInterval = 0.1f;

        [Header("Laser Visual")]
        [Tooltip("레이저 선 색상")]
        [SerializeField] private Color _laserColor = new Color(1f, 0.15f, 0.15f, 0.9f);

        [Tooltip("레이저 선 굵기 (월드 단위)")]
        [SerializeField] private float _laserWidth = 0.08f;

        // -------------------------------------------------------
        // 내부 상태
        // -------------------------------------------------------

        private LineRenderer _lineRenderer;

        // -------------------------------------------------------
        // 유니티 생명주기
        // -------------------------------------------------------

        private void Awake()
        {
            SetupLineRenderer();
        }

        // -------------------------------------------------------
        // LineRenderer 초기화
        // -------------------------------------------------------

        /// <summary>
        /// 레이저 시각 효과에 사용할 LineRenderer를 코드로 생성한다.
        /// </summary>
        private void SetupLineRenderer()
        {
            _lineRenderer = gameObject.AddComponent<LineRenderer>();
            _lineRenderer.positionCount = 2;
            _lineRenderer.startWidth = _laserWidth;
            _lineRenderer.endWidth = _laserWidth;

            // Sprites/Default 셰이더: 2D 환경에서 색상이 올바르게 표현된다.
            _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _lineRenderer.startColor = _laserColor;
            _lineRenderer.endColor = _laserColor;

            // 레이저가 다른 게임 오브젝트 위에 그려지도록 소팅 순서를 높게 설정
            _lineRenderer.sortingOrder = 8;
            _lineRenderer.enabled = false;
        }

        // -------------------------------------------------------
        // 공격 루프 (Tower 기본 루프를 override)
        // -------------------------------------------------------

        /// <summary>
        /// 타겟을 탐색하고, 발견하면 사거리 안에 있는 동안 지속 피해를 입힌다.
        /// 타겟이 없거나 사거리를 벗어나면 레이저를 끄고 다시 탐색 대기한다.
        /// </summary>
        protected override IEnumerator AttackLoop()
        {
            while (true)
            {
                Enemy target = FindFurthestEnemyInRange();

                if (target == null || !target.IsAlive)
                {
                    // 타겟 없음: 레이저 끄고 다음 프레임에 재탐색
                    _lineRenderer.enabled = false;
                    yield return null;
                    continue;
                }

                // 타겟이 사거리 안에 있는 동안 지속 공격
                while (target != null && target.IsAlive && IsInRange(target))
                {
                    UpdateLaserVisual(target.transform.position);
                    Attack(target);
                    yield return new WaitForSeconds(_damageTickInterval);
                }

                _lineRenderer.enabled = false;
            }
        }

        // -------------------------------------------------------
        // 공격 구현
        // -------------------------------------------------------

        /// <summary>
        /// 틱 피해를 적용한다.
        /// 한 틱의 피해량 = 공격력 × 틱 간격 → DPS = 공격력.
        /// ExtraDamage 효과도 동일한 비율로 적용한다.
        /// </summary>
        protected override void Attack(Enemy target)
        {
            float tickDamage = _attackPower * _damageTickInterval;

            if (_effectType == TowerData.TowerEffectType.ExtraDamage)
                tickDamage += _extraDamage * _damageTickInterval;

            target.TakeDamage(tickDamage);

            // 슬로우: 매 틱마다 갱신 → 레이저가 닿는 동안 지속 슬로우
            if (_effectType == TowerData.TowerEffectType.Slow)
                target.ApplySlow(_slowRatio, _slowDuration);

            // 기절: 매 틱마다 갱신 → 레이저가 닿는 동안 지속 기절 (적 사실상 고정)
            if (_effectType == TowerData.TowerEffectType.Stun)
                target.ApplyStun(_stunDuration);
        }

        // -------------------------------------------------------
        // 보조 메서드
        // -------------------------------------------------------

        /// <summary>
        /// 레이저 선을 타워에서 타겟 위치로 갱신한다.
        /// </summary>
        private void UpdateLaserVisual(Vector3 targetWorldPos)
        {
            _lineRenderer.enabled = true;

            Vector3 start = transform.position;
            Vector3 end = targetWorldPos;
            start.z = 0f;
            end.z = 0f;

            _lineRenderer.SetPosition(0, start);
            _lineRenderer.SetPosition(1, end);
        }

        /// <summary>
        /// 지정한 적이 사거리 내에 있는지 확인한다.
        /// </summary>
        private bool IsInRange(Enemy enemy)
        {
            if (enemy == null) return false;
            return Vector3.Distance(transform.position, enemy.transform.position) <= _range;
        }
    }
}
