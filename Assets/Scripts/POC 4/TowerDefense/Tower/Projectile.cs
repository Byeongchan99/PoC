using UnityEngine;

namespace POC4
{
    /// <summary>
    /// 타워에서 발사된 투사체.
    /// 타겟 적을 향해 이동하고, 도달 시 피해와 효과를 적용한 뒤 자신을 제거한다.
    /// 타겟이 사망하거나 사라지면 즉시 자신도 제거한다.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        // -------------------------------------------------------
        // 내부 상태
        // -------------------------------------------------------

        private Enemy _target;
        private float _damage;
        private float _speed;

        // 효과 관련 (6단계에서 본격 사용)
        private TowerData.TowerEffectType _effectType;
        private float _extraDamage;
        private float _slowRatio;
        private float _slowDuration;
        private float _stunDuration;

        // 도달 판정 기준 거리 (월드 단위)
        private const float ArrivalThreshold = 0.15f;

        // -------------------------------------------------------
        // 초기화
        // -------------------------------------------------------

        /// <summary>
        /// 투사체를 초기화한다. ArrowTower의 Attack()에서 Instantiate 직후 호출.
        /// </summary>
        public void Initialize(Enemy target, float damage, float speed,
                               TowerData.TowerEffectType effectType = TowerData.TowerEffectType.None,
                               float extraDamage = 0f, float slowRatio = 0f,
                               float slowDuration = 0f, float stunDuration = 0f)
        {
            _target = target;
            _damage = damage;
            _speed = speed;
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
        /// 타겟의 현재 위치를 향해 일정 속도로 이동한다 (추적 방식).
        /// </summary>
        private void MoveTowardTarget()
        {
            Vector3 targetPos = _target.transform.position;
            targetPos.z = transform.position.z; // 2D이므로 Z 축 고정

            Vector3 direction = (targetPos - transform.position).normalized;
            transform.position += direction * _speed * Time.deltaTime;

            // 투사체가 이동 방향을 바라보도록 회전 (시각적 연출)
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        /// <summary>
        /// 타겟과의 거리가 도달 기준 이하이면 피해와 효과를 적용하고 제거한다.
        /// </summary>
        private void CheckArrival()
        {
            float dist = Vector3.Distance(transform.position, _target.transform.position);
            if (dist > ArrivalThreshold) return;

            ApplyDamageAndEffect();
            Destroy(gameObject);
        }

        // -------------------------------------------------------
        // 피해 및 효과 적용
        // -------------------------------------------------------

        /// <summary>
        /// 타겟에게 피해를 입히고 타워 효과를 적용한다.
        /// 효과 상세 구현은 6단계에서 완성.
        /// </summary>
        private void ApplyDamageAndEffect()
        {
            float totalDamage = _damage;

            // 추가 피해 효과 (6단계)
            if (_effectType == TowerData.TowerEffectType.ExtraDamage)
            {
                totalDamage += _extraDamage;
            }

            _target.TakeDamage(totalDamage);

            // 슬로우 효과 (6단계)
            if (_effectType == TowerData.TowerEffectType.Slow)
            {
                _target.ApplySlow(_slowRatio, _slowDuration);
            }

            // 기절 효과 (6단계)
            if (_effectType == TowerData.TowerEffectType.Stun)
            {
                _target.ApplyStun(_stunDuration);
            }
        }

        // -------------------------------------------------------
        // 시각적 표현
        // -------------------------------------------------------

        /// <summary>
        /// 투사체의 SpriteRenderer를 코드로 생성한다.
        /// 노란색 작은 마름모꼴(회전한 정사각형)로 화살을 표현한다.
        /// </summary>
        private void CreateVisual()
        {
            SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = CreateArrowSprite();
            sr.color = new Color(1f, 0.9f, 0.2f, 1f); // 노란색
            sr.sortingOrder = 5;

            // 마름모꼴 표현: 45도 회전한 작은 사각형
            transform.localScale = new Vector3(0.2f, 0.2f, 1f);
        }

        /// <summary>
        /// 4×4 픽셀 흰색 스프라이트를 생성한다.
        /// </summary>
        private Sprite CreateArrowSprite()
        {
            const int size = 4;
            Texture2D tex = new Texture2D(size, size) { filterMode = FilterMode.Point };
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
