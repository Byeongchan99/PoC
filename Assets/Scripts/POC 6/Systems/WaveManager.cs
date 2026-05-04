using System;
using System.Collections.Generic;
using UnityEngine;

namespace POC6
{
    /// <summary>
    /// 웨이브 진행을 관리합니다.
    /// WaveData 목록을 순서대로 처리하고 현재 웨이브 번호를 추적합니다.
    /// 적 전멸 이벤트를 받아 GameManager에 웨이브 클리어를 알립니다.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        [Header("웨이브 데이터")]
        [Tooltip("5개의 WaveData ScriptableObject를 순서대로 연결합니다.")]
        [SerializeField] private List<WaveData> _waves = new();

        [Header("참조")]
        [SerializeField] private EnemySpawner _enemySpawner;

        /// <summary>웨이브가 클리어되었을 때 발행됩니다. (클리어한 웨이브 번호)</summary>
        public static event Action<int> OnWaveCleared;

        /// <summary>새 웨이브가 시작될 때 발행됩니다. (웨이브 번호)</summary>
        public static event Action<int> OnWaveStarted;

        // 현재 웨이브 인덱스 (0-based)
        private int _currentWaveIndex = 0;

        // 현재 웨이브가 진행 중인지
        private bool _isWaveActive = false;

        /// <summary>현재 웨이브 번호 (1-based)</summary>
        public int CurrentWaveNumber => _currentWaveIndex + 1;

        /// <summary>총 웨이브 수</summary>
        public int TotalWaves => _waves.Count;

        /// <summary>마지막 웨이브까지 모두 클리어했는지</summary>
        public bool IsAllWavesCleared => _currentWaveIndex >= _waves.Count;

        private void OnEnable()
        {
            if (_enemySpawner != null)
                _enemySpawner.OnAllEnemiesDefeated += HandleAllEnemiesDefeated;
        }

        private void OnDisable()
        {
            if (_enemySpawner != null)
                _enemySpawner.OnAllEnemiesDefeated -= HandleAllEnemiesDefeated;
        }

        // ────────────────────────────────────────────────
        // 공개 API (GameManager에서 호출)
        // ────────────────────────────────────────────────

        /// <summary>
        /// 웨이브 매니저를 초기 상태로 리셋합니다. 게임 시작 시 호출합니다.
        /// </summary>
        public void Initialize()
        {
            _currentWaveIndex = 0;
            _isWaveActive = false;
        }

        /// <summary>
        /// 현재 웨이브를 시작합니다. GameManager의 BuildPhase -> CombatPhase 전환 시 호출합니다.
        /// </summary>
        public void StartCurrentWave()
        {
            if (_isWaveActive || _currentWaveIndex >= _waves.Count) return;

            var wave = _waves[_currentWaveIndex];
            _isWaveActive = true;

            OnWaveStarted?.Invoke(CurrentWaveNumber);
            _enemySpawner.StartWave(wave);
        }

        /// <summary>
        /// 웨이브 실패 시 이전 웨이브 인덱스로 복귀합니다.
        /// 1웨이브에서 실패해도 1웨이브로 그대로 유지됩니다.
        /// </summary>
        public void RevertToPreviousWave()
        {
            _currentWaveIndex = Mathf.Max(0, _currentWaveIndex - 1);
            _isWaveActive = false;
            _enemySpawner.StopWave();
        }

        /// <summary>
        /// 현재 웨이브를 중단합니다 (실패 처리 시 사용).
        /// </summary>
        public void StopCurrentWave()
        {
            _isWaveActive = false;
            _enemySpawner.StopWave();
        }

        /// <summary>
        /// 현재 웨이브의 WaveData를 반환합니다.
        /// </summary>
        public WaveData GetCurrentWaveData()
        {
            if (_currentWaveIndex < _waves.Count)
                return _waves[_currentWaveIndex];
            return null;
        }

        // ────────────────────────────────────────────────
        // 이벤트 핸들러
        // ────────────────────────────────────────────────

        /// <summary>
        /// EnemySpawner에서 모든 적이 처치되었다고 알릴 때 호출됩니다.
        /// </summary>
        private void HandleAllEnemiesDefeated()
        {
            if (!_isWaveActive) return;

            _isWaveActive = false;
            int clearedWave = CurrentWaveNumber;
            _currentWaveIndex++;

            OnWaveCleared?.Invoke(clearedWave);
        }
    }
}
