using System;
using UnityEngine;

namespace POC7
{
    /// <summary>
    /// 플레이어 클릭마다 링 내부에 적을 스폰하는 컴포넌트.
    /// 난이도 곡선을 기반으로 스폰량과 적 체력을 조절한다.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        /// <summary>웨이브 스폰 완료 시 발생. 인자는 이번 웨이브에서 스폰된 적 수.</summary>
        public static event Action<int> OnEnemiesSpawned;

        /// <summary>스폰할 Enemy 프리팹. Enemy 컴포넌트가 부착되어 있어야 한다.</summary>
        [SerializeField] private GameObject _enemyPrefab;

        /// <summary>링 중심에서 스폰 가능한 최대 반경. 링 내곽 반경보다 작아야 한다.</summary>
        [SerializeField] private float _spawnRadius = 4f;

        /// <summary>x축: 난이도(0~1), y축: 스폰량. 난이도에 따른 적 수 변화를 설정한다.</summary>
        [SerializeField] private AnimationCurve _spawnCountCurve = AnimationCurve.Linear(0f, 1f, 1f, 5f);

        /// <summary>x축: 난이도(0~1), y축: 적 체력. 난이도에 따른 체력 변화를 설정한다.</summary>
        [SerializeField] private AnimationCurve _enemyHealthCurve = AnimationCurve.Linear(0f, 1f, 1f, 10f);

        /// <summary>이 웨이브 수에 도달하면 난이도가 1.0(최대)이 된다.</summary>
        [SerializeField] private int _maxDifficultyWaves = 30;

        /// <summary>새로 스폰할 적이 기존 적과 유지해야 할 최소 거리.</summary>
        [SerializeField] private float _minDistanceBetweenEnemies = 0.5f;

        /// <summary>스폰 위치를 찾지 못할 경우 재시도하는 최대 횟수.</summary>
        [SerializeField] private int _spawnAttemptLimit = 10;

        private int _currentWave;

        /// <summary>
        /// 오브젝트 활성화 시 플레이어 돌진 시작 이벤트를 구독한다.
        /// </summary>
        private void OnEnable()
        {
            PlayerController.OnDashStarted += SpawnWave;
        }

        /// <summary>
        /// 오브젝트 비활성화 시 이벤트 구독을 해제한다.
        /// </summary>
        private void OnDisable()
        {
            PlayerController.OnDashStarted -= SpawnWave;
        }

        /// <summary>
        /// 현재 웨이브 번호에 따른 난이도를 계산하고, 그에 맞는 수와 체력의 적을 스폰한다.
        /// PlayerController.OnDashStarted 이벤트 수신 시 호출된다.
        /// </summary>
        private void SpawnWave()
        {
            _currentWave++;

            float difficulty = Mathf.Clamp01(_currentWave / (float)_maxDifficultyWaves);
            int spawnCount = Mathf.Max(1, Mathf.RoundToInt(_spawnCountCurve.Evaluate(difficulty)));
            int health = Mathf.Max(1, Mathf.RoundToInt(_enemyHealthCurve.Evaluate(difficulty)));

            int actualSpawned = 0;
            for (int i = 0; i < spawnCount; i++)
            {
                Vector2 spawnPos = GetRandomSpawnPosition();
                if (SpawnEnemy(spawnPos, health))
                    actualSpawned++;
            }

            if (actualSpawned > 0)
                OnEnemiesSpawned?.Invoke(actualSpawned);
        }

        /// <summary>
        /// 지정한 위치에 적을 생성하고 체력을 초기화한다.
        ///
        /// [실무 권장]
        /// POC 단계에서는 Instantiate로 구현하지만, 실제 서비스에서는
        /// Unity의 ObjectPool(UnityEngine.Pool.ObjectPool)을 사용해
        /// GC 할당을 최소화하는 것을 권장한다.
        /// </summary>
        /// <returns>스폰 성공 시 true, 프리팹 미설정으로 실패 시 false</returns>
        private bool SpawnEnemy(Vector2 position, int health)
        {
            if (_enemyPrefab == null)
            {
                Debug.LogWarning("[EnemySpawner] Enemy 프리팹이 연결되지 않았습니다.");
                return false;
            }

            GameObject obj = Instantiate(_enemyPrefab, position, Quaternion.identity);

            if (obj.TryGetComponent(out Enemy enemy))
                enemy.Initialize(health);

            return true;
        }

        /// <summary>
        /// 링 중심 기준 spawnRadius 이내에서 기존 적과 겹치지 않는 랜덤 위치를 찾아 반환한다.
        /// spawnAttemptLimit 횟수 안에 조건을 만족하는 위치를 못 찾으면 마지막으로 시도한 위치를 반환한다.
        /// </summary>
        private Vector2 GetRandomSpawnPosition()
        {
            Vector2 candidate = Vector2.zero;

            for (int attempt = 0; attempt < _spawnAttemptLimit; attempt++)
            {
                // 원형 균등 분포: Random.insideUnitCircle은 중심 부근에 밀집하므로 sqrt로 보정한다
                Vector2 randomDir = UnityEngine.Random.insideUnitCircle.normalized;
                float randomDist = Mathf.Sqrt(UnityEngine.Random.value) * _spawnRadius;
                candidate = (Vector2)transform.position + randomDir * randomDist;

                // 해당 위치에 기존 적이 없으면 바로 사용한다
                Collider2D overlap = Physics2D.OverlapCircle(candidate, _minDistanceBetweenEnemies);
                if (overlap == null || !overlap.TryGetComponent(out Enemy _))
                    return candidate;
            }

            // 한계 횟수 초과 시 마지막 후보 위치를 그대로 사용한다
            return candidate;
        }
    }
}
