using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace POC4
{
    /// <summary>
    /// 적 하나를 제어하는 클래스.
    /// 스탯(체력, 공격력, 이동 속도)을 갖고, PathFinder가 계산한 경로를 따라 목표 지점까지 이동한다.
    /// 목표 지점 도달 시 플레이어 HP를 감소시키고 자신을 제거한다.
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        // -------------------------------------------------------
        // Inspector 노출 필드
        // -------------------------------------------------------

        [Header("Stats")]
        [SerializeField] private float _maxHp = 10f;
        [SerializeField] private float _attackPower = 1f;
        [Tooltip("초당 이동하는 그리드 셀 수 (실수값)")]
        [SerializeField] private float _moveSpeed = 2f;

        [Header("Debug")]
        [Tooltip("Scene 뷰에서 현재 경로를 Gizmo로 표시할지 여부")]
        [SerializeField] private bool _showPathGizmo = true;
        [SerializeField] private Color _pathGizmoColor = Color.yellow;

        // -------------------------------------------------------
        // 내부 상태
        // -------------------------------------------------------

        private float _currentHp;

        /// <summary>이동할 경로 좌표 리스트 (시작점 포함, 목표점 포함)</summary>
        private List<Vector2Int> _path;

        /// <summary>현재 이동 중인 경로 인덱스</summary>
        private int _currentPathIndex;

        private GridSystem _gridSystem;

        /// <summary>이동 코루틴 핸들. 외부에서 멈출 수 있도록 보관.</summary>
        private Coroutine _moveCoroutine;

        // -------------------------------------------------------
        // 프로퍼티
        // -------------------------------------------------------

        public float AttackPower => _attackPower;
        public bool IsAlive => _currentHp > 0f;

        /// <summary>적이 경로를 따라 얼마나 진행했는지 (0~1 사이 값, 1 = 목표 도달)</summary>
        public float PathProgress
        {
            get
            {
                if (_path == null || _path.Count == 0) return 0f;
                return (float)_currentPathIndex / (_path.Count - 1);
            }
        }

        // -------------------------------------------------------
        // 초기화
        // -------------------------------------------------------

        /// <summary>
        /// 스폰 직후 EnemySpawner가 호출하는 초기화 메서드.
        /// 경로와 GridSystem을 전달받아 이동을 시작한다.
        /// </summary>
        public void Initialize(List<Vector2Int> path, GridSystem gridSystem)
        {
            _currentHp = _maxHp;
            _path = path;
            _gridSystem = gridSystem;
            _currentPathIndex = 0;

            if (_path == null || _path.Count == 0)
            {
                Debug.LogWarning("[Enemy] 경로가 없습니다. 이동을 시작할 수 없습니다.");
                return;
            }

            // 시작 위치를 경로의 첫 번째 셀로 즉시 이동
            transform.position = _gridSystem.GridToWorldPosition(_path[0]);

            _moveCoroutine = StartCoroutine(MoveAlongPath());
        }

        // -------------------------------------------------------
        // 피해 처리
        // -------------------------------------------------------

        /// <summary>
        /// 피해를 받아 HP를 감소시킨다. HP가 0 이하면 사망 처리.
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (!IsAlive) return;

            _currentHp -= damage;

            if (_currentHp <= 0f)
            {
                Die();
            }
        }

        /// <summary>
        /// 적이 사망했을 때 호출. 이동을 멈추고 오브젝트를 제거한다.
        /// </summary>
        private void Die()
        {
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
            }

            Destroy(gameObject);
        }

        // -------------------------------------------------------
        // 이동 로직
        // -------------------------------------------------------

        /// <summary>
        /// 경로를 따라 순서대로 각 웨이포인트로 이동하는 코루틴.
        /// 목표 지점(마지막 웨이포인트) 도달 시 플레이어 HP를 감소시킨다.
        /// </summary>
        private IEnumerator MoveAlongPath()
        {
            // 경로의 두 번째 노드(인덱스 1)부터 이동 (인덱스 0은 시작 위치)
            _currentPathIndex = 1;

            while (_currentPathIndex < _path.Count)
            {
                Vector3 targetWorldPos = _gridSystem.GridToWorldPosition(_path[_currentPathIndex]);

                // 현재 웨이포인트에 도달할 때까지 이동
                yield return MoveToPosition(targetWorldPos);

                _currentPathIndex++;
            }

            // 목표 지점 도달
            OnReachedGoal();
        }

        /// <summary>
        /// 목표 월드 좌표로 일정 속도로 이동하는 코루틴.
        /// 도달 판정은 그리드 셀 크기의 1%를 기준으로 한다.
        /// </summary>
        private IEnumerator MoveToPosition(Vector3 targetPos)
        {
            // 2D이므로 z축 무시
            targetPos.z = transform.position.z;

            float arrivalThreshold = _gridSystem.CellSize * 0.01f;

            while (Vector3.Distance(transform.position, targetPos) > arrivalThreshold)
            {
                // 초당 moveSpeed * cellSize 만큼 이동
                float step = _moveSpeed * _gridSystem.CellSize * Time.deltaTime;
                transform.position = Vector3.MoveTowards(transform.position, targetPos, step);
                yield return null;
            }

            transform.position = targetPos;
        }

        /// <summary>
        /// 목표 지점에 도달했을 때 호출.
        /// PlayerHealth 컴포넌트를 씬에서 찾아 피해를 입힌다.
        /// </summary>
        private void OnReachedGoal()
        {
            // PlayerHealth는 2단계 이후 구현 예정.
            // 지금은 로그로만 표시.
            Debug.Log($"[Enemy] 목표 지점 도달. 플레이어에게 {_attackPower} 피해!");

            // TODO: PlayerHealth.Instance.TakeDamage(_attackPower);

            Destroy(gameObject);
        }

        // -------------------------------------------------------
        // 스탯 스케일링 (라운드 진행에 따른 강화)
        // -------------------------------------------------------

        /// <summary>
        /// 라운드 스케일링을 적용한다.
        /// hpMultiplier를 roundCount 제곱만큼 곱해 HP를 증가시킨다.
        /// 예: 2라운드에서 hpMultiplier=1.2이면 maxHp * 1.2^1 적용.
        /// </summary>
        public void ScaleStats(float hpMultiplier, int roundCount)
        {
            if (roundCount <= 0) return;

            float scale = Mathf.Pow(hpMultiplier, roundCount);
            _maxHp *= scale;
            _currentHp = _maxHp;
        }

        // -------------------------------------------------------
        // 슬로우 / 기절 상태 이상 (6단계에서 확장 예정)
        // -------------------------------------------------------

        /// <summary>
        /// 일정 시간 동안 이동 속도를 비율로 감소시킨다.
        /// </summary>
        public void ApplySlow(float slowRatio, float duration)
        {
            StartCoroutine(SlowCoroutine(slowRatio, duration));
        }

        private IEnumerator SlowCoroutine(float slowRatio, float duration)
        {
            float originalSpeed = _moveSpeed;
            _moveSpeed *= (1f - slowRatio);
            yield return new WaitForSeconds(duration);
            _moveSpeed = originalSpeed;
        }

        /// <summary>
        /// 일정 시간 동안 이동을 멈추게 한다 (기절).
        /// </summary>
        public void ApplyStun(float duration)
        {
            StartCoroutine(StunCoroutine(duration));
        }

        private IEnumerator StunCoroutine(float duration)
        {
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
            }

            yield return new WaitForSeconds(duration);

            // 기절 해제 후 이동 재개
            _moveCoroutine = StartCoroutine(ResumePathFromCurrentIndex());
        }

        /// <summary>
        /// 현재 경로 인덱스에서 이동을 재개한다.
        /// 기절 해제 후 호출.
        /// </summary>
        private IEnumerator ResumePathFromCurrentIndex()
        {
            while (_currentPathIndex < _path.Count)
            {
                Vector3 targetWorldPos = _gridSystem.GridToWorldPosition(_path[_currentPathIndex]);
                yield return MoveToPosition(targetWorldPos);
                _currentPathIndex++;
            }

            OnReachedGoal();
        }

        // -------------------------------------------------------
        // Scene 뷰 Gizmo
        // -------------------------------------------------------

        /// <summary>
        /// Scene 뷰에서 현재 적의 이동 경로를 선으로 표시한다.
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!_showPathGizmo) return;
            if (_path == null || _path.Count < 2) return;
            if (_gridSystem == null) return;

            Gizmos.color = _pathGizmoColor;

            for (int i = _currentPathIndex; i < _path.Count - 1; i++)
            {
                Vector3 from = _gridSystem.GridToWorldPosition(_path[i]);
                Vector3 to = _gridSystem.GridToWorldPosition(_path[i + 1]);
                Gizmos.DrawLine(from, to);

                // 웨이포인트 위치에 작은 구 표시
                Gizmos.DrawWireSphere(to, _gridSystem.CellSize * 0.1f);
            }
        }
    }
}
