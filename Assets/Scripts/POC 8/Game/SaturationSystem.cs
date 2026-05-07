using System;
using UnityEngine;

namespace POC8
{
    /// <summary>
    /// 살아있는 적 수를 추적하고, 임계치 도달 시 게임 오버를 발생시키는 컴포넌트.
    /// </summary>
    public class SaturationSystem : MonoBehaviour
    {
        /// <summary>패배 조건. 살아있는 적 수가 이 값에 도달하면 OnGameOver가 발생한다.</summary>
        [SerializeField] private int _maxEnemyCount = 20;

        /// <summary>게임 오버 시 발생하는 정적 이벤트. GameManager 등이 구독한다.</summary>
        public static event Action OnGameOver;

        /// <summary>카운트가 변경될 때마다 발생. 인자는 (현재 수, 최대 수). UI 갱신에 사용한다.</summary>
        public event Action<int, int> OnSaturationChanged;

        private int _currentEnemyCount;

        /// <summary>현재 살아있는 적 수. 외부에서 읽기 전용.</summary>
        public int CurrentEnemyCount => _currentEnemyCount;

        /// <summary>포화도 비율 (0~1). UI 슬라이더 등에 바인딩한다.</summary>
        public float SaturationRatio => _maxEnemyCount > 0
            ? Mathf.Clamp01(_currentEnemyCount / (float)_maxEnemyCount)
            : 0f;

        /// <summary>
        /// 오브젝트 활성화 시 스폰 및 처치 이벤트를 구독한다.
        /// </summary>
        private void OnEnable()
        {
            EnemySpawner.OnEnemiesSpawned += IncreaseCount;
            Enemy.OnEnemyKilled += HandleEnemyKilled;
        }

        /// <summary>
        /// 오브젝트 비활성화 시 이벤트 구독을 해제한다.
        /// </summary>
        private void OnDisable()
        {
            EnemySpawner.OnEnemiesSpawned -= IncreaseCount;
            Enemy.OnEnemyKilled -= HandleEnemyKilled;
        }

        /// <summary>
        /// 스폰된 적 수만큼 카운트를 증가시킨다. EnemySpawner.OnEnemiesSpawned 이벤트 수신 시 호출된다.
        /// </summary>
        private void IncreaseCount(int amount)
        {
            _currentEnemyCount += amount;
            OnSaturationChanged?.Invoke(_currentEnemyCount, _maxEnemyCount);
            CheckGameOver();
        }

        /// <summary>
        /// 적 처치 시 카운트를 1 감소시킨다. Enemy.OnEnemyKilled 이벤트 수신 시 호출된다.
        /// </summary>
        private void HandleEnemyKilled(Enemy enemy)
        {
            _currentEnemyCount = Mathf.Max(0, _currentEnemyCount - 1);
            OnSaturationChanged?.Invoke(_currentEnemyCount, _maxEnemyCount);
        }

        /// <summary>
        /// 현재 적 수가 임계치에 도달했는지 확인하고, 도달했으면 OnGameOver 이벤트를 발생시킨다.
        /// </summary>
        private void CheckGameOver()
        {
            if (_currentEnemyCount < _maxEnemyCount)
                return;

            Debug.Log($"[SaturationSystem] 게임 오버: 적 수 {_currentEnemyCount}/{_maxEnemyCount}");
            OnGameOver?.Invoke();
        }
    }
}
