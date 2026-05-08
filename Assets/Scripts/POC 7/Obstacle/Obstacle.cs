using UnityEngine;

namespace POC7
{
    /// <summary>
    /// 장애물의 형태를 지정하는 열거형.
    /// </summary>
    public enum ObstacleShape
    {
        Circle,
        Square,
        Triangle
    }

    /// <summary>
    /// 장애물의 형태 구성, 충돌 횟수 관리, 파괴 처리를 담당하는 컴포넌트.
    ///
    /// [씬 설정 주의사항]
    /// 이 컴포넌트가 부착된 프리팹은 반드시 "Obstacle" 레이어에 배치해야 한다.
    /// PlayerController와 AttackPathIndicator의 Obstacle Layer Mask 필드에 해당 레이어를 포함해야
    /// PathCalculator가 경로 계산 시 이 장애물을 감지할 수 있다.
    ///
    /// Rigidbody2D와 Collider2D는 Awake에서 자동으로 추가되므로 미리 부착하지 않아도 된다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Obstacle : MonoBehaviour
    {
        /// <summary>장애물의 형태. Inspector에서 Circle, Square, Triangle 중 선택한다.</summary>
        [SerializeField] private ObstacleShape _shapeType = ObstacleShape.Circle;

        /// <summary>
        /// 파괴되기까지 필요한 플레이어 충돌 횟수.
        /// -1로 설정하면 무적(파괴 불가능) 장애물이 된다.
        /// </summary>
        [SerializeField] private int _maxHits = 3;

        /// <summary>장애물 크기(반지름). 기본값 0.5는 지름 1 유닛에 해당한다.</summary>
        [SerializeField] private float _size = 0.5f;

        private int _remainingHits;
        private SpriteRenderer _spriteRenderer;
        private MeshRenderer _meshRenderer;

        /// <summary>_maxHits가 0 미만이면 무적 상태.</summary>
        public bool IsIndestructible => _maxHits < 0;

        /// <summary>
        /// Rigidbody2D를 Kinematic으로 설정하고, ShapeType에 맞는 Collider2D와 시각 요소를 자동 구성한다.
        /// </summary>
        private void Awake()
        {
            var rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            _remainingHits = _maxHits;

            SetupShape();
            UpdateColor();
        }

        /// <summary>
        /// 플레이어가 이 장애물에 충돌했을 때 PathCalculator가 호출한다.
        /// 무적이면 무시하고, 남은 횟수를 차감하여 0 이하가 되면 비활성화한다.
        /// </summary>
        public void RegisterHit()
        {
            if (IsIndestructible)
                return;

            _remainingHits--;
            UpdateColor();

            if (_remainingHits <= 0)
                gameObject.SetActive(false);
        }

        /// <summary>
        /// ShapeType에 따라 Collider2D와 시각 요소를 구성한다.
        /// </summary>
        private void SetupShape()
        {
            switch (_shapeType)
            {
                case ObstacleShape.Circle:
                    SetupCircle();
                    break;
                case ObstacleShape.Square:
                    SetupSquare();
                    break;
                case ObstacleShape.Triangle:
                    SetupTriangle();
                    break;
            }
        }

        /// <summary>
        /// 원형 장애물: CircleCollider2D와 원형 스프라이트를 추가한다.
        /// </summary>
        private void SetupCircle()
        {
            var col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = _size;

            _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

            // pixelsPerUnit = texSize / worldSize 로 설정해 localScale을 건드리지 않고 크기를 맞춘다.
            const int res = 64;
            _spriteRenderer.sprite = CreateCircleSprite(res, res / (_size * 2f));
        }

        /// <summary>
        /// 사각형 장애물: BoxCollider2D와 사각형 스프라이트를 추가한다.
        /// </summary>
        private void SetupSquare()
        {
            float side = _size * 2f;
            var col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = Vector2.one * side;

            _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            _spriteRenderer.sprite = CreateSquareSprite(side);
        }

        /// <summary>
        /// 삼각형 장애물: PolygonCollider2D와 프로시저럴 메시를 추가한다.
        /// _size를 외접원 반지름으로 하는 정삼각형을 생성한다.
        /// </summary>
        private void SetupTriangle()
        {
            float r = _size;
            float h = r * Mathf.Sqrt(3f) * 0.5f;

            // 정삼각형 꼭짓점 (위, 왼쪽 아래, 오른쪽 아래)
            var points = new Vector2[]
            {
                new Vector2(0f,  r),
                new Vector2(-h, -r * 0.5f),
                new Vector2( h, -r * 0.5f)
            };

            var col = gameObject.AddComponent<PolygonCollider2D>();
            col.isTrigger = true;
            col.SetPath(0, points);

            SetupTriangleMesh(points);
        }

        /// <summary>
        /// 삼각형 꼭짓점으로 MeshFilter + MeshRenderer를 구성한다.
        /// SpriteRenderer는 삼각형 표현이 어려우므로 직접 메시를 생성한다.
        /// Sprites/Default 셰이더는 양면 렌더링을 지원하므로 뒷면 컬링 걱정 없이 사용한다.
        /// </summary>
        private void SetupTriangleMesh(Vector2[] points)
        {
            var mf = gameObject.AddComponent<MeshFilter>();
            _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            _meshRenderer.material = new Material(Shader.Find("Sprites/Default"));

            var mesh = new Mesh();
            mesh.vertices = new Vector3[]
            {
                new Vector3(points[0].x, points[0].y, 0f),
                new Vector3(points[1].x, points[1].y, 0f),
                new Vector3(points[2].x, points[2].y, 0f)
            };
            mesh.triangles = new int[] { 0, 1, 2 };
            mesh.RecalculateNormals();
            mf.mesh = mesh;
        }

        /// <summary>
        /// 남은 충돌 횟수 비율에 따라 색상을 갱신한다.
        /// 무적이면 회색, 남은 횟수가 많을수록 초록, 적을수록 빨강으로 표시한다.
        /// </summary>
        private void UpdateColor()
        {
            Color color;

            if (IsIndestructible)
            {
                color = new Color(0.55f, 0.55f, 0.55f); // 회색 = 무적
            }
            else
            {
                float ratio = _maxHits > 0 ? (float)_remainingHits / _maxHits : 0f;
                color = Color.Lerp(Color.red, Color.green, ratio);
            }

            if (_spriteRenderer != null)
                _spriteRenderer.color = color;

            if (_meshRenderer != null)
                _meshRenderer.material.color = color;
        }

        /// <summary>
        /// 원형 텍스처로 Sprite를 생성한다.
        /// pixelsPerUnit으로 월드 크기를 제어하여 localScale을 변경하지 않는다.
        /// </summary>
        private Sprite CreateCircleSprite(int resolution, float pixelsPerUnit)
        {
            var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            float center = resolution * 0.5f;
            float radius = center - 1f;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    tex.SetPixel(x, y, dist <= radius ? Color.white : Color.clear);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), Vector2.one * 0.5f, pixelsPerUnit);
        }

        /// <summary>
        /// 흰색 사각형 Sprite를 생성한다.
        /// pixelsPerUnit으로 worldSize에 맞는 크기를 설정한다.
        /// </summary>
        private Sprite CreateSquareSprite(float worldSize)
        {
            const int texSize = 4;
            var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            var pixels = new Color[texSize * texSize];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            float ppu = texSize / worldSize;
            return Sprite.Create(tex, new Rect(0, 0, texSize, texSize), Vector2.one * 0.5f, ppu);
        }
    }
}
