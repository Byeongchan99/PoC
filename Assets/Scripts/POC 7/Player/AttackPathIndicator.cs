using UnityEngine;

namespace POC7
{
    /// <summary>
    /// 마우스 위치를 실시간으로 추적하여, 클릭 시 플레이어가 이동할 반사 경로를 LineRenderer로 시각화한다.
    /// 플레이어가 돌진 중일 때는 인디케이터를 숨긴다.
    ///
    /// [실무 권장]
    /// POC 단계에서는 LineRenderer로 충분하다.
    /// 추후 Shader Graph 기반의 점선 또는 화살표 UV 스크롤 셰이더를 적용하면
    /// 방향성이 명확한 풍부한 표현이 가능하다.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class AttackPathIndicator : MonoBehaviour
    {
        /// <summary>경로를 그릴 PlayerController. 반사 횟수와 링 Transform을 제공한다.</summary>
        [SerializeField] private PlayerController _playerController;

        /// <summary>경로 선의 두께.</summary>
        [SerializeField] private float _lineWidth = 0.05f;

        /// <summary>경로 선의 색상. 알파값도 적용된다.</summary>
        [SerializeField] private Color _lineColor = new Color(1f, 1f, 0f, 0.6f);

        /// <summary>경로의 마지막 지점에 표시할 점의 크기. 0이면 표시하지 않는다.</summary>
        [SerializeField] private float _endpointSize = 0.15f;

        /// <summary>경로 끝 지점을 나타내는 스프라이트 오브젝트. 없으면 자동 생성한다.</summary>
        [SerializeField] private SpriteRenderer _endpointMarker;

        /// <summary>장애물 감지에 사용할 레이어 마스크. PlayerController와 동일한 값을 설정해야 한다.</summary>
        [SerializeField] private LayerMask _obstacleLayerMask;

        private LineRenderer _lineRenderer;
        private Camera _mainCamera;

        /// <summary>
        /// LineRenderer를 초기화하고, 끝 지점 마커가 없으면 자동으로 생성한다.
        /// </summary>
        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.startWidth = _lineWidth;
            _lineRenderer.endWidth = _lineWidth;
            _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _lineRenderer.startColor = _lineColor;
            _lineRenderer.endColor = _lineColor;

            _mainCamera = Camera.main;

            if (_endpointMarker == null)
                _endpointMarker = CreateEndpointMarker();

            // Inspector에서 레이어 마스크를 설정하지 않은 경우 "Obstacle" 레이어를 자동으로 탐색한다.
            if (_obstacleLayerMask.value == 0)
            {
                int layer = LayerMask.NameToLayer("Obstacle");
                if (layer >= 0)
                    _obstacleLayerMask = 1 << layer;
            }
        }

        /// <summary>
        /// 매 프레임 마우스 위치 기준으로 경로를 재계산하여 LineRenderer를 갱신한다.
        /// </summary>
        private void Update()
        {
            // 돌진 중이거나 PlayerController가 없으면 인디케이터를 숨긴다.
            bool shouldShow = _playerController != null && !PlayerController.IsPlayerDashing;
            _lineRenderer.enabled = shouldShow;
            if (_endpointMarker != null)
                _endpointMarker.enabled = shouldShow;

            if (!shouldShow)
                return;

            Vector2 mouseWorldPos = GetMouseWorldPos();
            Vector2 playerPos = _playerController.transform.position;
            Transform ringTransform = _playerController.RingTransform;

            if (!TryGetDashDirection(playerPos, mouseWorldPos, ringTransform, out Vector2 direction))
            {
                _lineRenderer.positionCount = 0;
                if (_endpointMarker != null)
                    _endpointMarker.enabled = false;
                return;
            }

            Vector2 ringCenter = ringTransform != null ? (Vector2)ringTransform.position : Vector2.zero;
            PathCalculator.WaypointInfo[] waypointInfos = PathCalculator.ComputeWaypoints(
                playerPos, direction, ringCenter, _playerController.RingRadius,
                _playerController.BounceCount, _obstacleLayerMask);

            var positions = new Vector2[waypointInfos.Length];
            for (int i = 0; i < waypointInfos.Length; i++)
                positions[i] = waypointInfos[i].Position;

            DrawPath(playerPos, positions);
        }

        /// <summary>
        /// 마우스 스크린 좌표를 월드 좌표로 변환한다.
        /// </summary>
        private Vector2 GetMouseWorldPos()
        {
            Vector3 screenPos = Input.mousePosition;
            screenPos.z = -_mainCamera.transform.position.z;
            return _mainCamera.ScreenToWorldPoint(screenPos);
        }

        /// <summary>
        /// 플레이어 위치(P)에서 마우스 방향(d)으로 나아갈 때 링 원의 반대편 교점 방향을 계산한다.
        /// 클릭 위치가 너무 가깝거나 링 바깥 방향이면 false를 반환한다.
        /// </summary>
        /// <param name="direction">유효한 돌진 방향 단위벡터.</param>
        private bool TryGetDashDirection(Vector2 playerPos, Vector2 mouseWorldPos, Transform ringTransform, out Vector2 direction)
        {
            direction = Vector2.zero;

            Vector2 rawDir = mouseWorldPos - playerPos;
            if (rawDir.sqrMagnitude < 0.0001f)
                return false;

            direction = rawDir.normalized;

            Vector2 ringCenter = ringTransform != null ? (Vector2)ringTransform.position : Vector2.zero;
            float t = 2f * Vector2.Dot(ringCenter - playerPos, direction);

            return t >= 0.1f;
        }

        /// <summary>
        /// 시작점과 경유 지점 목록을 LineRenderer에 적용하고, 끝 지점 마커를 마지막 위치에 배치한다.
        /// </summary>
        private void DrawPath(Vector2 startPos, Vector2[] waypoints)
        {
            int pointCount = waypoints.Length + 1;
            _lineRenderer.positionCount = pointCount;
            _lineRenderer.SetPosition(0, startPos);

            for (int i = 0; i < waypoints.Length; i++)
                _lineRenderer.SetPosition(i + 1, waypoints[i]);

            if (_endpointMarker != null)
            {
                _endpointMarker.enabled = _endpointSize > 0f;
                _endpointMarker.transform.position = waypoints[waypoints.Length - 1];
            }
        }

        /// <summary>
        /// 끝 지점 마커로 사용할 원형 스프라이트 GameObject를 동적으로 생성한다.
        /// </summary>
        private SpriteRenderer CreateEndpointMarker()
        {
            var markerObj = new GameObject("AttackPathEndpoint");
            var sr = markerObj.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite();
            sr.color = _lineColor;
            markerObj.transform.localScale = Vector3.one * _endpointSize;
            return sr;
        }

        /// <summary>
        /// 단색 원형 텍스처를 생성하여 Sprite로 반환한다.
        /// </summary>
        private Sprite CreateCircleSprite()
        {
            const int texSize = 64;
            var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            float center = texSize / 2f;
            float radius = center - 1f;

            for (int y = 0; y < texSize; y++)
            {
                for (int x = 0; x < texSize; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    tex.SetPixel(x, y, dist <= radius ? Color.white : Color.clear);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, texSize, texSize), Vector2.one * 0.5f);
        }
    }
}
