using System;
using UnityEngine;

namespace POC8
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

        /// <summary>플레이어 공격력을 읽기 위한 참조. Inspector에서 연결해야 한다.</summary>
        [SerializeField] private PlayerCombat _playerCombat;

        /// <summary>링 중심에서 스폰 가능한 최대 반경. 링 내곽 반경보다 작아야 한다.</summary>
        [SerializeField] private float _spawnRadius = 4f;

        /// <summary>x축: 난이도(0~1), y축: 스폰량. 난이도에 따른 적 수 변화를 설정한다.</summary>
        [SerializeField] private AnimationCurve _spawnCountCurve = AnimationCurve.Linear(0f, 1f, 1f, 5f);

        /// <summary>이 웨이브 수에 도달하면 난이도가 1.0(최대)이 된다.</summary>
        [SerializeField] private int _maxDifficultyWaves = 30;


        /// <summary>각도 섹터 내에서 랜덤 배치 시 허용하는 지터 비율 (0~0.5).</summary>
        [SerializeField] private float _sectorJitter = 0.4f;

        /// <summary>적 간 최소 여백 (적 지름 외 추가 간격). 겹침 방지에 사용한다.</summary>
        [SerializeField] private float _spawnClearance = 0.2f;

        /// <summary>겹침 없는 위치를 찾지 못할 때 재시도하는 최대 횟수.</summary>
        [SerializeField] private int _spawnAttemptLimit = 20;

        // Enemy의 크기 공식을 미러링한다. Enemy._baseSize, _sizePerHealth와 동일한 값을 유지해야 한다.
        [SerializeField] private float _enemyBaseSize = 0.3f;
        [SerializeField] private float _enemySizePerHealth = 0.1f;
        [SerializeField] private float _enemyMaxVisualSize = 2.0f;

        private int _currentWave;

        /// <summary>
        /// 오브젝트 활성화 시 플레이어 돌진 시작 이벤트를 구독한다.
        /// </summary>
        private void OnEnable()
        {
            PlayerController.OnPlayerLanded += SpawnWave;
        }

        /// <summary>
        /// 오브젝트 비활성화 시 이벤트 구독을 해제한다.
        /// </summary>
        private void OnDisable()
        {
            PlayerController.OnPlayerLanded -= SpawnWave;
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
        /// 최대 체력은 플레이어 공격력의 2배로 제한되며, 2의 거듭제곱 단계 중 균등 랜덤 선택한다.
        /// </summary>
        private void SpawnWave()
        {
            _currentWave++;

            float difficulty = Mathf.Clamp01(_currentWave / (float)_maxDifficultyWaves);
            int spawnCount = Mathf.Max(1, Mathf.RoundToInt(_spawnCountCurve.Evaluate(difficulty)));

            // 최대 체력 = 플레이어 공격력 × 2.
            // 예: 공격력 64 → 최대 체력 128 → 지수 7 → 체력 2,4,8,16,32,64,128 중 랜덤.
            int playerAttackPower = _playerCombat != null ? _playerCombat.CurrentAttackPower : 1;
            int maxHealth = playerAttackPower * 2;

            // 최대 체력의 2의 거듭제곱 지수를 구한다. 예: 128 → 7, 64 → 6.
            int maxExponent = Mathf.FloorToInt(Mathf.Log(maxHealth, 2));

            // 겹침 체크에 사용할 적의 반경은 최대 체력 기준으로 계산한다 (최악의 크기 기준).
            float enemySize = Mathf.Min(_enemyBaseSize + maxHealth * _enemySizePerHealth, _enemyMaxVisualSize);
            float enemyRadius = enemySize / 2f;

            int actualSpawned = 0;
            for (int i = 0; i < spawnCount; i++)
            {
                // 1~maxExponent 범위에서 랜덤 지수를 선택하여 체력을 결정한다.
                // 예: maxExponent=3이면 지수 1,2,3 중 하나 → 체력 2,4,8 중 하나가 균등 확률로 선택된다.
                int randomExponent = UnityEngine.Random.Range(1, maxExponent + 1);
                int health = (int)Mathf.Pow(2, randomExponent);

                Vector2 spawnPos = GetSectorSpawnPosition(i, spawnCount, enemyRadius);
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
        /// 원형 영역을 spawnCount개의 각도 섹터로 나누고 index번째 섹터 안에서 위치를 찾는다.
        /// 겹침 체크를 통과할 때까지 _spawnAttemptLimit 횟수만큼 재시도한다.
        /// </summary>
        /// <param name="enemyRadius">스폰할 적의 반경. 겹침 판정 거리 계산에 사용한다.</param>
        private Vector2 GetSectorSpawnPosition(int index, int spawnCount, float enemyRadius)
        {
            float sectorAngle = 360f / Mathf.Max(spawnCount, 1);

            // 두 적이 겹치지 않으려면 중심 간 거리가 지름 + clearance 이상이어야 한다
            float minCenterDistance = enemyRadius * 2f + _spawnClearance;

            Vector2 candidate = (Vector2)transform.position;

            for (int attempt = 0; attempt < _spawnAttemptLimit; attempt++)
            {
                float baseAngle = sectorAngle * index;
                float jitterRange = sectorAngle * _sectorJitter;
                float angle = (baseAngle + UnityEngine.Random.Range(-jitterRange, jitterRange)) * Mathf.Deg2Rad;
                float dist = Mathf.Sqrt(UnityEngine.Random.Range(0.1f, 1f)) * _spawnRadius;

                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                candidate = (Vector2)transform.position + dir * dist;

                // minCenterDistance 반경 내에 다른 적이 없으면 이 위치를 사용한다
                Collider2D overlap = Physics2D.OverlapCircle(candidate, minCenterDistance);
                if (overlap == null || !overlap.TryGetComponent(out Enemy _))
                    return candidate;
            }

            // 재시도 한계 초과 시 마지막 후보 위치를 반환한다
            return candidate;
        }
    }
}
