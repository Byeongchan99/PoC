using UnityEngine;

namespace POC3
{
    public class HexRingSpawner : MonoBehaviour
    {
        [SerializeField] GameObject hexRingPrefab;
        [SerializeField] Transform worldContainer;

        float spawnTimer;

        // 난이도에 따라 2.5초 → 0.5초로 스폰 간격 감소
        float SpawnInterval => Mathf.Lerp(2.5f, 0.5f, GameManager.Instance.Difficulty);

        void Update()
        {
            if (GameManager.Instance.CurrentState != GameManager.State.Playing) return;

            spawnTimer += Time.deltaTime;
            if (spawnTimer < SpawnInterval) return;

            spawnTimer = 0f;
            // WorldContainer의 자식으로 생성, local position/rotation = 0으로 명시
            // Quaternion.identity를 world rotation으로 넘기면 WorldContainer 회전값이
            // local rotation에서 상쇄되어 이상한 각도가 저장됨
            var ring = Instantiate(hexRingPrefab, worldContainer);
            ring.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
    }
}
