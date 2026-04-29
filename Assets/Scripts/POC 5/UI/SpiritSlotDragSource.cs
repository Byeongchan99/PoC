using UnityEngine;
using UnityEngine.EventSystems;

namespace POC5.UI
{
    /// <summary>
    /// 설비의 정령 슬롯 패널에 붙어, 장착된 정령 카드를 꺼낼 수 있게 하는 드래그 소스.
    ///
    /// 동작 흐름:
    ///   1. 슬롯에 표시된 정령 아이콘을 드래그하면 OnBeginDrag가 호출된다.
    ///   2. 숨겨진 정령 카드(SpiritDragHandler)를 슬롯 위치에 재배치하고 활성화한다.
    ///   3. OnDrag 동안 정령 카드의 RectTransform을 마우스 이동량만큼 이동시킨다.
    ///   4. 드롭 시 SpiritDragHandler.HandleDrop()을 호출해 설비에 재장착하거나 탈착을 확정한다.
    ///
    /// 이 컴포넌트는 FacilityNodeView.InitializeSpiritSlot()에서 자동으로 추가된다.
    /// 프리팹에서 수동으로 붙여도 된다.
    /// </summary>
    public class SpiritSlotDragSource : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private FacilityNodeView _facilityView;
        private Canvas _canvas;
        private RectTransform _slotRectTransform;

        // 현재 슬롯에 등록된 정령 카드 핸들러
        private SpiritDragHandler _assignedHandler;
        // 드래그 중 이동시킬 정령 카드의 RectTransform
        private RectTransform _draggingRect;

        private void Awake()
        {
            _facilityView      = GetComponentInParent<FacilityNodeView>();
            _canvas            = GetComponentInParent<Canvas>();
            _slotRectTransform = GetComponent<RectTransform>();
        }

        /// <summary>
        /// 슬롯에 장착된 정령 카드 핸들러를 등록한다.
        /// SpiritDragHandler가 장착 완료 시 호출하고, 탈착 시 null을 전달해 해제한다.
        /// </summary>
        public void SetAssignedHandler(SpiritDragHandler handler)
        {
            _assignedHandler = handler;
        }

        /// <summary>
        /// 드래그 시작: 슬롯에 등록된 정령 카드를 슬롯 위치에서 꺼낸다.
        /// 등록된 카드가 없으면 아무 동작도 하지 않는다.
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_assignedHandler == null) return;

            // 정령 카드를 현재 슬롯 위치에 재배치하고 활성화한다
            _draggingRect = _assignedHandler.GetComponent<RectTransform>();
            _draggingRect.position = _slotRectTransform.position;
            _assignedHandler.gameObject.SetActive(true);
            _assignedHandler.transform.SetAsLastSibling();

            // 그래프 데이터 해제 및 슬롯 시각 초기화 (빈 상태 힌트 표시)
            _assignedHandler.NotifyExtractedFromSlot();

            _assignedHandler = null;
        }

        /// <summary>드래그 중: 정령 카드를 마우스 이동량만큼 이동시킨다.</summary>
        public void OnDrag(PointerEventData eventData)
        {
            if (_draggingRect == null) return;
            _draggingRect.anchoredPosition += eventData.delta / _canvas.scaleFactor;
        }

        /// <summary>
        /// 드래그 종료: 정령 카드의 드롭 처리를 SpiritDragHandler에 위임한다.
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            if (_draggingRect == null) return;
            var handler = _draggingRect.GetComponent<SpiritDragHandler>();
            handler?.HandleDrop(eventData.position);
            _draggingRect = null;
        }
    }
}
