using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using POC5.Data;
using POC5.Runtime;
using POC5.Graph;

namespace POC5.UI
{
    /// <summary>
    /// 설비 노드 카드 UI를 담당한다.
    /// FacilityNodeCard 프리팹에 이 컴포넌트를 붙이고
    /// Inspector에서 카드 내부 UI 요소들을 연결한다.
    ///
    /// 카드의 시각적 레이아웃(크기·색상·간격 등)은 프리팹 에디터에서 직접 수정한다.
    /// 이 스크립트는 데이터 바인딩과 포트 행 동적 생성만 담당한다.
    /// </summary>
    public class FacilityNodeView : MonoBehaviour
    {
        [Header("카드 내부 UI 참조 (프리팹에서 연결)")]
        [Tooltip("헤더의 레벨 텍스트.")]
        [SerializeField] private TextMeshProUGUI _levelText;

        [Tooltip("헤더의 설비명 텍스트.")]
        [SerializeField] private TextMeshProUGUI _nameText;

        [Tooltip("아이콘 이미지 컴포넌트.")]
        [SerializeField] private Image _iconImage;

        [Tooltip("포트 행들이 들어갈 컨테이너. VerticalLayoutGroup을 붙여둔다.")]
        [SerializeField] private Transform _portsContainer;

        [Tooltip("업그레이드 버튼.")]
        [SerializeField] private Button _upgradeButton;

        [Tooltip("업그레이드 가격 텍스트.")]
        [SerializeField] private TextMeshProUGUI _upgradePriceText;

        [Header("스피릿 슬롯 (프리팹에서 연결, 스피릿이 필요한 설비에만 표시)")]
        [Tooltip("스피릿 슬롯 패널. 설비가 RequiresSpirit=false이면 자동으로 숨겨진다.\n" +
                 "SpiritDragHandler가 정령 카드를 이 패널의 자식으로 이동해 슬롯을 채운다.")]
        [SerializeField] private GameObject _spiritSlotPanel;

        [Tooltip("정령이 배치되지 않았을 때 슬롯 안에 표시할 힌트 오브젝트.\n" +
                 "정령이 장착되면 자동으로 숨겨지고, 탈착되면 다시 표시된다.")]
        [SerializeField] private GameObject _spiritEmptyHint;

        [Tooltip("힌트 오브젝트 안의 텍스트. 필요한 정령 원소 이름이 자동으로 채워진다.")]
        [SerializeField] private TextMeshProUGUI _spiritHintText;

        [Tooltip("장착된 정령의 아이콘을 표시하는 Image. 정령이 없을 때는 숨겨진다.\n" +
                 "raycastTarget을 false로 설정해야 슬롯 드래그 이벤트가 패널에 전달된다.")]
        [SerializeField] private Image _assignedSpiritIcon;

        [Tooltip("배치된 스피릿 이름과 속성을 표시하는 텍스트. 선택 사항.")]
        [SerializeField] private TextMeshProUGUI _spiritInfoText;

        [Header("포트 행 프리팹")]
        [Tooltip("입력 포트용 행 프리팹. 빨간 원이 왼쪽에 배치된 것을 사용한다.")]
        [SerializeField] private PortView _inputPortPrefab;

        [Tooltip("출력 포트용 행 프리팹. 초록 원이 오른쪽에 배치된 것을 사용한다.")]
        [SerializeField] private PortView _outputPortPrefab;

        [Header("설정")]
        [Tooltip("포트 잔량 텍스트를 갱신하는 주기 (초).")]
        [SerializeField] private float _refreshInterval = 0.5f;

        private readonly List<PortView> _portViews = new List<PortView>();
        private float _refreshTimer;

        // 업그레이드 버튼 처리에 사용하는 런타임 참조
        private FacilityNode _facilityNode;
        private CurrencySystem _currencySystem;

        /// <summary>
        /// 이 카드에 포함된 모든 PortView.
        /// 4단계에서 PortConnectHandler가 포트 버튼에 접근할 때 사용한다.
        /// </summary>
        public IReadOnlyList<PortView> PortViews => _portViews;

        /// <summary>
        /// 정령 카드가 스냅될 슬롯 Transform.
        /// SpiritDragHandler가 카드를 이 Transform의 자식으로 이동한다.
        /// </summary>
        public Transform SpiritSlotTransform => _spiritSlotPanel?.transform;

        /// <summary>
        /// 설비 데이터를 카드 UI에 바인딩하고 포트 행을 생성한다.
        /// GameSceneManager에서 Instantiate 직후 호출한다.
        /// </summary>
        public void Initialize(FacilityNode facilityNode)
        {
            var data = facilityNode.GraphNode.Data;

            _levelText.text = "Lv.1";
            _nameText.text = data.DisplayName;

            if (data.Icon != null)
            {
                _iconImage.sprite = data.Icon;
                _iconImage.preserveAspect = true;
            }

            // 업그레이드 버튼은 SetupUpgradeButton() 호출 전까지 비활성화 상태로 둔다
            if (_upgradeButton != null)
                _upgradeButton.interactable = false;

            BuildPortRows(facilityNode.GraphNode);
            InitializeSpiritSlot(facilityNode.GraphNode.Data);

            // 포트 행 추가 후 ContentSizeFitter가 카드 높이를 즉시 재계산하도록 강제한다
            // 이 호출이 없으면 카드 높이가 첫 프레임에 0으로 보일 수 있다
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        }

        private void Update()
        {
            // 매 프레임이 아닌 일정 간격으로 잔량을 갱신해 텍스트 갱신 비용을 줄인다
            _refreshTimer -= Time.deltaTime;
            if (_refreshTimer > 0f) return;
            _refreshTimer = _refreshInterval;
            RefreshPortAmounts();
        }

        /// <summary>
        /// 스피릿 슬롯 패널을 초기화한다.
        /// RequiresSpirit이 false인 설비는 슬롯을 숨긴다.
        /// SpiritSlotDragSource가 없으면 자동으로 추가한다.
        /// </summary>
        private void InitializeSpiritSlot(POC5.Data.FacilityData data)
        {
            if (_spiritSlotPanel == null) return;
            _spiritSlotPanel.SetActive(data.RequiresSpirit);

            if (!data.RequiresSpirit) return;

            // 슬롯 패널에 드래그 소스 컴포넌트가 없으면 런타임에 추가한다
            if (_spiritSlotPanel.GetComponent<SpiritSlotDragSource>() == null)
                _spiritSlotPanel.AddComponent<SpiritSlotDragSource>();

            // 필요한 정령 원소를 힌트 텍스트에 채운다
            if (_spiritHintText != null)
                _spiritHintText.text = $"{data.RequiredSpiritElement} 정령 슬롯";

            SetSlotEmptyState(isEmpty: true);
        }

        /// <summary>
        /// 배치된 스피릿 정보를 슬롯 텍스트에 표시한다.
        /// spirit이 null이면 "배치 없음"으로 초기화한다.
        /// SpiritDragHandler가 배치/해제 시 호출한다.
        /// </summary>
        /// <summary>
        /// 슬롯 상태를 갱신한다.
        /// spirit이 null이면 빈 상태 힌트를 표시하고, 아니면 정령 아이콘을 표시한다.
        /// SpiritDragHandler가 장착/탈착 시 호출한다.
        /// </summary>
        public void UpdateSpiritDisplay(SpiritData spirit)
        {
            bool hasSpirit = spirit != null;
            SetSlotEmptyState(isEmpty: !hasSpirit);

            if (_assignedSpiritIcon != null)
            {
                _assignedSpiritIcon.gameObject.SetActive(hasSpirit);
                if (hasSpirit && spirit.Icon != null)
                {
                    _assignedSpiritIcon.sprite = spirit.Icon;
                    _assignedSpiritIcon.preserveAspect = true;
                }
            }
        }

        /// <summary>빈 상태 힌트의 표시 여부를 전환한다.</summary>
        private void SetSlotEmptyState(bool isEmpty)
        {
            if (_spiritEmptyHint != null)
                _spiritEmptyHint.SetActive(isEmpty);
        }

        /// <summary>
        /// 업그레이드 버튼을 활성화하고 클릭 로직을 연결한다.
        /// GameSceneManager가 설비 카드를 생성한 직후 호출한다.
        /// </summary>
        public void SetupUpgradeButton(FacilityNode facilityNode, CurrencySystem currencySystem)
        {
            _facilityNode    = facilityNode;
            _currencySystem  = currencySystem;

            if (_upgradeButton == null) return;
            _upgradeButton.onClick.AddListener(OnUpgradeClicked);
            _currencySystem.OnGoldChanged += OnGoldChanged;
            UpdateUpgradeButtonState();
        }

        private void OnDestroy()
        {
            if (_currencySystem != null)
                _currencySystem.OnGoldChanged -= OnGoldChanged;
        }

        /// <summary>업그레이드 버튼 클릭 핸들러.</summary>
        private void OnUpgradeClicked()
        {
            if (_facilityNode == null || _currencySystem == null) return;
            if (!_facilityNode.TryUpgrade(_currencySystem)) return;

            _levelText.text = $"Lv.{_facilityNode.Level}";
            UpdateUpgradeButtonState();
        }

        /// <summary>골드 변경 이벤트 콜백 — 구매 가능 여부를 재평가한다.</summary>
        private void OnGoldChanged(int newGold)
        {
            UpdateUpgradeButtonState();
        }

        /// <summary>
        /// 현재 레벨·골드 기준으로 업그레이드 버튼 상태와 가격 텍스트를 갱신한다.
        /// </summary>
        private void UpdateUpgradeButtonState()
        {
            if (_facilityNode == null || _upgradeButton == null) return;

            bool canUpgrade = _facilityNode.CanUpgrade();
            bool canAfford  = canUpgrade && _currencySystem.CanAfford(_facilityNode.GetUpgradeCost());

            _upgradeButton.interactable = canUpgrade && canAfford;

            if (_upgradePriceText != null)
            {
                _upgradePriceText.text = canUpgrade
                    ? $"업그레이드  {_facilityNode.GetUpgradeCost()}G"
                    : "최대 레벨";
            }
        }

        /// <summary>모든 포트 뷰의 잔량 텍스트를 즉시 갱신한다.</summary>
        public void RefreshPortAmounts()
        {
            foreach (var pv in _portViews)
                pv.RefreshAmount();
        }

        /// <summary>
        /// 입력 포트를 먼저, 출력 포트를 나중에 각각 프리팹으로 인스턴스화해 PortsContainer에 배치한다.
        /// </summary>
        private void BuildPortRows(FacilityGraphNode graphNode)
        {
            foreach (var port in graphNode.InputPorts)
                AddPortRow(port, _inputPortPrefab);

            foreach (var port in graphNode.OutputPorts)
                AddPortRow(port, _outputPortPrefab);
        }

        /// <summary>
        /// 포트 행 프리팹을 인스턴스화하고 PortsContainer 아래에 배치한 뒤 초기화한다.
        /// </summary>
        private void AddPortRow(Port port, PortView prefab)
        {
            if (prefab == null)
            {
                Debug.LogError("[FacilityNodeView] 포트 행 프리팹이 연결되지 않았습니다. " +
                               "Inspector에서 InputPortPrefab / OutputPortPrefab을 설정해 주세요.");
                return;
            }

            var portView = Instantiate(prefab, _portsContainer);
            portView.Initialize(port);
            _portViews.Add(portView);
        }
    }
}
