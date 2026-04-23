using UnityEngine;

namespace POC3
{
    public class HexRingSpawner : MonoBehaviour
    {
        [SerializeField] GameObject hexRingPrefab;

        float spawnTimer;

        // 난이도에 따라 2.5초 → 0.5초로 스폰 간격 감소
        float SpawnInterval => Mathf.Lerp(2.5f, 0.5f, GameManager.Instance.Difficulty);

        void Update()
        {
            if (GameManager.Instance.CurrentState != GameManager.State.Playing) return;

            spawnTimer += Time.deltaTime;
            if (spawnTimer < SpawnInterval) return;

            spawnTimer = 0f;
            Instantiate(hexRingPrefab, Vector3.zero, Quaternion.identity);
        }
    }
}
