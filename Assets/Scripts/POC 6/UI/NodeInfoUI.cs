using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace POC6
{
    /// <summary>
    /// Build Phase에서 노드를 클릭하면 해당 노드의 상세 정보를 표시합니다.
    /// 노드 타입, 체력, 레벨, 동력 수신량, 공격 스탯, 업그레이드, 동력 연결을 담당합니다.
    /// NodeUpgradeUI를 대체합니다.
    /// </summary>
    public class NodeInfoUI : MonoBehaviour
    {
        [Header("패널")]
        [SerializeField] private GameObject _panelRoot;

        [Header("기본 정보")]
        [SerializeField] private TextMeshProUGUI _nodeNameText;
        [SerializeField] private TextMeshProUGUI _nodeTypeText;
        [SerializeField] private TextMeshProUGUI _hpText;
        [SerializeField] private TextMeshProUGUI _levelText;

        [Header("동력 정보")]
        [Tooltip("받는 동력량과 비율을 표시합니다. (예: 동력: 50 / 100 (50%))")]
        [SerializeField] private TextMeshProUGUI _powerText;

        [Header("공격 스탯 (공격 노드 전용)")]
        [Tooltip("공격 노드일 때만 활성화할 섹션 루트 오브젝트")]
        [SerializeField] private GameObject _attackStatsSection;
        [SerializeField] private TextMeshProUGUI _damageText;
        [SerializeField] private TextMeshProUGUI _fireRateText;
        [SerializeField] private TextMeshProUGUI _attackRangeText;

        [Header("업그레이드")]
        [SerializeField] private TextMeshProUGUI _upgradeCostText;
        [SerializeField] private Button _upgradeButton;

        [Header("동력 연결")]
        [Tooltip("클릭하면 이 노드에서 동력 연결 드래그를 시작합니다.")]
        [SerializeField] private Button _connectPowerButton;

        [Header("닫기")]
        [SerializeField] private Button _closeButton;

        [Header("참조")]
        [SerializeField] private GameConfig _config;
        [SerializeField] private GoldSystem _goldSystem;
        [SerializeField] private ShipGrid _shipGrid;
        [SerializeField] private PowerGraph _powerGraph;
        [SerializeField] private HealthSystem _healthSystem;
        [SerializeField] private PowerConnectionDragger _powerConnectionDragger;
        [SerializeField] private NodePlacer _nodePlacer;
        [SerializeField] private Camera _mainCamera;

        // 현재 선택된 노드
        private PlacedNode _selectedNode;

        private void Awake()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;

            _upgradeButton?.onClick.AddListener(HandleUpgradeClicked);
            _connectPowerButton?.onClick.AddListener(HandleConnectPowerClicked);
            _closeButton?.onClick.AddListener(Hide);

            if (_panelRoot != null) _panelRoot.SetActive(false);
        }

        private void Update()
        {
            // Build Phase에서만 동작
            if (GameManager.Instance?.CurrentState != GameState.BuildPhase) return;

            // 노드 배치 중일 때는 클릭을 가로채지 않음
            if (_nodePlacer != null && _nodePlacer.IsPlacing) return;

            if (Mouse.current.leftButton.wasPressedThisFrame)
                TrySelectNodeAtMouse();
        }

        // ────────────────────────────────────────────────
        // 노드 선택
        // ────────────────────────────────────────────────

        /// <summary>
        /// 마우스 위치의 노드를 클릭했는지 확인하고, 있으면 정보 패널을 표시합니다.
        /// </summary>
        private void TrySelectNodeAtMouse()
        {
            if (_shipGrid == null)
            {
                Debug.LogWarning("[NodeInfoUI] ShipGrid 참조가 없습니다. Inspector에서 연결해주세요.");
                return;
            }

            Vector3 mouseWorld = GetMouseWorldPosition();
            Vector2Int cell = _shipGrid.WorldToGrid(mouseWorld);
            PlacedNode node = _shipGrid.GetNodeAt(cell);

            if (node != null)
                ShowForNode(node);
            else
                Hide();
        }

        /// <summary>
        /// 특정 노드의 정보 패널을 표시합니다.
        /// </summary>
        public void ShowForNode(PlacedNode node)
        {
            _selectedNode = node;
            if (_panelRoot != null) _panelRoot.SetActive(true);
            RefreshUI();
        }

        /// <summary>
        /// 패널을 숨기고 선택을 초기화합니다.
        /// </summary>
        public void Hide()
        {
            _selectedNode = null;
            if (_panelRoot != null) _panelRoot.SetActive(false);
        }

        // ────────────────────────────────────────────────
        // UI 갱신
        // ────────────────────────────────────────────────

        /// <summary>
        /// 선택된 노드의 모든 정보를 UI에 반영합니다.
        /// </summary>
        private void RefreshUI()
        {
            if (_selectedNode == null) return;

            RefreshBasicInfo();
            RefreshHPInfo();
            RefreshPowerInfo();
            RefreshAttackStats();
            RefreshUpgradeInfo();
        }

        /// <summary>
        /// 노드 이름, 타입, 레벨을 갱신합니다.
        /// </summary>
        private void RefreshBasicInfo()
        {
            if (_nodeNameText != null)
                _nodeNameText.text = _selectedNode.Data.NodeName;

            if (_nodeTypeText != null)
                _nodeTypeText.text = GetNodeTypeName(_selectedNode.Data.NodeType);

            if (_levelText != null)
                _levelText.text = $"레벨 {_selectedNode.CurrentUpgradeLevel + 1}";
        }

        /// <summary>
        /// 노드의 현재/최대 체력을 갱신합니다.
        /// WorldInstance에서 NodeHealth 컴포넌트를 읽어옵니다.
        /// </summary>
        private void RefreshHPInfo()
        {
            if (_hpText == null) return;

            NodeHealth nodeHealth = _selectedNode.WorldInstance != null
                ? _selectedNode.WorldInstance.GetComponent<NodeHealth>()
                : null;

            _hpText.text = nodeHealth != null
                ? $"HP: {Mathf.CeilToInt(nodeHealth.CurrentHealth)} / {Mathf.CeilToInt(nodeHealth.MaxHealth)}"
                : "HP: -";
        }

        /// <summary>
        /// 이 노드가 받고 있는 동력량을 갱신합니다.
        /// 공격 노드 전용 정보입니다.
        /// </summary>
        private void RefreshPowerInfo()
        {
            if (_powerText == null) return;
            if (_powerGraph == null) return;

            if (_selectedNode.Data.NodeType != NodeType.Attack)
            {
                _powerText.text = string.Empty;
                return;
            }

            float received = _powerGraph.GetReceivedPower(_selectedNode);
            float total = _powerGraph.GetTotalPower(_selectedNode);

            _powerText.text = total > 0
                ? $"동력: {received:F0} / {total:F0} ({received / total * 100f:F0}%)"
                : "동력: 연결 없음";
        }

        /// <summary>
        /// 공격 노드의 실제 전투 스탯을 갱신합니다.
        /// 동력 연결 여부에 따라 실제로 적용되는 수치를 표시합니다.
        /// </summary>
        private void RefreshAttackStats()
        {
            bool isAttack = _selectedNode.Data.NodeType == NodeType.Attack;

            if (_attackStatsSection != null)
                _attackStatsSection.SetActive(isAttack);

            if (!isAttack || _powerGraph == null) return;

            AttackStats stats = _powerGraph.GetEffectiveStats(_selectedNode);

            if (_damageText != null) _damageText.text = $"데미지: {stats.Damage:F1}";
            if (_fireRateText != null) _fireRateText.text = $"공격속도: {stats.FireRate:F1}/s";
            if (_attackRangeText != null) _attackRangeText.text = $"사거리: {stats.AttackRange:F1}";
        }

        /// <summary>
        /// 업그레이드 비용과 버튼 활성화 여부를 갱신합니다.
        /// </summary>
        private void RefreshUpgradeInfo()
        {
            int cost = CalculateUpgradeCost(_selectedNode);

            if (_upgradeCostText != null)
                _upgradeCostText.text = $"업그레이드: {cost}G";

            if (_upgradeButton != null)
                _upgradeButton.interactable = _goldSystem != null && _goldSystem.CanAfford(cost);
        }

        // ────────────────────────────────────────────────
        // 버튼 핸들러
        // ────────────────────────────────────────────────

        /// <summary>
        /// 업그레이드 버튼 클릭 처리.
        /// 골드를 소비하고 노드 레벨을 올린 뒤 체력을 재계산합니다.
        /// </summary>
        private void HandleUpgradeClicked()
        {
            if (_selectedNode == null) return;

            int cost = CalculateUpgradeCost(_selectedNode);

            if (_goldSystem == null || !_goldSystem.CanAfford(cost))
            {
                Debug.Log("[NodeInfoUI] 골드 부족");
                return;
            }

            _goldSystem.SpendGold(cost);
            _selectedNode.UpgradeLevel();
            _healthSystem.Initialize();
            RefreshUI();
        }

        /// <summary>
        /// 동력 연결 버튼 클릭 처리.
        /// 패널을 닫고 이 노드에서 동력 연결 드래그를 시작합니다.
        /// </summary>
        private void HandleConnectPowerClicked()
        {
            if (_selectedNode == null || _powerConnectionDragger == null) return;

            Hide();
            _powerConnectionDragger.BeginDrag(_selectedNode);
        }

        // ────────────────────────────────────────────────
        // 헬퍼
        // ────────────────────────────────────────────────

        /// <summary>
        /// 업그레이드 비용을 계산합니다. 기본 비용 * 배수 ^ 현재 레벨
        /// </summary>
        private int CalculateUpgradeCost(PlacedNode node)
        {
            if (_config == null) return 0;
            return Mathf.RoundToInt(
                _config.BaseUpgradeCost * Mathf.Pow(_config.UpgradeCostMultiplier, node.CurrentUpgradeLevel)
            );
        }

        /// <summary>
        /// NodeType 열거형 값을 한국어 이름으로 변환합니다.
        /// </summary>
        private string GetNodeTypeName(NodeType type)
        {
            return type switch
            {
                NodeType.Core => "코어",
                NodeType.Attack => "공격",
                NodeType.Special => "특수",
                NodeType.Normal => "일반",
                _ => "알 수 없음"
            };
        }

        private Vector3 GetMouseWorldPosition()
        {
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector3 pos = new Vector3(mouseScreenPos.x, mouseScreenPos.y, Mathf.Abs(_mainCamera.transform.position.z));
            return _mainCamera.ScreenToWorldPoint(pos);
        }
    }
}
