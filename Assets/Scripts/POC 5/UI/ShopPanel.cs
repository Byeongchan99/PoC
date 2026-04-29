using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using POC5.Data;
using POC5.Runtime;

namespace POC5.UI
{
    /// <summary>
    /// 상점 패널을 관리하는 컴포넌트.
    ///
    /// 동작 흐름:
    ///   1. 씬 시작 시 _availableItems 배열을 읽어 ShopItemView를 동적으로 생성한다.
    ///   2. Open() 호출 시 패널이 표시되고 구매 가능 여부가 갱신된다.
    ///   3. 구매 버튼 클릭 → 골드 차감 → 캔버스에 설비 카드 생성.
    ///   4. 골드가 바뀌면 모든 구매 버튼의 활성화 상태를 재평가한다.
    ///
    /// 사용법:
    ///   상점 패널 GameObject에 이 컴포넌트를 붙이고
    ///   Inspector에서 필수 참조를 모두 연결한다.
    ///   상점 열기 버튼의 OnClick 이벤트에 ShopPanel.Open()을 연결한다.
    /// </summary>
    public class ShopPanel : MonoBehaviour
    {
        [Header("씬 참조")]
        [Tooltip("골드 잔액을 관리하는 CurrencySystem.")]
        [SerializeField] private CurrencySystem _currencySystem;

        [Tooltip("설비 카드를 씬에 생성하는 GameSceneManager.")]
        [SerializeField] private GameSceneManager _sceneManager;

        [Header("상점 패널 UI")]
        [Tooltip("상점 패널 루트 GameObject. Open/Close 시 활성화 토글된다.")]
        [SerializeField] private GameObject _panelRoot;

        [Tooltip("ShopItemView들이 배치될 스크롤 뷰 콘텐츠 컨테이너.")]
        [SerializeField] private Transform _itemContainer;

        [Tooltip("닫기 버튼.")]
        [SerializeField] private Button _closeButton;

        [Header("아이템 프리팹")]
        [Tooltip("상점 아이템 한 칸 프리팹. ShopItemView가 붙어 있어야 한다.")]
        [SerializeField] private ShopItemView _itemPrefab;

        [Header("판매 목록")]
        [Tooltip("상점에서 판매할 FacilityData SO 목록.")]
        [SerializeField] private FacilityData[] _availableItems;

        [Tooltip("구매한 설비 카드가 생성될 Canvas 기준 X 범위 (±픽셀).")]
        [SerializeField] private float _spawnScatterRange = 80f;

        private readonly List<ShopItemView> _itemViews = new List<ShopItemView>();

        private void Awake()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(Close);

            _currencySystem.OnGoldChanged += OnGoldChanged;
        }

        private void OnDestroy()
        {
            _currencySystem.OnGoldChanged -= OnGoldChanged;
        }

        private void Start()
        {
            BuildItemList();
            // 씬 시작 시 패널은 닫힌 상태로 둔다
            Close();
        }

        /// <summary>상점 패널을 열고 구매 가능 여부를 즉시 갱신한다.</summary>
        public void Open()
        {
            _panelRoot.SetActive(true);
            RefreshAffordability();
        }

        /// <summary>상점 패널을 닫는다.</summary>
        public void Close()
        {
            _panelRoot.SetActive(false);
        }

        /// <summary>
        /// _availableItems 배열을 순회하며 ShopItemView를 동적으로 생성한다.
        /// Start()에서 한 번만 호출된다.
        /// </summary>
        private void BuildItemList()
        {
            if (_availableItems == null) return;

            foreach (var data in _availableItems)
            {
                if (data == null) continue;
                var item = Instantiate(_itemPrefab, _itemContainer);
                bool canAfford = _currencySystem.CanAfford(data.PurchasePrice);
                item.Initialize(data, OnItemPurchased, canAfford);
                _itemViews.Add(item);
            }
        }

        /// <summary>
        /// 구매 버튼 클릭 시 호출되는 콜백.
        /// 골드를 차감하고 캔버스 중앙 근처에 설비 카드를 생성한다.
        /// </summary>
        private void OnItemPurchased(FacilityData data)
        {
            if (!_currencySystem.TrySpend(data.PurchasePrice)) return;

            // 카드가 정확히 겹치지 않도록 중앙에서 약간 흩어진 위치에 생성한다
            var position = new Vector2(
                Random.Range(-_spawnScatterRange, _spawnScatterRange),
                Random.Range(-_spawnScatterRange, _spawnScatterRange));
            _sceneManager.SpawnFacility(data, position);

            Debug.Log($"[ShopPanel] {data.DisplayName} 구매 완료 (-{data.PurchasePrice} G)");
        }

        /// <summary>
        /// 잔액이 변경될 때마다 모든 아이템의 구매 버튼 활성화 상태를 갱신한다.
        /// </summary>
        private void OnGoldChanged(int newGold)
        {
            RefreshAffordability();
        }

        /// <summary>
        /// 현재 잔액 기준으로 각 아이템의 구매 가능 여부를 재평가한다.
        /// </summary>
        private void RefreshAffordability()
        {
            if (_availableItems == null) return;
            for (int i = 0; i < _itemViews.Count; i++)
            {
                bool canAfford = _currencySystem.CanAfford(_availableItems[i].PurchasePrice);
                _itemViews[i].SetAffordable(canAfford);
            }
        }
    }
}
