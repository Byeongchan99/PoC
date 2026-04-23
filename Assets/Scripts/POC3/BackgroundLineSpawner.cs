using UnityEngine;

namespace POC3
{
    /// <summary>
    /// 일정 간격으로 BackgroundLine 오브젝트를 생성하여
    /// 원점에서 사방으로 직선이 뻗어나가는 시각 효과를 만듭니다.
    /// WorldContainer와 독립적으로 동작합니다.
    /// </summary>
    public class BackgroundLineSpawner : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] GameObject backgroundLinePrefab;

        [Header("Spawn Settings")]
        // 몇 초마다 새 선을 생성할지
        [SerializeField] float spawnInterval = 0.08f;

        // 씬 시작 시 화면을 채우기 위해 미리 생성할 선의 수
        [SerializeField] int preloadCount = 30;

        [Header("Line Properties")]
        [SerializeField] float lineSpeed    = 4f;
        [SerializeField] float lineLength   = 0.6f;
        [SerializeField] float maxDistance  = 12f;
        [SerializeField] float lineWidth    = 0.03f;
        [SerializeField] Color lineColor    = new Color(1f, 1f, 1f, 0.25f);

        [Header("Material")]
        // Sprites/Default 등 LineRenderer에 사용할 머티리얼
        [SerializeField] Material lineMaterial;

        float _spawnTimer;

        void Start()
        {
            // 첫 프레임에 화면이 비어 보이지 않도록 다양한 거리에 선을 미리 생성
            for (int i = 0; i < preloadCount; i++)
            {
                float preloadDist = Random.Range(0f, maxDistance);
                SpawnLine(preloadDist);
            }
        }

        void Update()
        {
            _spawnTimer += Time.deltaTime;
            if (_spawnTimer < spawnInterval) return;

            _spawnTimer = 0f;
            SpawnLine(0f);
        }

        /// <summary>
        /// 임의의 방향으로 BackgroundLine을 하나 생성합니다.
        /// </summary>
        /// <param name="startDist">선의 꼬리(start point)가 시작할 원점 기준 거리</param>
        void SpawnLine(float startDist)
        {
            float angleDeg = Random.Range(0f, 360f);

            var go = Instantiate(backgroundLinePrefab, Vector3.zero, Quaternion.identity, transform);

            // lineMaterial이 할당된 경우에만 적용 (미할당 시 LineRenderer 기본값 사용)
            if (lineMaterial != null)
                go.GetComponent<LineRenderer>().material = lineMaterial;

            go.GetComponent<BackgroundLine>().Initialize(
                angleDeg, startDist, lineLength,
                lineSpeed, maxDistance,
                lineWidth, lineColor
            );
        }
    }
}
