using UnityEngine;
using UnityEngine.EventSystems;

namespace POC5.UI
{
    /// <summary>
    /// 카드를 마우스 드래그로 이동시키는 컴포넌트.
    /// IBeginDragHandler, IDragHandler, IEndDragHandler를 구현해
    /// Unity EventSystem의 드래그 이벤트를 처리한다.
    ///
    /// Unity EventSystem 동작 규칙:
    ///   포트 원형 버튼(Button 컴포넌트) 위에서 드래그하면
    ///   버튼이 포인터 이벤트를 소비하므로 카드 드래그가 발생하지 않는다.
    ///   카드 배경(포트 버튼 이외 영역)을 드래그할 때만 카드가 이동한다.
    /// </summary>
    public class NodeDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private RectTransform _rectTransform;
        private Canvas _canvas;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            // anchoredPosition 이동량 계산에 필요한 Canvas를 상위에서 찾는다
            _canvas = GetComponentInParent<Canvas>();

            if (_canvas == null)
                Debug.LogWarning($"[NodeDragHandler] {name}: 상위에서 Canvas를 찾지 못했습니다.");
        }

        /// <summary>
        /// 드래그 시작 시 호출된다.
        /// 현재 단계에서는 별도 처리가 없다.
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData) { }

        /// <summary>
        /// 드래그 중 매 프레임 호출된다.
        /// eventData.delta는 화면 픽셀 단위이므로 Canvas scaleFactor로 나눠
        /// Canvas 좌표 단위로 변환한 뒤 anchoredPosition에 더한다.
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            if (_canvas == null) return;
            _rectTransform.anchoredPosition += eventData.delta / _canvas.scaleFactor;
        }

        /// <summary>
        /// 드래그 종료 시 호출된다.
        /// 현재 단계에서는 별도 처리가 없다.
        /// </summary>
        public void OnEndDrag(PointerEventData eventData) { }
    }
}
