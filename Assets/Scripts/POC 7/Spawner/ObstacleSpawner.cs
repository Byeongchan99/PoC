using UnityEngine;

namespace POC7
{
    /// <summary>
    /// 플레이어가 착지할 때마다 링 내부에 장애물을 스폰하는 컴포넌트.
    /// EnemySpawner와 동일하게 OnPlayerLanded 이벤트에 구독하여 같은 타이밍에 동작한다.
    /// </summary>
    public class ObstacleSpawner : MonoBehaviour
    {
        /// <summary>
        /// 스폰할 장애물 프리팹 목록. 스폰 시 이 배열에서 무작위로 선택한다.
        /// 각 프리팹에 Obstacle 컴포넌트가 부착되어 있고 "Obstacle" 레이어로 설정되어야 한다.
        /// </summary>
        [SerializeField] private GameObject[] _obstaclePrefabs;

        /// <summary>웨이브당 최소 스폰 수.</summary>
        [SerializeField] private int _minSpawnCount = 1;

        /// <summary>웨이브당 최대 스폰 수.</summary>
        [SerializeField] private int _maxSpawnCount = 2;

        /// <summary>링 중심에서 장애물을 스폰할 수 있는 최대 반경.</summary>
        [SerializeField] private float _spawnRadius = 3f;

        /// <summary>장애물 스폰 시 다른 콜라이더와의 최소 간격. 겹침 방지에 사용한다.</summary>
        [SerializeField] private float _spawnClearance = 0.5f;

        /// <summary>겹침 없는 위치를 찾지 못할 때 재시도하는 최대 횟수.</summary>
        [SerializeField] private int _spawnAttemptLimit = 20;

        /// <summary>
        /// 오브젝트 활성화 시 착지 이벤트를 구독한다.
        /// </summary>
        private void OnEnable()
        {
            PlayerController.OnPlayerLanded += SpawnObstacles;
        }

        /// <summary>
        /// 오브젝트 비활성화 시 이벤트 구독을 해제한다.
        /// </summary>
        private void OnDisable()
        {
            PlayerController.OnPlayerLanded -= SpawnObstacles;
        }

        /// <summary>
        /// GameManager가 게임 시작 시 첫 웨이브를 강제 스폰하기 위해 호출한다.
        /// </summary>
        public void SpawnInitialWave()
        {
            SpawnObstacles();
        }

        /// <summary>
        /// _minSpawnCount ~ _maxSpawnCount 범위 내 무작위 수의 장애물을 스폰한다.
        /// _obstaclePrefabs가 비어 있으면 아무것도 스폰하지 않는다.
        /// </summary>
        private void SpawnObstacles()
        {
            if (_obstaclePrefabs == null || _obstaclePrefabs.Length == 0)
                return;

            int count = Random.Range(_minSpawnCount, _maxSpawnCount + 1);

            for (int i = 0; i < count; i++)
            {
                GameObject prefab = _obstaclePrefabs[Random.Range(0, _obstaclePrefabs.Length)];
                if (prefab == null)
                    continue;

                Vector2 pos = GetRandomPosition();
                Instantiate(prefab, pos, Quaternion.identity);
            }
        }

        /// <summary>
        /// 링 내부에서 다른 콜라이더와 겹치지 않는 무작위 위치를 반환한다.
        /// _spawnAttemptLimit 횟수 안에 적합한 위치를 찾지 못하면 마지막 후보를 반환한다.
        ///
        /// [실무 권장]
        /// 스폰 영역이 복잡해지면 Poisson Disk Sampling으로 교체하면
        /// 더 균등한 분포와 안정적인 겹침 방지를 달성할 수 있다.
        /// </summary>
        private Vector2 GetRandomPosition()
        {
            Vector2 candidate = transform.position;

            for (int attempt = 0; attempt < _spawnAttemptLimit; attempt++)
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                // sqrt 분포를 사용하면 원형 영역에서 면적 균등 배치가 된다.
                float dist = Mathf.Sqrt(Random.Range(0.1f, 1f)) * _spawnRadius;
                candidate = (Vector2)transform.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;

                if (Physics2D.OverlapCircle(candidate, _spawnClearance) == null)
                    return candidate;
            }

            return candidate;
        }
    }
}
