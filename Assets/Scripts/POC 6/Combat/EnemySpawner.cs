using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace POC6
{
    /// <summary>
    /// WaveData를 기반으로 화면 밖 360도에서 적을 스폰하는 시스템입니다.
    /// 스폰이 모두 완료되면 WaveManager에 알립니다.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private ShipGrid _shipGrid;
        [SerializeField] private Transform _shipTransform;

        [Header("스폰 설정")]
        [Tooltip("우주선 중심에서 적이 스폰될 반경. 카메라 시야 밖이어야 합니다.")]
        [SerializeField] private float _spawnRadius = 20f;

        // 현재 활성화된 적 목록 (WaveManager 완료 판정에 사용)
        private List<Enemy> _activeEnemies = new();

        // 현재 진행 중인 스폰 코루틴
        private Coroutine _spawnCoroutine;

        // 스폰 예정인 총 적 수 (웨이브 완료 판정에 사용)
        private int _totalEnemiesInWave;

        // 처치된 적 수
        private int _defeatedCount;

        /// <summary>이 웨이브의 모든 적이 처치되었을 때 발행됩니다.</summary>
        public event System.Action OnAllEnemiesDefeated;

        private void OnEnable()
        {
            Enemy.OnEnemyDied += HandleEnemyDied;
        }

        private void OnDisable()
        {
            Enemy.OnEnemyDied -= HandleEnemyDied;
        }

        // ────────────────────────────────────────────────
        // 공개 API
        // ────────────────────────────────────────────────

        /// <summary>
        /// 웨이브를 시작합니다. GameManager에서 Combat Phase 진입 시 호출합니다.
        /// </summary>
        public void StartWave(WaveData waveData)
        {
            // 이전 웨이브 잔존 적 처리
            DespawnAllEnemies();

            _totalEnemiesInWave = waveData.GetTotalEnemyCount();
            _defeatedCount = 0;

            _spawnCoroutine = StartCoroutine(SpawnRoutine(waveData));
        }

        /// <summary>
        /// 모든 적을 제거하고 스폰을 중단합니다.
        /// </summary>
        public void StopWave()
        {
            if (_spawnCoroutine != null)
            {
                StopCoroutine(_spawnCoroutine);
                _spawnCoroutine = null;
            }

            DespawnAllEnemies();
        }

        /// <summary>
        /// 현재 활성 적 목록을 반환합니다.
        /// </summary>
        public List<Enemy> GetActiveEnemies() => _activeEnemies;

        // ────────────────────────────────────────────────
        // 스폰 루틴
        // ────────────────────────────────────────────────

        /// <summary>
        /// WaveData의 SpawnInfos를 순서대로 처리해서 적을 스폰하는 코루틴입니다.
        /// </summary>
        private IEnumerator SpawnRoutine(WaveData waveData)
        {
            foreach (var spawnInfo in waveData.SpawnInfos)
            {
                // 그룹 스폰 지연
                if (spawnInfo.SpawnDelay > 0f)
                    yield return new WaitForSeconds(spawnInfo.SpawnDelay);

                for (int i = 0; i < spawnInfo.Count; i++)
                {
                    SpawnEnemy(spawnInfo.EnemyType);

                    if (spawnInfo.SpawnInterval > 0f)
                        yield return new WaitForSeconds(spawnInfo.SpawnInterval);
                }
            }
        }

        /// <summary>
        /// 우주선 주변 360도 랜덤 위치에서 적 한 마리를 스폰합니다.
        /// 카메라가 우주선을 따라다니므로 화면 밖의 기준이 우주선 위치입니다.
        /// VisualPrefab이 없으면 기본 원형 도형으로 생성합니다.
        /// </summary>
        private void SpawnEnemy(EnemyData enemyData)
        {
            if (enemyData == null) return;

            Vector3 shipPos = _shipTransform != null ? _shipTransform.position : Vector3.zero;

            // 360도 랜덤 방향으로 spawnRadius 거리에 스폰
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            Vector3 spawnPos = shipPos + (Vector3)(randomDir * _spawnRadius);

            GameObject enemyObj;

            if (enemyData.VisualPrefab != null)
            {
                enemyObj = Instantiate(enemyData.VisualPrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                // 프리팹 없을 때 기본 원형 도형으로 생성
                enemyObj = new GameObject($"Enemy_{enemyData.EnemyName}");
                enemyObj.transform.position = spawnPos;
                enemyObj.transform.localScale = Vector3.one * 0.8f;

                var rb = enemyObj.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

                var col = enemyObj.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.5f;

                var sr = enemyObj.AddComponent<SpriteRenderer>();
                sr.sprite = GetFallbackSprite();
                sr.color = Color.red;
            }

            // Enemy 컴포넌트가 없으면 추가
            Enemy enemy = enemyObj.GetComponent<Enemy>();
            if (enemy == null)
                enemy = enemyObj.AddComponent<Enemy>();

            enemy.Initialize(enemyData, _shipGrid);
            _activeEnemies.Add(enemy);
        }

        // 폴백용 1x1 흰색 스프라이트 (한 번만 생성해서 재사용)
        private static Sprite _fallbackSprite;

        /// <summary>
        /// 프리팹 없이 생성된 적에게 적용할 기본 흰색 스프라이트를 반환합니다.
        /// </summary>
        private static Sprite GetFallbackSprite()
        {
            if (_fallbackSprite != null) return _fallbackSprite;

            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _fallbackSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return _fallbackSprite;
        }

        // ────────────────────────────────────────────────
        // 이벤트 핸들러
        // ────────────────────────────────────────────────

        /// <summary>
        /// 적 처치 이벤트를 받아서 카운트를 추적합니다.
        /// 모든 적이 처치되면 OnAllEnemiesDefeated 이벤트를 발행합니다.
        /// </summary>
        private void HandleEnemyDied(Enemy enemy)
        {
            _activeEnemies.Remove(enemy);
            _defeatedCount++;

            if (_defeatedCount >= _totalEnemiesInWave && _spawnCoroutine == null)
            {
                // 모든 스폰이 완료되고 모든 적이 처치됨
                OnAllEnemiesDefeated?.Invoke();
            }
        }

        // ────────────────────────────────────────────────
        // 헬퍼
        // ────────────────────────────────────────────────

        private void DespawnAllEnemies()
        {
            foreach (var enemy in _activeEnemies)
            {
                if (enemy != null && enemy.gameObject != null)
                    Destroy(enemy.gameObject);
            }

            _activeEnemies.Clear();
        }
    }
}
