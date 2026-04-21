using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;

    [Tooltip("소환 간격 (초)")]
    public float spawnInterval = 5f;

    [Tooltip("소환 반경 (카메라 밖)")]
    public float spawnRadius = 9f;

    [Tooltip("시작 HP")]
    public float baseHp = 30f;

    [Tooltip("분당 HP 증가 배율")]
    public float hpScalePerMin = 1f;

    void Start()
    {
        InvokeRepeating(nameof(SpawnEnemy), 0f, spawnInterval);
    }

    void SpawnEnemy()
    {
        if (enemyPrefab == null) return;

        Vector2 pos = Random.insideUnitCircle.normalized * spawnRadius;
        GameObject go = Instantiate(enemyPrefab, pos, Quaternion.identity);

        float elapsed = GameManager.Instance.ElapsedTime;
        float hp = baseHp * (1f + elapsed / 60f * hpScalePerMin);

        go.GetComponent<EnemyController>().Init(hp);
    }
}
