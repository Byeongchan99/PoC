using System;
using UnityEngine;

namespace POC7
{
    /// <summary>
    /// 플레이어 클릭마다 링 내부에 적을 스폰하는 컴포넌트.
    /// 난이도에 따라 스폰량과 체력을 조절하며, 적이 원형 영역에 고루 퍼지도록 배치한다.
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

        /// <summary>이 웨이브 수에 도달하면 난이도가 1.0(최대)이 된다.</summary>
        [SerializeField] private int _maxDifficultyWaves = 30;

        /// <summary>
        /// 난이도 0일 때 적 체력 지수. 체력 = 2^지수이므로 기본값 1 → 시작 체력 2.
        /// </summary>
        [SerializeField] private int _healthMinExponent = 1;

        /// <summary>
        /// 난이도 1일 때 적 체력 지수. 기본값 4 → 최대 체력 16 (2, 4, 8, 16 순서로 증가).
        /// </summary>
        [SerializeField] private int _healthMaxExponent = 4;

        /// <summary>각도 섹터 내에서 랜덤 배치 시 허용하는 지터 비율 (0~0.5).</summary>
        [SerializeField] private float _sectorJitter = 0.4f;

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
        /// GameManager가 게임 시작 시 첫 웨이브를 강제 스폰하기 위해 호출한다.
        /// </summary>
        public void SpawnInitialWave()
        {
            SpawnWave();
        }

        /// <summary>
        /// 현재 웨이브 번호에 따른 난이도를 계산하고, 그에 맞는 수와 체력의 적을 스폰한다.
        /// 체력은 2의 거듭제곱으로 증가: 2 → 4 → 8 → 16
        /// </summary>
        private void SpawnWave()
        {
            _currentWave++;

            float difficulty = Mathf.Clamp01(_currentWave / (float)_maxDifficultyWaves);
            int spawnCount = Mathf.Max(1, Mathf.RoundToInt(_spawnCountCurve.Evaluate(difficulty)));

            // 체력을 2의 거듭제곱으로 계산한다.
            // 예: exponent=1→2, exponent=2→4, exponent=3→8, exponent=4→16
            int exponent = Mathf.RoundToInt(Mathf.Lerp(_healthMinExponent, _healthMaxExponent, difficulty));
            int health = Mathf.Max(1, (int)Mathf.Pow(2, exponent));

            int actualSpawned = 0;
            for (int i = 0; i < spawnCount; i++)
            {
                Vector2 spawnPos = GetSectorSpawnPosition(i, spawnCount);
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
        /// 원형 영역을 spawnCount개의 각도 섹터로 나누고, index번째 섹터 안에서 랜덤 위치를 반환한다.
        /// 이 방식은 적들이 특정 방향에 몰리지 않고 링 전체에 고루 퍼지게 한다.
        /// </summary>
        private Vector2 GetSectorSpawnPosition(int index, int spawnCount)
        {
            float sectorAngle = 360f / Mathf.Max(spawnCount, 1);

            // 섹터 중앙 각도에 ±jitter 범위의 랜덤 오프셋을 추가한다
            float baseAngle = sectorAngle * index;
            float jitterRange = sectorAngle * _sectorJitter;
            float angle = (baseAngle + UnityEngine.Random.Range(-jitterRange, jitterRange)) * Mathf.Deg2Rad;

            // sqrt 보정으로 원형 내부에 균등하게 분포시킨다
            float dist = Mathf.Sqrt(UnityEngine.Random.Range(0.1f, 1f)) * _spawnRadius;

            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            return (Vector2)transform.position + dir * dist;
        }
    }
}
