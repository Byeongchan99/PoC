using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace POC4
{
    /// <summary>
    /// 적을 일정 간격으로 스폰하는 클래스.
    /// GridSystem의 SpawnPoint에서 Enemy 프리팹을 생성하고,
    /// PathFinder로 계산한 경로를 Enemy에게 전달한다.
    ///
    /// 7단계 연동:
    ///   GameManager가 StartSpawning(count, roundScaleIndex)을 호출해 전투 페이즈를 시작한다.
    ///   모든 적이 처치/목표 도달하면 OnAllEnemiesDefeated 이벤트를 발생시킨다.
    ///   GameManager가 이 이벤트를 받아 다음 준비 페이즈로 전환한다.
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
        [Tooltip("적 스폰 사이의 간격 (초)")]
        [SerializeField] private float _spawnInterval = 1.5f;

        [Header("Scaling")]
        [Tooltip("라운드마다 적 HP에 곱해지는 배율")]
        [SerializeField] private float _hpScalePerRound = 1.2f;

        // -------------------------------------------------------
        // 이벤트
        // -------------------------------------------------------

        /// <summary>
        /// 현재 라운드의 모든 적이 처치되거나 목표 지점에 도달했을 때 발생한다.
        /// GameManager가 구독해 준비 페이즈로 전환한다.
        /// </summary>
        public event Action OnAllEnemiesDefeated;

        // -------------------------------------------------------
        // 내부 상태
        // -------------------------------------------------------

        /// <summary>현재 라운드에서 아직 처리(처치/목표 도달)되지 않은 적 수</summary>
        private int _remainingEnemies;

        /// <summary>현재 스폰 코루틴이 실행 중인지 여부 (마지막 적 스폰 전)</summary>
        private bool _isSpawning;

        /// <summary>현재 라운드의 HP 스케일링 인덱스 (0 = 스케일링 없음)</summary>
        private int _roundScaleIndex;

        /// <summary>현재 라운드에서 계산된 경로. 벽 변경 없이 재사용.</summary>
        private List<Vector2Int> _cachedPath;

        // -------------------------------------------------------
        // 유니티 생명주기
        // -------------------------------------------------------

        private void Start()
        {
            ValidateReferences();
        }

        private void OnEnable()
        {
            // 적 제거 이벤트를 구독해 남은 적 수를 추적한다.
            Enemy.OnAnyEnemyDefeated += HandleEnemyDefeated;
        }

        private void OnDisable()
        {
            Enemy.OnAnyEnemyDefeated -= HandleEnemyDefeated;
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
        /// 전투 페이즈 진입 시 GameManager가 호출한다.
        /// totalEnemies: 이번 라운드에 스폰할 총 적 수 (GameManager가 계산해서 전달).
        /// roundScaleIndex: HP 스케일링 지수 (1라운드=0, 2라운드=1, ...).
        /// </summary>
        public void StartSpawning(int totalEnemies, int roundScaleIndex)
        {
            if (_isSpawning)
            {
                Debug.LogWarning("[EnemySpawner] 이미 스폰 중입니다.");
                return;
            }

            _cachedPath = _pathFinder.FindDefaultPath();

            if (_cachedPath == null)
            {
                Debug.LogError("[EnemySpawner] 시작점에서 목표점까지의 경로를 찾을 수 없습니다.");
                return;
            }

            _roundScaleIndex = roundScaleIndex;
            _remainingEnemies = totalEnemies;
            _isSpawning = true;

            Debug.Log($"[EnemySpawner] 스폰 시작. 적 {totalEnemies}마리 (스케일 인덱스: {roundScaleIndex})");

            StartCoroutine(SpawnCoroutine(totalEnemies));
        }

        /// <summary>
        /// 스폰을 즉시 중단한다. 게임 오버 시 GameManager가 호출한다.
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
        /// 일정 간격으로 적을 순차적으로 스폰한다.
        /// 모든 적 스폰 완료 후 남은 적이 없으면 라운드 종료를 처리한다.
        /// </summary>
        private IEnumerator SpawnCoroutine(int count)
        {
            for (int i = 0; i < count; i++)
            {
                SpawnEnemy();

                if (i < count - 1)
                    yield return new WaitForSeconds(_spawnInterval);
            }

            _isSpawning = false;

            // 모든 적이 이미 처치된 경우를 대비해 라운드 완료 여부를 확인한다.
            CheckRoundComplete();
        }

        /// <summary>
        /// 적 하나를 스폰하고 경로 및 스케일링을 적용해 초기화한다.
        /// </summary>
        private void SpawnEnemy()
        {
            if (_enemyPrefab == null || _gridSystem == null) return;

            Vector3 spawnWorldPos = _gridSystem.GridToWorldPosition(_gridSystem.SpawnPoint);
            Enemy enemy = Instantiate(_enemyPrefab, spawnWorldPos, Quaternion.identity);

            enemy.ScaleStats(_hpScalePerRound, _roundScaleIndex);
            enemy.Initialize(_cachedPath, _gridSystem);
        }

        // -------------------------------------------------------
        // 적 처리 추적
        // -------------------------------------------------------

        /// <summary>
        /// Enemy.OnAnyEnemyDefeated 이벤트를 받아 남은 적 수를 줄인다.
        /// 모든 적이 처리되면 OnAllEnemiesDefeated를 발생시킨다.
        /// </summary>
        private void HandleEnemyDefeated()
        {
            _remainingEnemies = Mathf.Max(0, _remainingEnemies - 1);
            CheckRoundComplete();
        }

        /// <summary>
        /// 남은 적이 없고 스폰도 완료된 경우 라운드 종료 이벤트를 발생시킨다.
        /// </summary>
        private void CheckRoundComplete()
        {
            if (_isSpawning || _remainingEnemies > 0) return;

            Debug.Log("[EnemySpawner] 모든 적 처치 완료!");
            OnAllEnemiesDefeated?.Invoke();
        }

        // -------------------------------------------------------
        // Inspector ContextMenu (디버그 전용)
        // -------------------------------------------------------

        /// <summary>
        /// 스케일링 없이 기본 5마리로 즉시 스폰을 시작한다.
        /// </summary>
        [ContextMenu("Debug: 스폰 시작 (기본 5마리)")]
        private void DebugStartSpawning()
        {
            StartSpawning(5, 0);
        }

        /// <summary>
        /// 씬에 존재하는 모든 적을 즉시 처치한다.
        /// Die() 경로를 통해 이벤트가 정상적으로 발생한다.
        /// </summary>
        [ContextMenu("Debug: 적 모두 제거")]
        private void DebugClearAllEnemies()
        {
            Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            foreach (Enemy enemy in enemies)
            {
                // TakeDamage를 통해 Die()가 호출되도록 해 이벤트가 정상 발생한다.
                enemy.TakeDamage(99999f);
            }
            Debug.Log($"[EnemySpawner] 적 {enemies.Length}마리 제거 완료.");
        }
    }
}
