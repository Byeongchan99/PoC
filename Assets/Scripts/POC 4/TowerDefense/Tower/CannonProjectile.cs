using UnityEngine;

namespace POC4
{
    /// <summary>
    /// 포탄 타워에서 발사되는 투사체.
    /// 타겟을 향해 이동하다가 도달 시 폭발 반경 내 모든 적에게 피해와 효과를 적용한다.
    /// 단일 타겟 Projectile과 달리 범위 피해(AoE)를 처리한다.
    /// </summary>
    public class CannonProjectile : MonoBehaviour
    {
        // -------------------------------------------------------
        // 내부 상태
        // -------------------------------------------------------

        private Enemy _target;
        private float _damage;
        private float _speed;
        private float _explosionRadius;

        private TowerData.TowerEffectType _effectType;
        private float _extraDamage;
        private float _slowRatio;
        private float _slowDuration;
        private float _stunDuration;

        // 타겟과의 도달 판정 기준 거리 (월드 단위)
        private const float ArrivalThreshold = 0.2f;

        // -------------------------------------------------------
        // 초기화
        // -------------------------------------------------------

        /// <summary>
        /// 투사체를 초기화한다. CannonTower의 Attack()에서 Instantiate 직후 호출.
        /// </summary>
        public void Initialize(Enemy target, float damage, float speed, float explosionRadius,
                               TowerData.TowerEffectType effectType = TowerData.TowerEffectType.None,
                               float extraDamage = 0f, float slowRatio = 0f,
                               float slowDuration = 0f, float stunDuration = 0f)
        {
            _target = target;
            _damage = damage;
            _speed = speed;
            _explosionRadius = explosionRadius;
            _effectType = effectType;
            _extraDamage = extraDamage;
            _slowRatio = slowRatio;
            _slowDuration = slowDuration;
            _stunDuration = stunDuration;

            CreateVisual();
        }

        // -------------------------------------------------------
        // 유니티 생명주기
        // -------------------------------------------------------

        private void Update()
        {
            // 타겟이 사망했거나 오브젝트가 제거된 경우 투사체도 제거
            if (_target == null || !_target.IsAlive)
            {
                Destroy(gameObject);
                return;
            }

            MoveTowardTarget();
            CheckArrival();
        }

        // -------------------------------------------------------
        // 이동
        // -------------------------------------------------------

        /// <summary>
        /// 타겟의 현재 위치를 향해 이동한다.
        /// </summary>
        private void MoveTowardTarget()
        {
            Vector3 targetPos = _target.transform.position;
            targetPos.z = transform.position.z;

            Vector3 direction = (targetPos - transform.position).normalized;
            transform.position += direction * _speed * Time.deltaTime;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        /// <summary>
        /// 타겟과의 거리가 도달 기준 이하이면 폭발을 실행한다.
        /// </summary>
        private void CheckArrival()
        {
            float dist = Vector3.Distance(transform.position, _target.transform.position);
            if (dist > ArrivalThreshold) return;

            ApplyAreaDamage();
            Destroy(gameObject);
        }

        // -------------------------------------------------------
        // 범위 피해 적용
        // -------------------------------------------------------

        /// <summary>
        /// 폭발 위치(_explosionRadius) 내 살아있는 모든 적에게 피해와 효과를 적용한다.
        /// </summary>
        private void ApplyAreaDamage()
        {
            float totalDamage = _damage;
            if (_effectType == TowerData.TowerEffectType.ExtraDamage)
                totalDamage += _extraDamage;

            Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);

            foreach (Enemy enemy in allEnemies)
            {
                if (!enemy.IsAlive) continue;

                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist > _explosionRadius) continue;

                enemy.TakeDamage(totalDamage);

                if (_effectType == TowerData.TowerEffectType.Slow)
                    enemy.ApplySlow(_slowRatio, _slowDuration);

                if (_effectType == TowerData.TowerEffectType.Stun)
                    enemy.ApplyStun(_stunDuration);
            }
        }

        // -------------------------------------------------------
        // 시각적 표현
        // -------------------------------------------------------

        /// <summary>
        /// 포탄을 나타내는 주황색 원형 스프라이트를 코드로 생성한다.
        /// </summary>
        private void CreateVisual()
        {
            SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite();
            sr.color = new Color(1f, 0.5f, 0.1f, 1f); // 주황색
            sr.sortingOrder = 5;
            transform.localScale = new Vector3(0.35f, 0.35f, 1f);
        }

        /// <summary>
        /// 8×8 픽셀 흰색 스프라이트를 생성한다 (원형으로 표현).
        /// </summary>
        private Sprite CreateCircleSprite()
        {
            const int size = 8;
            Texture2D tex = new Texture2D(size, size) { filterMode = FilterMode.Point };
            Color[] pixels = new Color[size * size];

            float center = size * 0.5f - 0.5f;
            float radius = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    // 원 내부만 흰색, 외부는 투명
                    pixels[y * size + x] = (dx * dx + dy * dy <= radius * radius)
                        ? Color.white
                        : Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
