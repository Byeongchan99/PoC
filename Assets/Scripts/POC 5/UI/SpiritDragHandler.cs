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
    ///   2. 설비 카드 위에 드롭 → 검증 통과 시 카드가 슬롯 안으로 스냅(장착)
    ///   3. 슬롯의 빈 상태 힌트가 사라지고 카드가 슬롯을 채운다
    ///
    /// 탈착 흐름:
    ///   1. 슬롯에 장착된 카드를 드래그 → 카드가 슬롯에서 빠져나와 Canvas로 복귀
    ///   2. 슬롯이 즉시 빈 상태 힌트를 표시한다
    ///   3. 빈 곳에 드롭 → 탈착 확정, 카드는 드롭 위치에 머문다
    ///   4. 다른 설비에 드롭 → 새 슬롯에 장착
    ///
    /// 검증 규칙:
    ///   RequiresSpirit = true이고 RequiredSpiritElement가 스피릿 Element와 일치해야 한다.
    /// </summary>
    public class SpiritDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private SpiritCardView _cardView;
        private RectTransform _rectTransform;
        private Canvas _canvas;
        private GraphicRaycaster _raycaster;
        private Transform _canvasTransform;

        // 현재 장착된 설비 뷰. null이면 미장착 상태.
        private FacilityNodeView _currentAssignedFacilityView;

        private void Awake()
        {
            _cardView        = GetComponent<SpiritCardView>();
            _rectTransform   = GetComponent<RectTransform>();
            _canvas          = GetComponentInParent<Canvas>();
            _raycaster       = _canvas.GetComponent<GraphicRaycaster>();
            _canvasTransform = _canvas.transform;
        }

        /// <summary>
        /// 드래그 시작.
        /// 슬롯에 장착된 상태라면 카드를 Canvas로 복귀시켜 자유롭게 드래그할 수 있게 하고,
        /// 슬롯은 즉시 빈 상태로 표시한다.
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (transform.parent != _canvasTransform)
            {
                // 슬롯에서 분리 전에 현재 월드 위치를 저장한다
                Vector3 worldPos = transform.position;

                // Canvas 직속 자식으로 복귀. 설비 카드가 이동해도 따라가지 않도록 한다
                transform.SetParent(_canvasTransform, false);

                // 앵커를 중앙 고정으로 되돌리고 슬롯과 동일한 화면 위치를 유지한다
                _rectTransform.anchorMin = _rectTransform.anchorMax =
                    _rectTransform.pivot  = new Vector2(0.5f, 0.5f);
                _rectTransform.position  = worldPos;

                // 슬롯을 즉시 빈 상태로 표시한다
                _currentAssignedFacilityView?.UpdateSpiritDisplay(null);
            }

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
        /// 검증을 통과하면 카드가 슬롯에 스냅된다.
        /// </summary>
        private void TryAssignToFacility(FacilityNodeView facilityView)
        {
            var spiritData   = _cardView.Data;
            var facilityNode = facilityView.GetComponent<FacilityNode>();
            if (facilityNode == null) return;

            var facilityData = facilityNode.GraphNode.Data;

            // 스피릿이 필요 없는 설비에는 장착 불가
            if (!facilityData.RequiresSpirit)
            {
                Debug.LogWarning(
                    $"[SpiritDragHandler] {facilityData.DisplayName}은(는) 스피릿이 필요 없는 설비입니다.");
                ReturnToCurrentSlotOrStay();
                return;
            }

            // 속성(원소) 불일치
            if (facilityData.RequiredSpiritElement != spiritData.Element)
            {
                Debug.LogWarning(
                    $"[SpiritDragHandler] 속성 불일치: {spiritData.Element} 스피릿 → " +
                    $"{facilityData.DisplayName} ({facilityData.RequiredSpiritElement} 필요)");
                ReturnToCurrentSlotOrStay();
                return;
            }

            // 기존에 장착된 설비 슬롯을 해제한다 (OnBeginDrag에서 이미 빈 상태로 표시됨)
            if (_currentAssignedFacilityView != null && _currentAssignedFacilityView != facilityView)
            {
                var prevNode = _currentAssignedFacilityView.GetComponent<FacilityNode>();
                prevNode?.GraphNode.UnassignSpirit();
            }

            // 그래프에 장착하고 슬롯에 스냅한다
            facilityNode.GraphNode.AssignSpirit(spiritData);
            facilityView.UpdateSpiritDisplay(spiritData);
            _currentAssignedFacilityView = facilityView;

            SnapToSlot(facilityView.SpiritSlotTransform);

            Debug.Log(
                $"[SpiritDragHandler] {spiritData.DisplayName}({spiritData.Element}) → " +
                $"{facilityData.DisplayName} 장착");
        }

        /// <summary>
        /// 빈 곳에 드롭했을 때 탈착을 확정한다.
        /// 그래프에서 스피릿을 제거하고 카드는 드롭 위치에 그대로 둔다.
        /// </summary>
        private void UnassignFromCurrentFacility()
        {
            if (_currentAssignedFacilityView == null) return;

            var facilityNode = _currentAssignedFacilityView.GetComponent<FacilityNode>();
            facilityNode?.GraphNode.UnassignSpirit();

            // OnBeginDrag에서 이미 UpdateSpiritDisplay(null)을 호출했으므로 UI 갱신은 불필요하다
            _currentAssignedFacilityView = null;

            Debug.Log($"[SpiritDragHandler] {_cardView.Data.DisplayName} 탈착");
        }

        /// <summary>
        /// 검증 실패 시 이전 슬롯이 있으면 되돌아가고, 없으면 현재 위치에 머문다.
        /// </summary>
        private void ReturnToCurrentSlotOrStay()
        {
            if (_currentAssignedFacilityView == null) return;

            // 슬롯이 비어 있는 상태이므로 다시 채워준다
            var facilityNode = _currentAssignedFacilityView.GetComponent<FacilityNode>();
            facilityNode?.GraphNode.AssignSpirit(_cardView.Data);
            _currentAssignedFacilityView.UpdateSpiritDisplay(_cardView.Data);

            SnapToSlot(_currentAssignedFacilityView.SpiritSlotTransform);
        }

        /// <summary>
        /// 카드를 슬롯 Transform의 자식으로 이동하고 슬롯을 가득 채우도록 RectTransform을 설정한다.
        /// </summary>
        private void SnapToSlot(Transform slotTransform)
        {
            if (slotTransform == null) return;

            transform.SetParent(slotTransform, false);
            _rectTransform.anchorMin = Vector2.zero;
            _rectTransform.anchorMax = Vector2.one;
            _rectTransform.pivot     = new Vector2(0.5f, 0.5f);
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;
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
