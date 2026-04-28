using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace POC4
{
    /// <summary>
    /// 카드 제작 UI를 담당하는 클래스.
    ///
    /// 제작 흐름 (3단계):
    ///   1단계: 카드 종류 선택 (벽 / 타워)
    ///   2단계: 세부 종류 선택 (랜덤 3개 선택지)
    ///          - 벽: WallType 7종 중 3개 랜덤
    ///          - 타워: TowerType 3종 전부
    ///   3단계: 효과 선택 (None 고정 + 랜덤 3개, 총 4개)
    ///
    /// Canvas 패널 구성:
    ///   모달 패널 (ModalPanel) 아래 단계별 패널 3개를 두고,
    ///   현재 단계에 해당하는 패널만 활성화한다.
    ///   코스트는 항상 표시되는 별도 TMP_Text에 갱신한다.
    ///
    /// 제작 확정 시 CardData를 런타임으로 생성해 Hand에 추가하고 코스트를 차감한다.
    /// </summary>
    public class CardCraftingUI : MonoBehaviour
    {
        // -------------------------------------------------------
        // 제작 단계 열거형
        // -------------------------------------------------------

        private enum CraftingStep { None, SelectKind, SelectType, SelectEffect }

        // -------------------------------------------------------
        // Inspector 노출 필드 - 데이터 참조
        // -------------------------------------------------------

        [Header("References")]
        [SerializeField] private Hand _hand;
        [SerializeField] private CostManager _costManager;

        [Header("Tower Data Templates (TowerType별 기본 스탯 템플릿)")]
        [Tooltip("Arrow, Laser, Cannon 순서로 각 TowerType의 기본 TowerData 에셋을 등록한다.")]
        [SerializeField] private List<TowerData> _towerTemplates = new List<TowerData>();

        [Header("Crafting Costs")]
        [Tooltip("효과 없는 카드의 기본 제작 비용")]
        [SerializeField] private int _baseCraftCost = 5;

        [Tooltip("효과 있는 카드에 추가되는 비용")]
        [SerializeField] private int _effectExtraCost = 1;

        [Header("Wall Bonus Values (런타임 생성 WallData에 적용될 효과 수치)")]
        [SerializeField] private float _wallAttackBonus = 5f;
        [SerializeField] private float _wallRangeBonus = 1f;
        [SerializeField] private float _wallAttackSpeedBonus = 0.5f;

        // -------------------------------------------------------
        // Inspector 노출 필드 - Canvas UI 참조
        // -------------------------------------------------------

        [Header("Canvas - 코스트 표시")]
        [Tooltip("현재 보유 코스트를 표시하는 TMP_Text")]
        [SerializeField] private TMP_Text _costText;

        [Header("Canvas - 모달")]
        [Tooltip("제작 모달 전체를 감싸는 패널 (열릴 때 SetActive(true))")]
        [SerializeField] private GameObject _modalPanel;

        [Header("Canvas - 단계 패널 (ModalPanel 자식)")]
        [Tooltip("1단계: 카드 종류 선택 패널")]
        [SerializeField] private GameObject _step1Panel;

        [Tooltip("2단계: 세부 종류 선택 패널")]
        [SerializeField] private GameObject _step2Panel;

        [Tooltip("3단계: 효과 선택 패널")]
        [SerializeField] private GameObject _step3Panel;

        [Header("Canvas - 2단계 버튼 배열 (3개)")]
        [Tooltip("2단계에서 표시할 선택지 버튼 3개. 각 버튼은 TMP_Text 자식을 가져야 한다.")]
        [SerializeField] private Button[] _typeButtons = new Button[3];

        [Header("Canvas - 3단계 버튼 배열 (4개)")]
        [Tooltip("3단계에서 표시할 선택지 버튼 4개. 각 버튼은 TMP_Text 자식을 가져야 한다.")]
        [SerializeField] private Button[] _effectButtons = new Button[4];

        // -------------------------------------------------------
        // 내부 상태
        // -------------------------------------------------------

        private CraftingStep _step = CraftingStep.None;

        private CardData.CardKind _selectedKind;

        private WallData.WallType[] _wallTypeOptions;
        private TowerData.TowerType[] _towerTypeOptions;

        private WallData.WallType _selectedWallType;
        private TowerData.TowerType _selectedTowerType;

        private WallData.WallEffectType[] _wallEffectOptions;
        private TowerData.TowerEffectType[] _towerEffectOptions;

        // -------------------------------------------------------
        // 유니티 생명주기
        // -------------------------------------------------------

        private void Awake()
        {
            _modalPanel?.SetActive(false);
        }

        private void Update()
        {
            UpdateCostText();
        }

        // -------------------------------------------------------
        // 코스트 갱신
        // -------------------------------------------------------

        private void UpdateCostText()
        {
            if (_costText == null) return;
            int cost = _costManager != null ? _costManager.CurrentCost : 0;
            _costText.text = $"보유 코스트: {cost}";
        }

        // -------------------------------------------------------
        // 모달 열기 / 닫기 (Canvas 버튼 OnClick에 연결)
        // -------------------------------------------------------

        /// <summary>
        /// 카드 제작 버튼 OnClick에 연결한다. 모달을 열고 1단계로 진입한다.
        /// </summary>
        public void OpenModal()
        {
            _step = CraftingStep.SelectKind;
            _modalPanel?.SetActive(true);
            ShowStep(_step);
        }

        /// <summary>
        /// 닫기 버튼 OnClick에 연결한다. 모달을 닫고 초기 상태로 돌아간다.
        /// </summary>
        public void CloseModal()
        {
            _step = CraftingStep.None;
            _modalPanel?.SetActive(false);
        }

        // -------------------------------------------------------
        // 단계 전환
        // -------------------------------------------------------

        /// <summary>
        /// 현재 단계에 해당하는 패널만 활성화하고 나머지를 비활성화한다.
        /// </summary>
        private void ShowStep(CraftingStep step)
        {
            _step1Panel?.SetActive(step == CraftingStep.SelectKind);
            _step2Panel?.SetActive(step == CraftingStep.SelectType);
            _step3Panel?.SetActive(step == CraftingStep.SelectEffect);
        }

        // -------------------------------------------------------
        // 1단계: 카드 종류 선택 (Canvas 버튼 OnClick에 연결)
        // -------------------------------------------------------

        /// <summary>
        /// '벽 카드' 버튼 OnClick에 연결한다.
        /// </summary>
        public void OnSelectWallKind()
        {
            _selectedKind = CardData.CardKind.Wall;
            PrepareWallTypeOptions();
            ConfigureTypeButtons();
            _step = CraftingStep.SelectType;
            ShowStep(_step);
        }

        /// <summary>
        /// '타워 카드' 버튼 OnClick에 연결한다.
        /// </summary>
        public void OnSelectTowerKind()
        {
            _selectedKind = CardData.CardKind.Tower;
            PrepareTowerTypeOptions();
            ConfigureTypeButtons();
            _step = CraftingStep.SelectType;
            ShowStep(_step);
        }

        // -------------------------------------------------------
        // 2단계: 세부 종류 선택 버튼 구성
        // -------------------------------------------------------

        /// <summary>
        /// _typeButtons 배열의 버튼 텍스트와 클릭 이벤트를 현재 선택지로 설정한다.
        /// </summary>
        private void ConfigureTypeButtons()
        {
            if (_typeButtons == null) return;

            int optionCount = _selectedKind == CardData.CardKind.Wall
                ? _wallTypeOptions.Length
                : _towerTypeOptions.Length;

            for (int i = 0; i < _typeButtons.Length; i++)
            {
                Button btn = _typeButtons[i];
                if (btn == null) continue;

                if (i < optionCount)
                {
                    btn.gameObject.SetActive(true);

                    // 버튼 텍스트 갱신
                    TMP_Text label = btn.GetComponentInChildren<TMP_Text>();
                    if (label != null)
                    {
                        label.text = _selectedKind == CardData.CardKind.Wall
                            ? _wallTypeOptions[i].ToString()
                            : _towerTypeOptions[i].ToString();
                    }

                    // 클릭 콜백 설정 (클로저로 인덱스 캡처)
                    int capturedIndex = i;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnTypeSelected(capturedIndex));
                }
                else
                {
                    btn.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 2단계 버튼 클릭 시 호출된다. 선택된 종류를 저장하고 3단계로 이동한다.
        /// </summary>
        private void OnTypeSelected(int index)
        {
            if (_selectedKind == CardData.CardKind.Wall)
            {
                if (index >= _wallTypeOptions.Length) return;
                _selectedWallType = _wallTypeOptions[index];
                PrepareWallEffectOptions();
            }
            else
            {
                if (index >= _towerTypeOptions.Length) return;
                _selectedTowerType = _towerTypeOptions[index];
                PrepareTowerEffectOptions();
            }

            ConfigureEffectButtons();
            _step = CraftingStep.SelectEffect;
            ShowStep(_step);
        }

        // -------------------------------------------------------
        // 3단계: 효과 선택 버튼 구성
        // -------------------------------------------------------

        /// <summary>
        /// _effectButtons 배열의 버튼 텍스트, 활성화 여부, 클릭 이벤트를 현재 선택지로 설정한다.
        /// </summary>
        private void ConfigureEffectButtons()
        {
            if (_effectButtons == null) return;

            int optionCount = _selectedKind == CardData.CardKind.Wall
                ? _wallEffectOptions.Length
                : _towerEffectOptions.Length;

            int currentCost = _costManager != null ? _costManager.CurrentCost : 0;

            for (int i = 0; i < _effectButtons.Length; i++)
            {
                Button btn = _effectButtons[i];
                if (btn == null) continue;

                if (i < optionCount)
                {
                    btn.gameObject.SetActive(true);

                    bool hasEffect = _selectedKind == CardData.CardKind.Wall
                        ? _wallEffectOptions[i] != WallData.WallEffectType.None
                        : _towerEffectOptions[i] != TowerData.TowerEffectType.None;

                    int cost = _baseCraftCost + (hasEffect ? _effectExtraCost : 0);
                    bool canAfford = currentCost >= cost;

                    btn.interactable = canAfford;

                    TMP_Text label = btn.GetComponentInChildren<TMP_Text>();
                    if (label != null)
                    {
                        string effectName = _selectedKind == CardData.CardKind.Wall
                            ? _wallEffectOptions[i].ToString()
                            : _towerEffectOptions[i].ToString();
                        label.text = $"{effectName}\n({cost} 코스트)";
                    }

                    int capturedIndex = i;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnEffectSelected(capturedIndex));
                }
                else
                {
                    btn.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 3단계 버튼 클릭 시 호출된다. 선택된 효과로 카드를 제작한다.
        /// </summary>
        private void OnEffectSelected(int index)
        {
            if (_selectedKind == CardData.CardKind.Wall)
            {
                if (index >= _wallEffectOptions.Length) return;
                WallData.WallEffectType effect = _wallEffectOptions[index];
                bool hasEffect = effect != WallData.WallEffectType.None;
                int cost = _baseCraftCost + (hasEffect ? _effectExtraCost : 0);
                CraftWallCard(_selectedWallType, effect, cost);
            }
            else
            {
                if (index >= _towerEffectOptions.Length) return;
                TowerData.TowerEffectType effect = _towerEffectOptions[index];
                bool hasEffect = effect != TowerData.TowerEffectType.None;
                int cost = _baseCraftCost + (hasEffect ? _effectExtraCost : 0);
                CraftTowerCard(_selectedTowerType, effect, cost);
            }
        }

        // -------------------------------------------------------
        // 이전 단계 버튼 (Canvas 버튼 OnClick에 연결)
        // -------------------------------------------------------

        /// <summary>
        /// 2단계의 '이전 단계' 버튼 OnClick에 연결한다.
        /// </summary>
        public void GoBackToStep1()
        {
            _step = CraftingStep.SelectKind;
            ShowStep(_step);
        }

        /// <summary>
        /// 3단계의 '이전 단계' 버튼 OnClick에 연결한다.
        /// </summary>
        public void GoBackToStep2()
        {
            _step = CraftingStep.SelectType;
            ShowStep(_step);
        }

        // -------------------------------------------------------
        // 선택지 랜덤 준비
        // -------------------------------------------------------

        private void PrepareWallTypeOptions()
        {
            WallData.WallType[] all = (WallData.WallType[])System.Enum.GetValues(typeof(WallData.WallType));
            _wallTypeOptions = SampleArray(all, 3);
        }

        private void PrepareTowerTypeOptions()
        {
            _towerTypeOptions = (TowerData.TowerType[])System.Enum.GetValues(typeof(TowerData.TowerType));
        }

        /// <summary>
        /// None을 항상 첫 번째에 고정하고, 나머지 효과에서 3개를 랜덤 추출해 총 4개를 준비한다.
        /// </summary>
        private void PrepareWallEffectOptions()
        {
            List<WallData.WallEffectType> nonNone = new List<WallData.WallEffectType>();
            foreach (WallData.WallEffectType e in System.Enum.GetValues(typeof(WallData.WallEffectType)))
            {
                if (e != WallData.WallEffectType.None) nonNone.Add(e);
            }

            WallData.WallEffectType[] sampled = SampleArray(nonNone.ToArray(), 3);

            _wallEffectOptions = new WallData.WallEffectType[sampled.Length + 1];
            _wallEffectOptions[0] = WallData.WallEffectType.None;
            System.Array.Copy(sampled, 0, _wallEffectOptions, 1, sampled.Length);
        }

        /// <summary>
        /// None을 항상 첫 번째에 고정하고, 나머지 효과에서 3개를 랜덤 추출해 총 4개를 준비한다.
        /// </summary>
        private void PrepareTowerEffectOptions()
        {
            List<TowerData.TowerEffectType> nonNone = new List<TowerData.TowerEffectType>();
            foreach (TowerData.TowerEffectType e in System.Enum.GetValues(typeof(TowerData.TowerEffectType)))
            {
                if (e != TowerData.TowerEffectType.None) nonNone.Add(e);
            }

            TowerData.TowerEffectType[] sampled = SampleArray(nonNone.ToArray(), 3);

            _towerEffectOptions = new TowerData.TowerEffectType[sampled.Length + 1];
            _towerEffectOptions[0] = TowerData.TowerEffectType.None;
            System.Array.Copy(sampled, 0, _towerEffectOptions, 1, sampled.Length);
        }

        /// <summary>
        /// 배열에서 count개를 중복 없이 랜덤 추출한다. (Fisher-Yates 셔플)
        /// </summary>
        private T[] SampleArray<T>(T[] source, int count)
        {
            T[] copy = (T[])source.Clone();

            for (int i = copy.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (copy[i], copy[j]) = (copy[j], copy[i]);
            }

            count = Mathf.Min(count, copy.Length);
            T[] result = new T[count];
            System.Array.Copy(copy, result, count);
            return result;
        }

        // -------------------------------------------------------
        // 카드 제작 실행
        // -------------------------------------------------------

        /// <summary>
        /// 선택한 WallType과 WallEffectType으로 WallData와 CardData를 런타임 생성해
        /// 손패에 추가하고 코스트를 차감한다.
        /// </summary>
        private void CraftWallCard(WallData.WallType type, WallData.WallEffectType effect, int cost)
        {
            if (_costManager == null || !_costManager.SpendCost(cost)) return;

            WallData wallData = ScriptableObject.CreateInstance<WallData>();
            wallData.Initialize(type, effect, _wallAttackBonus, _wallRangeBonus, _wallAttackSpeedBonus);

            CardData cardData = ScriptableObject.CreateInstance<CardData>();
            cardData.Initialize(wallData, cost);

            _hand.AddCard(cardData);

            Debug.Log($"[CardCraftingUI] 벽 카드 제작 완료: {type} / {effect} (비용: {cost})");

            CloseModal();
        }

        /// <summary>
        /// 선택한 TowerType과 TowerEffectType으로 TowerData와 CardData를 런타임 생성해
        /// 손패에 추가하고 코스트를 차감한다.
        /// </summary>
        private void CraftTowerCard(TowerData.TowerType type, TowerData.TowerEffectType effect, int cost)
        {
            TowerData template = FindTowerTemplate(type);
            if (template == null)
            {
                Debug.LogWarning($"[CardCraftingUI] TowerType '{type}'에 해당하는 템플릿이 없습니다.");
                return;
            }

            if (_costManager == null || !_costManager.SpendCost(cost)) return;

            TowerData towerData = ScriptableObject.CreateInstance<TowerData>();
            towerData.Initialize(template, effect);

            CardData cardData = ScriptableObject.CreateInstance<CardData>();
            cardData.Initialize(towerData, cost);

            _hand.AddCard(cardData);

            Debug.Log($"[CardCraftingUI] 타워 카드 제작 완료: {type} / {effect} (비용: {cost})");

            CloseModal();
        }

        private TowerData FindTowerTemplate(TowerData.TowerType type)
        {
            foreach (TowerData template in _towerTemplates)
            {
                if (template != null && template.Type == type)
                    return template;
            }
            return null;
        }
    }
}
