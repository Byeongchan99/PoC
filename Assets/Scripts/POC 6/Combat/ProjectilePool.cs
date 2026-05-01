using System.Collections.Generic;
using UnityEngine;

namespace POC6
{
    /// <summary>
    /// Projectile의 Object Pool입니다.
    /// 발사체를 매번 생성/파괴하는 대신 미리 생성해두고 재사용해서 GC 부하를 줄입니다.
    /// </summary>
    public class ProjectilePool : MonoBehaviour
    {
        public static ProjectilePool Instance { get; private set; }

        [Header("풀 설정")]
        [Tooltip("풀에 미리 생성해둘 발사체 수")]
        [SerializeField] private int _initialSize = 30;

        [Tooltip("발사체 프리팹. Projectile 컴포넌트가 붙어있어야 합니다.")]
        [SerializeField] private Projectile _projectilePrefab;

        private Queue<Projectile> _pool = new();

        private void Awake()
        {
            // 씬 단위 싱글톤 (DontDestroyOnLoad 사용 안 함)
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            // 풀 초기화: 미리 지정한 수만큼 발사체 생성
            for (int i = 0; i < _initialSize; i++)
            {
                Projectile projectile = CreateNewProjectile();
                _pool.Enqueue(projectile);
            }
        }

        /// <summary>
        /// 풀에서 발사체를 가져와서 발사합니다.
        /// 풀이 비어있으면 새로 생성합니다.
        /// </summary>
        public Projectile Get(Vector3 position, Vector2 direction, float speed, float damage,
            float maxRange, int pierceCount, string ownerTag)
        {
            Projectile projectile = _pool.Count > 0 ? _pool.Dequeue() : CreateNewProjectile();

            projectile.Launch(position, direction, speed, damage, maxRange, pierceCount, ownerTag, Return);
            return projectile;
        }

        /// <summary>
        /// 사용이 끝난 발사체를 Pool에 반환합니다. Projectile에서 자동으로 호출됩니다.
        /// </summary>
        public void Return(Projectile projectile)
        {
            projectile.gameObject.SetActive(false);
            projectile.transform.SetParent(transform);
            _pool.Enqueue(projectile);
        }

        /// <summary>
        /// 새 발사체 인스턴스를 생성하고 비활성 상태로 Pool 부모 아래 둡니다.
        /// </summary>
        private Projectile CreateNewProjectile()
        {
            Projectile p;

            if (_projectilePrefab != null)
            {
                p = Instantiate(_projectilePrefab, transform);
            }
            else
            {
                // 프리팹 없을 때 기본 오브젝트 생성
                var go = new GameObject("Projectile");
                go.transform.SetParent(transform);
                go.AddComponent<Rigidbody2D>();

                var col = go.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.15f;

                var sr = go.AddComponent<SpriteRenderer>();
                sr.color = Color.yellow;

                go.transform.localScale = Vector3.one * 0.3f;
                p = go.AddComponent<Projectile>();
            }

            p.gameObject.SetActive(false);
            return p;
        }
    }
}
