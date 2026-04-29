using UnityEngine;
using UnityEngine.UI;

namespace POC5.UI
{
    /// <summary>
    /// 두 세계 좌표 사이에 직선을 렌더링하는 커스텀 UI 컴포넌트.
    /// MaskableGraphic을 상속해 uGUI Canvas 위에서 동작하므로 외부 패키지가 필요 없다.
    ///
    /// 사용법:
    ///   ConnectionLayer(Canvas 자식, 전체 화면 스트레치) 아래에 배치한다.
    ///   SetWorldPoints()로 시작·끝 세계 좌표를 전달하면 선이 그려진다.
    ///
    /// 실무 팁: 복잡한 곡선(베지어)이 필요하면 OnPopulateMesh에서
    ///   선분을 여러 개로 분할해 이어 붙이는 방식으로 확장할 수 있다.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class ConnectionLineView : MaskableGraphic
    {
        [Tooltip("선의 굵기 (픽셀).")]
        [SerializeField] private float _lineWidth = 3f;

        private RectTransform _rectTransform;
        private Vector2 _startLocal;
        private Vector2 _endLocal;

        /// <summary>선의 굵기. 변경 시 메시를 자동으로 다시 생성한다.</summary>
        public float LineWidth
        {
            get => _lineWidth;
            set { _lineWidth = value; SetVerticesDirty(); }
        }

        protected override void Awake()
        {
            base.Awake();
            _rectTransform = GetComponent<RectTransform>();
            // 연결선이 포트 버튼이나 카드 클릭을 가로채지 않도록 레이캐스트 대상에서 제외한다
            raycastTarget = false;
        }

        /// <summary>
        /// 세계 좌표를 이 RectTransform의 로컬 좌표로 변환한 뒤 선을 업데이트한다.
        /// ConnectionLayer가 Canvas를 꽉 채우는 경우 로컬 좌표 = Canvas 좌표이다.
        /// </summary>
        public void SetWorldPoints(Vector3 worldStart, Vector3 worldEnd)
        {
            _startLocal = _rectTransform.InverseTransformPoint(worldStart);
            _endLocal   = _rectTransform.InverseTransformPoint(worldEnd);
            SetVerticesDirty();
        }

        /// <summary>
        /// 두 점 사이에 사각형 메시(가로로 눕힌 직사각형)를 생성해 선처럼 보이게 한다.
        /// Canvas 렌더링 파이프라인이 이 메서드를 호출해 메시를 채운다.
        /// </summary>
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Vector2 diff = _endLocal - _startLocal;
            // 두 점이 거의 같으면 선을 그리지 않는다
            if (diff.sqrMagnitude < 0.01f) return;

            // 선 방향에 수직인 벡터(두께 방향)를 계산한다
            Vector2 dir  = diff.normalized;
            Vector2 perp = new Vector2(-dir.y, dir.x) * (_lineWidth * 0.5f);

            UIVertex vert = UIVertex.simpleVert;
            vert.color = color;

            // 사각형의 4 꼭짓점: 시작 양끝 2개 + 끝 양끝 2개
            vert.position = _startLocal - perp; vh.AddVert(vert);
            vert.position = _startLocal + perp; vh.AddVert(vert);
            vert.position = _endLocal   + perp; vh.AddVert(vert);
            vert.position = _endLocal   - perp; vh.AddVert(vert);

            // 삼각형 2개로 사각형을 구성한다
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(0, 2, 3);
        }
    }
}
