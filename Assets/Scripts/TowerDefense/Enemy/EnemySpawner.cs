using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace POC4
{
    /// <summary>
    /// 적을 일정 간격으로 스폰하는 클래스.
    /// GridSystem의 SpawnPoint에서 Enemy 프리팹을 생성하고,
    /// PathFinder로 계산한 경로를 Enemy에게 전달한다.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        // -------------------------------------------------------
        // Inspector 노출 필드
        // -------------------------------------------------------

        [Header("References")]
        [Tooltip("스폰할 적 프리팹 (Enemy 컴포넌트 포함 필수)")]
        [SerializeField] private Enemy _enemyPrefab;

        [Tooltip("GridSystem 컴포넌트가 있는 GameObject")]
        [SerializeField] private GridSystem _gridSystem;

        [Tooltip("PathFinder 컴포넌트가 있는 GameObject")]
        [SerializeField] private PathFinder _pathFinder;

        [Header("Spawn Settings")]
        [Tooltip("한 라운드에 스폰할 적의 수 (기본값, 라운드마다 스케일링)")]
        [SerializeField] private int _enemyCountPerRound = 5;

        [Tooltip("적 스폰 사이의 간격 (초)")]
        [SerializeField] private float _spawnInterval = 1.5f;

        [Header("Scaling (Round Progression)")]
        [Tooltip("라운드마다 추가되는 적 수")]
        [SerializeField] private int _enemyCountIncreasePerRound = 2;

        [Tooltip("라운드마다 적 HP에 곱해지는 배율")]
        [SerializeField] private float _hpScalePerRound = 1.2f;

        // -------------------------------------------------------
        // 내부 상태
        // -------------------------------------------------------

        private int _currentRound = 1;
        private int _remainingEnemies; // 현재 라운드에서 아직 처치하지 못한 적 수
        private int _spawnedCount;     // 현재 라운드에서 스폰한 적 수
        private bool _isSpawning;

        /// <summary>
        /// 현재 계산된 경로. 벽 변경 없이는 동일한 경로를 재사용.
        /// </summary>
        private List<Vector2Int> _cachedPath;

        // -------------------------------------------------------
        // 유니티 생명주기
        // -------------------------------------------------------

        private void Start()
        {
            ValidateReferences();
        }

        // -------------------------------------------------------
        // 유효성 검사
        // -------------------------------------------------------

        /// <summary>
        /// 필수 참조가 Inspector에 연결되었는지 확인한다.
        /// </summary>
        private void ValidateReferences()
        {
            if (_enemyPrefab == null)
                Debug.LogError("[EnemySpawner] Enemy 프리팹이 Inspector에 연결되지 않았습니다.");
            if (_gridSystem == null)
                Debug.LogError("[EnemySpawner] GridSystem이 Inspector에 연결되지 않았습니다.");
            if (_pathFinder == null)
                Debug.LogError("[EnemySpawner] PathFinder가 Inspector에 연결되지 않았습니다.");
        }

        // -------------------------------------------------------
        // 스폰 시작 / 중단
        // -------------------------------------------------------

        /// <summary>
        /// 현재 라운드의 적 스폰을 시작한다.
        /// 전투 페이즈 진입 시 GameManager가 호출한다 (7단계 연동 예정).
        /// </summary>
        public void StartSpawning()
        {
            if (_isSpawning)
            {
                Debug.LogWarning("[EnemySpawner] 이미 스폰 중입니다.");
                return;
            }

            // 경로 계산 (벽이 없는 1단계에서는 직선 경로)
            _cachedPath = _pathFinder.FindDefaultPath();

            if (_cachedPath == null)
            {
                Debug.LogError("[EnemySpawner] 시작점에서 목표점까지의 경로를 찾을 수 없습니다.");
                return;
            }

            int totalEnemies = CalculateEnemyCount(_currentRound);
            _remainingEnemies = totalEnemies;
            _spawnedCount = 0;
            _isSpawning = true;

            Debug.Log($"[EnemySpawner] 라운드 {_currentRound} 시작. 적 {totalEnemies}마리 스폰 예정.");

            StartCoroutine(SpawnCoroutine(totalEnemies));
        }

        /// <summary>
        /// 스폰을 즉시 중단한다. (디버그 또는 게임 종료 시 사용)
        /// </summary>
        public void StopSpawning()
        {
            StopAllCoroutines();
            _isSpawning = false;
        }

        // -------------------------------------------------------
        // 스폰 코루틴
        // -------------------------------------------------------

        /// <summary>
        /// 일정 간격으로 적을 순차적으로 스폰하는 코루틴.
        /// </summary>
        private IEnumerator SpawnCoroutine(int count)
        {
            for (int i = 0; i < count; i++)
            {
                SpawnEnemy();
                _spawnedCount++;

                // 마지막 적이 아니면 다음 스폰 전에 대기
                if (i < count - 1)
                {
                    yield return new WaitForSeconds(_spawnInterval);
                }
            }

            _isSpawning = false;
        }

        /// <summary>
        /// 적 하나를 스폰하고 초기화한다.
        /// </summary>
        private void SpawnEnemy()
        {
            if (_enemyPrefab == null || _gridSystem == null) return;

            // SpawnPoint 월드 좌표에서 적 생성
            Vector3 spawnWorldPos = _gridSystem.GridToWorldPosition(_gridSystem.SpawnPoint);
            Enemy enemy = Instantiate(_enemyPrefab, spawnWorldPos, Quaternion.identity);

            // 라운드 스케일링 적용
            ApplyRoundScaling(enemy, _currentRound);

            // 경로 전달 및 이동 시작
            enemy.Initialize(_cachedPath, _gridSystem);
        }

        /// <summary>
        /// 라운드에 따라 적 스탯을 스케일링한다.
        /// HP는 매 라운드 _hpScalePerRound 배씩 증가.
        /// </summary>
        private void ApplyRoundScaling(Enemy enemy, int round)
        {
            // EnemyStatsScaler 컴포넌트로 분리 가능하나 POC 단계에서는 인라인 처리
            // HP 스케일링은 Enemy.ScaleStats()로 위임 (아래 참조)
            enemy.ScaleStats(_hpScalePerRound, round - 1);
        }

        // -------------------------------------------------------
        // 라운드 진행
        // -------------------------------------------------------

        /// <summary>
        /// 적이 처치되거나 목표 지점에 도달했을 때 호출되어야 한다.
        /// 모든 적이 처리되면 라운드 종료를 알린다.
        /// Enemy가 Destroy될 때 자동으로 호출되도록 연결 예정 (7단계에서 GameManager와 연동).
        /// </summary>
        public void OnEnemyRemoved()
        {
            _remainingEnemies--;

            if (_remainingEnemies <= 0 && !_isSpawning)
            {
                Debug.Log($"[EnemySpawner] 라운드 {_currentRound} 완료!");
                _currentRound++;
                // TODO: GameManager.Instance.OnRoundCleared();
            }
        }

        /// <summary>
        /// 현재 라운드 번호를 기반으로 스폰할 적의 수를 계산한다.
        /// 라운드 1부터 시작하며 매 라운드 _enemyCountIncreasePerRound 씩 증가.
        /// </summary>
        private int CalculateEnemyCount(int round)
        {
            return _enemyCountPerRound + _enemyCountIncreasePerRound * (round - 1);
        }

        // -------------------------------------------------------
        // Inspector ContextMenu (디버그 전용)
        // -------------------------------------------------------

        /// <summary>
        /// Inspector의 우클릭 메뉴에서 즉시 스폰을 시작할 수 있다.
        /// 에디터에서 빠른 테스트 용도.
        /// </summary>
        [ContextMenu("Debug: 스폰 시작")]
        private void DebugStartSpawning()
        {
            StartSpawning();
        }

        /// <summary>
        /// 씬에 존재하는 모든 적을 즉시 제거한다.
        /// </summary>
        [ContextMenu("Debug: 적 모두 제거")]
        private void DebugClearAllEnemies()
        {
            Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            foreach (Enemy enemy in enemies)
            {
                Destroy(enemy.gameObject);
            }
            _remainingEnemies = 0;
            Debug.Log($"[EnemySpawner] 적 {enemies.Length}마리 제거 완료.");
        }
    }
}
