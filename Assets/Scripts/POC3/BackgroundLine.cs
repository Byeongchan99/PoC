using UnityEngine;

namespace POC3
{
    /// <summary>
    /// 원점에서 특정 방향으로 뻗어나가는 직선 하나를 표현합니다.
    /// LineRenderer의 두 점이 같은 속도로 바깥쪽으로 이동하여
    /// 고정 길이의 대시(dash)가 바깥으로 날아가는 효과를 냅니다.
    /// startDist가 maxDistance에 도달하면 자동으로 오브젝트를 파괴합니다.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class BackgroundLine : MonoBehaviour
    {
        // 이동 방향 (단위 벡터)
        Vector2 _direction;

        // 선의 꼬리(startPoint)가 원점에서 떨어진 거리
        float _startDist;

        // 선의 고정 길이 (endDist = startDist + lineLength)
        float _lineLength;

        // 선이 이동하는 속도 (초당 거리)
        float _lineSpeed;

        // 이 거리에 도달하면 완전히 투명해지고 파괴됨
        float _maxDistance;

        LineRenderer _lineRenderer;

        // 선의 초기 색상 (알파값 포함)
        Color _baseColor;

        /// <summary>
        /// BackgroundLineSpawner가 생성 직후 호출하여 초기 매개변수를 설정합니다.
        /// </summary>
        /// <param name="angleDeg">원점 기준 방향 각도 (도)</param>
        /// <param name="startDist">초기 꼬리 거리 (프리로드 시 0보다 클 수 있음)</param>
        /// <param name="lineLength">대시 길이</param>
        /// <param name="lineSpeed">바깥 이동 속도</param>
        /// <param name="maxDistance">파괴 기준 최대 거리</param>
        /// <param name="lineWidth">LineRenderer 너비</param>
        /// <param name="lineColor">선 색상 (알파는 fade 기준으로 사용)</param>
        public void Initialize(float angleDeg, float startDist, float lineLength,
                               float lineSpeed, float maxDistance,
                               float lineWidth, Color lineColor)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            _direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            _startDist  = startDist;
            _lineLength = lineLength;
            _lineSpeed  = lineSpeed;
            _maxDistance = maxDistance;
            _baseColor  = lineColor;

            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.positionCount = 2;
            _lineRenderer.startWidth    = lineWidth;
            _lineRenderer.endWidth      = lineWidth;
            _lineRenderer.useWorldSpace = true;

            UpdateLineRenderer();
        }

        void Update()
        {
            _startDist += _lineSpeed * Time.deltaTime;

            if (_startDist >= _maxDistance)
            {
                Destroy(gameObject);
                return;
            }

            UpdateLineRenderer();
        }

        /// <summary>
        /// LineRenderer의 두 점 위치와 알파값을 현재 거리에 맞게 갱신합니다.
        /// 거리가 maxDistance에 가까울수록 선이 투명해집니다.
        /// </summary>
        void UpdateLineRenderer()
        {
            float endDist = _startDist + _lineLength;

            Vector3 startPos = (Vector3)(_direction * _startDist);
            Vector3 endPos   = (Vector3)(_direction * endDist);

            _lineRenderer.SetPosition(0, startPos);
            _lineRenderer.SetPosition(1, endPos);

            // 꼬리가 maxDistance에 가까울수록 알파 감소
            float alpha = Mathf.Clamp01(1f - _startDist / _maxDistance);
            Color c = _baseColor;
            c.a = alpha * _baseColor.a;
            _lineRenderer.startColor = c;
            _lineRenderer.endColor   = c;
        }
    }
}
