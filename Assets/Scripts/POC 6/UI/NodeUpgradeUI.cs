using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace POC6
{
    /// <summary>
    /// Build Phase에서 노드를 클릭하면 나타나는 업그레이드 패널입니다.
    /// 업그레이드 비용을 표시하고, 골드가 충분하면 업그레이드를 실행합니다.
    /// </summary>
    public class NodeUpgradeUI : MonoBehaviour
    {
        [Header("UI 참조")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private TextMeshProUGUI _nodeNameText;
        [SerializeField] private TextMeshProUGUI _upgradeLevelText;
        [SerializeField] private TextMeshProUGUI _upgradeCostText;
        [SerializeField] private Button _upgradeButton;
        [SerializeField] private Button _closeButton;

        [Header("참조")]
        [SerializeField] private GameConfig _config;
        [SerializeField] private GoldSystem _goldSystem;
        [SerializeField] private ShipGrid _shipGrid;
        [SerializeField] private HealthSystem _healthSystem;
        [SerializeField] private Camera _mainCamera;

        // 현재 선택된 노드
        private PlacedNode _selectedNode;

        private void Awake()
        {
            if (_mainCamera == null)
                _mainCamera = Camera.main;

            _upgradeButton?.onClick.AddListener(HandleUpgradeClicked);
            _closeButton?.onClick.AddListener(Hide);

            _panelRoot.SetActive(false);
        }

        private void Update()
        {
            // Build Phase에서만 동작 (GameState 체크)
            if (GameManager.Instance?.CurrentState != GameState.BuildPhase) return;

            // 마우스 클릭으로 노드 선택
            if (Input.GetMouseButtonDown(0))
                TrySelectNodeAtMouse();
        }

        /// <summary>
        /// 마우스 위치의 노드를 선택하고 업그레이드 UI를 표시합니다.
        /// </summary>
        private void TrySelectNodeAtMouse()
        {
            Vector3 mouseWorld = GetMouseWorldPosition();
            Vector2Int cell = _shipGrid.WorldToGrid(mouseWorld);
            PlacedNode node = _shipGrid.GetNodeAt(cell);

            if (node != null)
                ShowForNode(node);
            else
                Hide();
        }

        /// <summary>
        /// 특정 노드에 대한 업그레이드 정보를 표시합니다.
        /// </summary>
        public void ShowForNode(PlacedNode node)
        {
            _selectedNode = node;
            _panelRoot.SetActive(true);
            RefreshUI();
        }

        /// <summary>
        /// 업그레이드 패널을 숨깁니다.
        /// </summary>
        public void Hide()
        {
            _selectedNode = null;
            _panelRoot.SetActive(false);
        }

        /// <summary>
        /// 업그레이드 버튼 클릭 처리.
        /// 비용을 소비하고 노드 레벨을 올립니다.
        /// </summary>
        private void HandleUpgradeClicked()
        {
            if (_selectedNode == null) return;

            int cost = CalculateUpgradeCost(_selectedNode);

            if (!_goldSystem.CanAfford(cost))
            {
                Debug.Log("[NodeUpgradeUI] 골드 부족!");
                return;
            }

            _goldSystem.SpendGold(cost);
            _selectedNode.UpgradeLevel();

            // 업그레이드 후 체력 재계산 (건강 기여도가 변할 수 있음)
            _healthSystem.Initialize(_shipGrid);

            RefreshUI();
        }

        /// <summary>
        /// 현재 노드의 업그레이드 비용을 계산합니다.
        /// 기본 비용 * 배수 ^ 현재 레벨
        /// </summary>
        private int CalculateUpgradeCost(PlacedNode node)
        {
            return Mathf.RoundToInt(
                _config.BaseUpgradeCost * Mathf.Pow(_config.UpgradeCostMultiplier, node.CurrentUpgradeLevel)
            );
        }

        /// <summary>
        /// UI 텍스트를 현재 노드 상태에 맞게 갱신합니다.
        /// </summary>
        private void RefreshUI()
        {
            if (_selectedNode == null) return;

            if (_nodeNameText != null)
                _nodeNameText.text = _selectedNode.Data.NodeName;

            if (_upgradeLevelText != null)
                _upgradeLevelText.text = $"레벨 {_selectedNode.CurrentUpgradeLevel + 1}";

            int cost = CalculateUpgradeCost(_selectedNode);

            if (_upgradeCostText != null)
                _upgradeCostText.text = $"업그레이드: {cost}G";

            if (_upgradeButton != null)
                _upgradeButton.interactable = _goldSystem.CanAfford(cost);
        }

        private Vector3 GetMouseWorldPosition()
        {
            Vector3 mouseScreenPos = Input.mousePosition;
            mouseScreenPos.z = Mathf.Abs(_mainCamera.transform.position.z);
            return _mainCamera.ScreenToWorldPoint(mouseScreenPos);
        }
    }
}
