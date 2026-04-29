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
    ///   2. 설비 슬롯에 드롭 → 검증 통과 시 카드가 슬롯 위치로 이동하고 아이콘만 표시
    ///   3. 설비의 빈 상태 힌트가 사라진다
    ///
    /// 탈착 흐름:
    ///   1. 슬롯 위에 있는 카드를 드래그 → 카드가 원래 레이아웃으로 복원되며 자유 이동
    ///   2. 설비의 빈 상태 힌트가 즉시 표시된다
    ///   3. 빈 곳에 드롭 → 탈착 확정
    ///   4. 다른 설비에 드롭 → 새 슬롯에 장착
    ///
    /// 카드는 항상 Canvas의 직접 자식으로 유지되므로 설비 카드의 레이아웃에 영향을 주지 않는다.
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

        /// <summary>
        /// 드래그 시작.
        /// 장착 상태라면 슬롯에 빈 힌트를 표시하고 카드를 전체 레이아웃으로 복원한다.
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_currentAssignedFacilityView != null)
                _currentAssignedFacilityView.UpdateSpiritDisplay(null);

            _cardView.SetIconOnly(false);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);
            transform.SetAsLastSibling();
        }

        /// <summary>
        /// 드래그 중: 마우스 이동량만큼 카드를 이동한다.
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            _rectTransform.anchoredPosition += eventData.delta / _canvas.scaleFactor;
        }

        /// <summary>
        /// 드래그 종료: 드롭 위치 아래의 설비를 찾아 장착을 시도한다.
        /// 설비가 없는 빈 곳에 드롭하면 탈착을 확정한다.
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            var facilityView = FindFacilityViewAtScreenPoint(eventData.position);
            if (facilityView != null)
                TryAssignToFacility(facilityView);
            else
                UnassignFromCurrentFacility();
        }

        /// <summary>
        /// 대상 설비에 스피릿 장착을 시도한다.
        /// 검증을 통과하면 카드가 슬롯 위치로 이동하고 아이콘만 표시된다.
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
                ReturnToCurrentSlotOrStay();
                return;
            }

            if (facilityData.RequiredSpiritElement != spiritData.Element)
            {
                Debug.LogWarning(
                    $"[SpiritDragHandler] 속성 불일치: {spiritData.Element} → " +
                    $"{facilityData.DisplayName} ({facilityData.RequiredSpiritElement} 필요)");
                ReturnToCurrentSlotOrStay();
                return;
            }

            // 같은 슬롯에 다시 드롭하면 원래 위치로 복귀한다
            if (_currentAssignedFacilityView == facilityView)
            {
                facilityView.UpdateSpiritDisplay(spiritData);
                MoveToSlot(facilityView.SpiritSlotTransform);
                return;
            }

            // 이전 설비에서 그래프 데이터를 해제한다
            if (_currentAssignedFacilityView != null)
            {
                var prevNode = _currentAssignedFacilityView.GetComponent<FacilityNode>();
                prevNode?.GraphNode.UnassignSpirit();
            }

            facilityNode.GraphNode.AssignSpirit(spiritData);
            facilityView.UpdateSpiritDisplay(spiritData);
            _currentAssignedFacilityView = facilityView;

            MoveToSlot(facilityView.SpiritSlotTransform);

            Debug.Log(
                $"[SpiritDragHandler] {spiritData.DisplayName}({spiritData.Element}) → " +
                $"{facilityData.DisplayName} 장착");
        }

        /// <summary>
        /// 빈 곳에 드롭했을 때 탈착을 확정한다.
        /// 그래프에서 스피릿을 제거하고 카드는 드롭 위치에 그대로 머문다.
        /// </summary>
        private void UnassignFromCurrentFacility()
        {
            if (_currentAssignedFacilityView == null) return;

            var facilityNode = _currentAssignedFacilityView.GetComponent<FacilityNode>();
            facilityNode?.GraphNode.UnassignSpirit();

            _currentAssignedFacilityView = null;

            Debug.Log($"[SpiritDragHandler] {_cardView.Data.DisplayName} 탈착");
        }

        /// <summary>
        /// 검증 실패 시 이전 슬롯이 있으면 되돌아가고, 없으면 현재 위치에 머문다.
        /// 그래프 데이터는 OnBeginDrag에서 변경하지 않으므로 UI만 복원한다.
        /// </summary>
        private void ReturnToCurrentSlotOrStay()
        {
            if (_currentAssignedFacilityView == null) return;
            _currentAssignedFacilityView.UpdateSpiritDisplay(_cardView.Data);
            MoveToSlot(_currentAssignedFacilityView.SpiritSlotTransform);
        }

        /// <summary>
        /// 카드를 슬롯의 월드 위치로 이동하고 아이콘만 표시 모드로 전환한다.
        /// 카드는 Canvas의 자식으로 유지되므로 설비 카드 레이아웃에 영향을 주지 않는다.
        /// </summary>
        private void MoveToSlot(Transform slotTransform)
        {
            if (slotTransform == null) return;
            _cardView.SetIconOnly(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);
            _rectTransform.position = slotTransform.position;
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
