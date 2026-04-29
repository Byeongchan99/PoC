using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using POC5.Runtime;

namespace POC5.UI
{
    /// <summary>
    /// 스피릿 카드를 설비 카드 위에 드래그-드롭해 스피릿을 배치하는 컴포넌트.
    ///
    /// 동작 흐름:
    ///   1. 스피릿 카드를 드래그 → 마우스를 따라 이동
    ///   2. 설비 카드 위에 드롭 → 속성 검증
    ///   3. 검증 통과 시 스피릿 배치, 설비 카드의 스피릿 슬롯 갱신
    ///   4. 드래그 카드는 항상 원래 위치로 복귀
    ///
    /// 속성 검증 규칙:
    ///   설비가 RequiresSpirit = true이고 RequiredSpiritElement가 스피릿의 Element와 일치해야 한다.
    ///
    /// 재배치 처리:
    ///   이 스피릿이 이미 다른 설비에 배치 중이면 이전 설비의 슬롯을 먼저 초기화한다.
    /// </summary>
    public class SpiritDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private SpiritCardView _cardView;
        private RectTransform _rectTransform;
        private Canvas _canvas;
        private GraphicRaycaster _raycaster;

        // 드래그 시작 전 상태 저장 (드롭 후 복귀에 사용)
        private int _originalSiblingIndex;
        private Vector2 _originalAnchoredPosition;

        // 현재 이 스피릿이 배치된 설비 뷰 (재배치 시 이전 슬롯 초기화에 사용)
        private FacilityNodeView _currentAssignedFacilityView;

        private void Awake()
        {
            _cardView      = GetComponent<SpiritCardView>();
            _rectTransform = GetComponent<RectTransform>();
            _canvas        = GetComponentInParent<Canvas>();
            _raycaster     = _canvas.GetComponent<GraphicRaycaster>();
        }

        /// <summary>
        /// 드래그 시작: 현재 위치를 저장하고 카드를 최상위로 이동해 다른 카드 위에 렌더링한다.
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            _originalSiblingIndex    = transform.GetSiblingIndex();
            _originalAnchoredPosition = _rectTransform.anchoredPosition;
            // 다른 카드보다 앞에 그려지도록 Hierarchy 맨 뒤로 이동한다
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
        /// 드래그 종료: 드롭 대상 설비를 찾아 배치를 시도하고, 카드를 원래 위치로 복귀한다.
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            // 렌더 순서와 위치를 드래그 전으로 복원한다
            transform.SetSiblingIndex(_originalSiblingIndex);
            _rectTransform.anchoredPosition = _originalAnchoredPosition;

            var facilityView = FindFacilityViewAtScreenPoint(eventData.position);
            if (facilityView != null)
                TryAssignToFacility(facilityView);
        }

        /// <summary>
        /// 대상 설비에 스피릿 배치를 시도한다.
        /// RequiresSpirit과 RequiredSpiritElement를 검증한 뒤 배치한다.
        /// </summary>
        private void TryAssignToFacility(FacilityNodeView facilityView)
        {
            var spiritData   = _cardView.Data;
            var facilityNode = facilityView.GetComponent<FacilityNode>();

            if (facilityNode == null) return;

            var facilityData = facilityNode.GraphNode.Data;

            // 스피릿이 필요 없는 설비에는 배치 불가
            if (!facilityData.RequiresSpirit)
            {
                Debug.LogWarning($"[SpiritDragHandler] {facilityData.DisplayName}은(는) 스피릿이 필요 없는 설비입니다.");
                return;
            }

            // 속성(원소) 불일치
            if (facilityData.RequiredSpiritElement != spiritData.Element)
            {
                Debug.LogWarning(
                    $"[SpiritDragHandler] 속성 불일치: {spiritData.Element} 스피릿 → " +
                    $"{facilityData.DisplayName} ({facilityData.RequiredSpiritElement} 필요)");
                return;
            }

            // 이미 같은 설비에 배치 중이면 무시한다
            if (_currentAssignedFacilityView == facilityView) return;

            // 이전 설비의 스피릿 슬롯을 초기화한다
            if (_currentAssignedFacilityView != null)
                _currentAssignedFacilityView.UpdateSpiritDisplay(null);

            // 그래프 노드에 스피릿을 배치하고 카드 UI를 갱신한다
            facilityNode.GraphNode.AssignSpirit(spiritData);
            facilityView.UpdateSpiritDisplay(spiritData);
            _currentAssignedFacilityView = facilityView;

            Debug.Log($"[SpiritDragHandler] {spiritData.DisplayName}({spiritData.Element}) → {facilityData.DisplayName} 배치 완료");
        }

        /// <summary>
        /// 화면 좌표 아래의 FacilityNodeView를 반환한다.
        /// GraphicRaycaster로 UI 오브젝트를 감지하고 계층에서 FacilityNodeView를 찾는다.
        /// </summary>
        private FacilityNodeView FindFacilityViewAtScreenPoint(Vector2 screenPoint)
        {
            var results   = new List<RaycastResult>();
            var eventData = new PointerEventData(EventSystem.current) { position = screenPoint };
            _raycaster.Raycast(eventData, results);

            foreach (var result in results)
            {
                var fv = result.gameObject.GetComponentInParent<FacilityNodeView>();
                if (fv != null) return fv;
            }
            return null;
        }
    }
}
