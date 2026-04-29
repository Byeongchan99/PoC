using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using POC5.Runtime;

namespace POC5.UI
{
    /// <summary>
    /// 스피릿 카드를 설비 슬롯에 드래그-드롭으로 장착/탈착하는 컴포넌트.
    ///
    /// 장착 흐름:
    ///   1. 스피릿 카드를 드래그 → Canvas 위에서 자유롭게 이동
    ///   2. 설비 슬롯에 드롭 → 검증 통과 시 카드가 비활성화되고, 슬롯에 정령 아이콘이 표시된다
    ///
    /// 탈착 흐름:
    ///   1. 설비 슬롯의 아이콘을 드래그 → SpiritSlotDragSource가 이 핸들러를 재활성화한다
    ///   2. NotifyExtractedFromSlot()이 호출되어 그래프 데이터와 슬롯 시각이 초기화된다
    ///   3. SpiritSlotDragSource가 드래그를 제어하고, 드롭 시 HandleDrop()을 호출한다
    ///
    /// 카드는 항상 Canvas의 직접 자식으로 유지되므로 설비 카드 레이아웃에 영향을 주지 않는다.
    /// </summary>
    public class SpiritDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private SpiritCardView _cardView;
        private RectTransform _rectTransform;
        private Canvas _canvas;
        private GraphicRaycaster _raycaster;

        // 현재 장착된 설비 뷰. null이면 미장착 상태.
        private FacilityNodeView _currentAssignedFacilityView;

        private void Awake()
        {
            _cardView      = GetComponent<SpiritCardView>();
            _rectTransform = GetComponent<RectTransform>();
            _canvas        = GetComponentInParent<Canvas>();
            _raycaster     = _canvas.GetComponent<GraphicRaycaster>();
        }

        /// <summary>드래그 시작: 카드를 최상위 레이어로 올린다.</summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            transform.SetAsLastSibling();
        }

        /// <summary>드래그 중: 마우스 이동량만큼 카드를 이동한다.</summary>
        public void OnDrag(PointerEventData eventData)
        {
            _rectTransform.anchoredPosition += eventData.delta / _canvas.scaleFactor;
        }

        /// <summary>
        /// 드래그 종료: 드롭 위치 아래의 설비를 찾아 장착을 시도한다.
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            HandleDrop(eventData.position);
        }

        /// <summary>
        /// SpiritSlotDragSource가 슬롯에서 카드를 꺼낼 때 호출한다.
        /// 그래프 데이터를 해제하고 설비 슬롯 시각을 빈 상태로 되돌린다.
        /// </summary>
        public void NotifyExtractedFromSlot()
        {
            if (_currentAssignedFacilityView == null) return;

            var facilityNode = _currentAssignedFacilityView.GetComponent<FacilityNode>();
            facilityNode?.GraphNode.UnassignSpirit();
            _currentAssignedFacilityView.UpdateSpiritDisplay(null);

            Debug.Log($"[SpiritDragHandler] {_cardView.Data.DisplayName} 슬롯 추출");
            _currentAssignedFacilityView = null;
        }

        /// <summary>
        /// 주어진 화면 좌표에 드롭했을 때의 처리.
        /// SpiritSlotDragSource와 OnEndDrag 모두 이 메서드를 공유한다.
        /// </summary>
        public void HandleDrop(Vector2 screenPosition)
        {
            var facilityView = FindFacilityViewAtScreenPoint(screenPosition);
            if (facilityView != null)
                TryAssignToFacility(facilityView);
            else
                UnassignFromCurrentFacility();
        }

        /// <summary>
        /// 대상 설비에 스피릿 장착을 시도한다.
        /// 검증을 통과하면 카드가 비활성화되고 슬롯에 정령 아이콘이 표시된다.
        /// </summary>
        private void TryAssignToFacility(FacilityNodeView facilityView)
        {
            var spiritData   = _cardView.Data;
            var facilityNode = facilityView.GetComponent<FacilityNode>();
            if (facilityNode == null) return;

            var facilityData = facilityNode.GraphNode.Data;

            if (!facilityData.RequiresSpirit)
            {
                Debug.LogWarning(
                    $"[SpiritDragHandler] {facilityData.DisplayName}은(는) 스피릿이 필요 없는 설비입니다.");
                return;
            }

            if (facilityData.RequiredSpiritElement != spiritData.Element)
            {
                Debug.LogWarning(
                    $"[SpiritDragHandler] 속성 불일치: {spiritData.Element} → " +
                    $"{facilityData.DisplayName} ({facilityData.RequiredSpiritElement} 필요)");
                return;
            }

            // 이전 설비에서 그래프 데이터와 슬롯 등록을 해제한다
            if (_currentAssignedFacilityView != null)
            {
                var prevNode    = _currentAssignedFacilityView.GetComponent<FacilityNode>();
                var prevSlotSrc = _currentAssignedFacilityView.GetComponentInChildren<SpiritSlotDragSource>();
                prevNode?.GraphNode.UnassignSpirit();
                prevSlotSrc?.SetAssignedHandler(null);
                _currentAssignedFacilityView.UpdateSpiritDisplay(null);
            }

            facilityNode.GraphNode.AssignSpirit(spiritData);
            facilityView.UpdateSpiritDisplay(spiritData);
            _currentAssignedFacilityView = facilityView;

            // 슬롯 드래그 소스에 이 핸들러를 등록하고 카드를 숨긴다
            var slotSource = facilityView.GetComponentInChildren<SpiritSlotDragSource>();
            slotSource?.SetAssignedHandler(this);
            gameObject.SetActive(false);

            Debug.Log(
                $"[SpiritDragHandler] {spiritData.DisplayName}({spiritData.Element}) → " +
                $"{facilityData.DisplayName} 장착");
        }

        /// <summary>빈 곳에 드롭했을 때 탈착을 확정한다.</summary>
        private void UnassignFromCurrentFacility()
        {
            if (_currentAssignedFacilityView == null) return;

            var facilityNode = _currentAssignedFacilityView.GetComponent<FacilityNode>();
            var slotSource   = _currentAssignedFacilityView.GetComponentInChildren<SpiritSlotDragSource>();
            facilityNode?.GraphNode.UnassignSpirit();
            slotSource?.SetAssignedHandler(null);
            _currentAssignedFacilityView.UpdateSpiritDisplay(null);
            _currentAssignedFacilityView = null;

            Debug.Log($"[SpiritDragHandler] {_cardView.Data.DisplayName} 탈착");
        }

        /// <summary>
        /// 화면 좌표 아래의 FacilityNodeView를 반환한다.
        /// 이 스피릿 카드 자신(및 자식들)은 결과에서 제외한다.
        /// </summary>
        private FacilityNodeView FindFacilityViewAtScreenPoint(Vector2 screenPoint)
        {
            var results   = new List<RaycastResult>();
            var eventData = new PointerEventData(EventSystem.current) { position = screenPoint };
            _raycaster.Raycast(eventData, results);

            foreach (var result in results)
            {
                if (result.gameObject.transform.IsChildOf(transform)) continue;
                var fv = result.gameObject.GetComponentInParent<FacilityNodeView>();
                if (fv != null) return fv;
            }
            return null;
        }
    }
}
