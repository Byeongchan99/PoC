using UnityEngine;

namespace POC1
{
    public class EnemySpawner : MonoBehaviour
    {
        public GameObject enemyPrefab;

        [Tooltip("소환 간격 (초)")]
        public float spawnInterval = 5f;

        [Tooltip("화면 가장자리로부터 안쪽 여백 (뷰포트 비율 0~0.5)")]
        [Range(0f, 0.4f)]
        public float spawnMargin = 0.05f;

        [Tooltip("시작 HP")]
        public float baseHp = 30f;

        [Tooltip("분당 HP 증가 배율")]
        public float hpScalePerMin = 1f;

        [Tooltip("비주얼이 최대(strongColor/maxScale)에 도달하는 경과 시간 (초)")]
        public float maxStrengthTime = 300f;

        [Tooltip("게임 시작 시 소환할 적 수")]
        public int initialSpawnCount = 5;

        void Start()
        {
            for (int i = 0; i < initialSpawnCount; i++)
                SpawnEnemy();

            InvokeRepeating(nameof(SpawnEnemy), spawnInterval, spawnInterval);
        }

        void SpawnEnemy()
        {
            if (enemyPrefab == null) return;

            Vector2 pos = RandomScreenPosition();
            GameObject go = Instantiate(enemyPrefab, pos, Quaternion.identity);

            float elapsed = GameManager.Instance.ElapsedTime;
            float ratio = elapsed / 60f * hpScalePerMin;
            float hp = baseHp * (1f + ratio);

            float strengthRatio = Mathf.Clamp01(elapsed / maxStrengthTime);
            go.GetComponent<EnemyController>().Init(hp, strengthRatio);
        }

        Vector2 RandomScreenPosition()
        {
            float m = spawnMargin;
            float x = Random.Range(m, 1f - m);
            float y = Random.Range(m, 1f - m);
            return Camera.main.ViewportToWorldPoint(new Vector3(x, y, Mathf.Abs(Camera.main.transform.position.z)));
        }
    }
}
