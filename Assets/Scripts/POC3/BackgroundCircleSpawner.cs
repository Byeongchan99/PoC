using UnityEngine;

namespace POC3
{
    public class BackgroundCircleSpawner : MonoBehaviour
    {
        [SerializeField] float spawnInterval = 0.5f;
        [SerializeField] float expandSpeed = 3f;
        [SerializeField] float maxRadius = 8f;
        [SerializeField] float lineWidth = 0.03f;
        [SerializeField] Color lineColor = new Color(1f, 1f, 1f, 0.4f);
        [SerializeField] int segments = 64;
        [SerializeField] Material lineMaterial;

        float timer;

        void Start()
        {
            // 시작 시 화면이 비어 보이지 않도록 여러 단계의 원을 미리 채움
            int preloadCount = Mathf.CeilToInt(maxRadius / (expandSpeed * spawnInterval));
            for (int i = 0; i < preloadCount; i++)
                SpawnCircle(i * spawnInterval * expandSpeed);
        }

        void Update()
        {
            timer += Time.deltaTime;
            if (timer < spawnInterval) return;

            timer = 0f;
            SpawnCircle(0f);
        }

        void SpawnCircle(float initialRadius)
        {
            var go = new GameObject("BackgroundCircle");
            go.transform.position = Vector3.zero;

            var circle = go.AddComponent<BackgroundCircle>();
            circle.Init(expandSpeed, maxRadius, lineWidth, lineColor, segments, lineMaterial, initialRadius);
        }
    }
}
