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
    ///   2. 설비 카드 위에 드롭 → 속성 검증 후 배치
    ///   3. 빈 곳에 드롭하거나 검증 실패 → 카드는 드롭한 위치에 그대로 머문다
    ///
    /// 속성 검증 규칙:
    ///   설비가 RequiresSpirit = true이고 RequiredSpiritElement가 스피릿의 Element와 일치해야 한다.
    ///
    /// 재배치 처리:
    ///   이 스피릿이 이미 다른 설비에 배치 중이면 이전 설비의 슬롯을 먼저 초기화한다.
    ///
    /// 레이캐스트 처리:
    ///   드롭 시 자기 자신(스피릿 카드)이 레이캐스트 결과에 포함되므로 이를 걸러낸다.
    /// </summary>
    public class SpiritDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private SpiritCardView _cardView;
        private RectTransform _rectTransform;
        private Canvas _canvas;
        private GraphicRaycaster _raycaster;

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
        /// 드래그 시작: 카드를 Hierarchy 맨 뒤로 이동해 다른 UI 위에 렌더링한다.
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            transform.SetAsLastSibling();
        }

        /// <summary>
        /// 드래그 중: 마우스 이동량만큼 카드를 이동한다.
        /// 설비 노드와 동일하게 드롭한 위치에 카드가 그대로 남는다.
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            _rectTransform.anchoredPosition += eventData.delta / _canvas.scaleFactor;
        }

        /// <summary>
        /// 드래그 종료: 드롭 위치 아래의 설비를 찾아 배치를 시도한다.
        /// 설비가 없는 빈 곳에 드롭하면 현재 배치를 해제한다.
        /// 배치 성공 여부와 무관하게 카드는 현재 위치에 머문다.
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
        /// 현재 배치된 설비의 스피릿 슬롯을 초기화하고 배치 상태를 해제한다.
        /// 빈 곳에 드롭했을 때 호출된다.
        /// </summary>
        private void UnassignFromCurrentFacility()
        {
            if (_currentAssignedFacilityView == null) return;

            var facilityNode = _currentAssignedFacilityView.GetComponent<FacilityNode>();
            if (facilityNode != null)
                facilityNode.GraphNode.UnassignSpirit();

            _currentAssignedFacilityView.UpdateSpiritDisplay(null);
            _currentAssignedFacilityView = null;

            Debug.Log($"[SpiritDragHandler] {_cardView.Data.DisplayName} 배치 해제");
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

            // 그래프 노드에 스피릿을 배치하고 설비 카드 UI를 갱신한다
            facilityNode.GraphNode.AssignSpirit(spiritData);
            facilityView.UpdateSpiritDisplay(spiritData);
            _currentAssignedFacilityView = facilityView;

            Debug.Log($"[SpiritDragHandler] {spiritData.DisplayName}({spiritData.Element}) → {facilityData.DisplayName} 배치 완료");
        }

        /// <summary>
        /// 화면 좌표 아래의 FacilityNodeView를 반환한다.
        /// 이 스피릿 카드 자신(및 자식들)은 결과에서 제외한다.
        /// 드래그 중 스피릿 카드가 최상위에 있어 자기 자신이 먼저 감지되기 때문이다.
        /// </summary>
        private FacilityNodeView FindFacilityViewAtScreenPoint(Vector2 screenPoint)
        {
            var results   = new List<RaycastResult>();
            var eventData = new PointerEventData(EventSystem.current) { position = screenPoint };
            _raycaster.Raycast(eventData, results);

            foreach (var result in results)
            {
                // 자기 자신과 자신의 자식 오브젝트는 건너뛴다
                if (result.gameObject.transform.IsChildOf(transform)) continue;

                var fv = result.gameObject.GetComponentInParent<FacilityNodeView>();
                if (fv != null) return fv;
            }
            return null;
        }
    }
}
