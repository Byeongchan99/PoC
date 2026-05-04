using UnityEngine;

namespace POC7
{
    /// <summary>
    /// 런타임에 EdgeCollider2D 점들을 원형으로 배치하여 링 충돌 영역을 생성하는 컴포넌트.
    /// 같은 GameObject에 EdgeCollider2D가 있어야 한다.
    /// </summary>
    [RequireComponent(typeof(EdgeCollider2D))]
    public class RingColliderBuilder : MonoBehaviour
    {
        [SerializeField] private float _innerRadius = 5f;
        [SerializeField] private int _pointCount = 64;

        private EdgeCollider2D _edgeCollider;

        /// <summary>
        /// 컴포넌트 초기화 시 EdgeCollider2D 참조를 가져오고 원형 콜라이더를 생성한다.
        /// </summary>
        private void Awake()
        {
            _edgeCollider = GetComponent<EdgeCollider2D>();
            BuildCircleCollider();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Inspector에서 값이 변경될 때마다 콜라이더를 즉시 갱신하여 Scene 뷰에서 미리볼 수 있게 한다.
        /// </summary>
        private void OnValidate()
        {
            // OnValidate는 Awake보다 먼저 호출될 수 있으므로 null 체크 필요
            if (_edgeCollider == null)
                _edgeCollider = GetComponent<EdgeCollider2D>();

            if (_edgeCollider != null)
                BuildCircleCollider();
        }
#endif

        /// <summary>
        /// pointCount 개의 점을 원형으로 계산하여 EdgeCollider2D에 할당한다.
        /// 시작점과 끝점을 동일하게 설정해 닫힌 원을 만든다.
        /// </summary>
        private void BuildCircleCollider()
        {
            // 닫힌 원을 만들기 위해 마지막 점을 시작점과 같게 하므로 pointCount + 1개 필요
            Vector2[] points = new Vector2[_pointCount + 1];

            for (int i = 0; i < _pointCount; i++)
            {
                // 0 ~ 2PI 범위를 pointCount로 균등 분할
                float angle = 2f * Mathf.PI * i / _pointCount;
                points[i] = new Vector2(
                    Mathf.Cos(angle) * _innerRadius,
                    Mathf.Sin(angle) * _innerRadius
                );
            }

            // 시작점과 끝점을 연결하여 닫힌 원 형태 완성
            points[_pointCount] = points[0];

            _edgeCollider.points = points;
        }
    }
}
