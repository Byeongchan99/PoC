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
    ///   2. 설비 슬롯에 드롭 → 검증 통과 시 슬롯 위치로 이동, 아이콘만 표시, 크기를 슬롯에 맞춤
    ///   3. 설비가 이동하면 LateUpdate에서 카드 위치를 슬롯에 동기화한다
    ///
    /// 탈착 흐름:
    ///   1. 장착된 카드를 드래그 → LateUpdate 추적 중단, 카드 원래 레이아웃 복원
    ///   2. 설비의 빈 상태 힌트가 즉시 표시된다
    ///   3. 빈 곳에 드롭 → 탈착 확정, 다른 설비에 드롭 → 새 슬롯에 장착
    ///
    /// 카드는 항상 Canvas의 직접 자식으로 유지되므로 설비 카드 레이아웃에 영향을 주지 않는다.
    /// </summary>
    public class SpiritDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private SpiritCardView _cardView;
        private RectTransform _rectTransform;
        private Canvas _canvas;
        private GraphicRaycaster _raycaster;
        private ContentSizeFitter _contentSizeFitter;

        // 현재 장착된 설비 뷰. null이면 미장착 상태.
        private FacilityNodeView _currentAssignedFacilityView;

        // 슬롯에 고정(도킹)된 상태. 드래그 중에는 false가 되어 LateUpdate 추적을 멈춘다.
        private bool _isDocked = false;

        private void Awake()
        {
            _cardView          = GetComponent<SpiritCardView>();
            _rectTransform     = GetComponent<RectTransform>();
            _canvas            = GetComponentInParent<Canvas>();
            _raycaster         = _canvas.GetComponent<GraphicRaycaster>();
            _contentSizeFitter = GetComponent<ContentSizeFitter>();
        }

        /// <summary>
        /// 도킹 중일 때 슬롯의 월드 위치를 매 프레임 추적한다.
        /// NodeDragHandler가 Update에서 설비를 이동시키므로 LateUpdate에서 동기화해야
        /// 같은 프레임 안에서 1프레임 지연 없이 따라갈 수 있다.
        /// </summary>
        private void LateUpdate()
        {
            if (!_isDocked || _currentAssignedFacilityView == null) return;

            var slotTransform = _currentAssignedFacilityView.SpiritSlotTransform;
            if (slotTransform != null)
                _rectTransform.position = slotTransform.position;
        }

        /// <summary>
        /// 드래그 시작.
        /// 도킹 상태라면 LateUpdate 추적을 멈추고 카드를 원래 레이아웃으로 복원한다.
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_currentAssignedFacilityView != null)
            {
                _currentAssignedFacilityView.UpdateSpiritDisplay(null);
                Undock();
            }
            transform.SetAsLastSibling();
        }

        /// <summary>드래그 중: 마우스 이동량만큼 카드를 이동한다.</summary>
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
        /// 검증을 통과하면 카드가 슬롯에 도킹된다.
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
                Dock(facilityView);
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

            Dock(facilityView);

            Debug.Log(
                $"[SpiritDragHandler] {spiritData.DisplayName}({spiritData.Element}) → " +
                $"{facilityData.DisplayName} 장착");
        }

        /// <summary>빈 곳에 드롭했을 때 탈착을 확정한다.</summary>
        private void UnassignFromCurrentFacility()
        {
            if (_currentAssignedFacilityView == null) return;

            var facilityNode = _currentAssignedFacilityView.GetComponent<FacilityNode>();
            facilityNode?.GraphNode.UnassignSpirit();

            _currentAssignedFacilityView = null;

            Debug.Log($"[SpiritDragHandler] {_cardView.Data.DisplayName} 탈착");
        }

        /// <summary>검증 실패 시 이전 슬롯이 있으면 되돌아가고, 없으면 현재 위치에 머문다.</summary>
        private void ReturnToCurrentSlotOrStay()
        {
            if (_currentAssignedFacilityView == null) return;
            _currentAssignedFacilityView.UpdateSpiritDisplay(_cardView.Data);
            Dock(_currentAssignedFacilityView);
        }

        /// <summary>
        /// 카드를 슬롯 크기로 조정하고 아이콘만 표시한 뒤 LateUpdate 추적을 시작한다.
        /// ContentSizeFitter를 비활성화해 크기를 직접 제어한다.
        /// </summary>
        private void Dock(FacilityNodeView facilityView)
        {
            var slotTransform = facilityView.SpiritSlotTransform;
            if (slotTransform == null) return;

            _cardView.SetIconOnly(true);

            if (_contentSizeFitter != null)
                _contentSizeFitter.enabled = false;

            // 슬롯의 실제 크기를 읽어 카드에 적용한다
            var slotRT = slotTransform as RectTransform;
            if (slotRT != null && slotRT.rect.size.sqrMagnitude > 0.01f)
                _rectTransform.sizeDelta = slotRT.rect.size;

            _rectTransform.position = slotTransform.position;
            _isDocked = true;
        }

        /// <summary>
        /// LateUpdate 추적을 중단하고 카드를 원래 레이아웃(ContentSizeFitter)으로 복원한다.
        /// </summary>
        private void Undock()
        {
            _isDocked = false;
            _cardView.SetIconOnly(false);

            if (_contentSizeFitter != null)
                _contentSizeFitter.enabled = true;

            LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);
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
