using System.Collections.Generic;
using UnityEngine;

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
    ///   3단계: 효과 선택 (랜덤 3개 선택지)
    ///          - 벽: WallEffectType 4종 중 3개 랜덤
    ///          - 타워: TowerEffectType 4종 중 3개 랜덤
    ///
    /// 제작 확정 시 CardData를 런타임으로 생성해 Hand에 추가하고 코스트를 차감한다.
    /// 코스트 부족 시 해당 선택지 버튼이 비활성화된다.
    ///
    /// 주의: 모달 오픈 중 다른 UI(WallPlacer, TowerPlacer)를 함께 조작하지 않도록 한다.
    ///       페이즈 기반 잠금은 7단계 GameManager에서 처리한다.
    /// </summary>
    public class CardCraftingUI : MonoBehaviour
    {
        // -------------------------------------------------------
        // 제작 단계 열거형
        // -------------------------------------------------------

        private enum CraftingStep { None, SelectKind, SelectType, SelectEffect }

        // -------------------------------------------------------
        // Inspector 노출 필드
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
        [Tooltip("AttackBoost 효과 적용 시 공격력 증가량")]
        [SerializeField] private float _wallAttackBonus = 5f;

        [Tooltip("RangeBoost 효과 적용 시 사거리 증가량")]
        [SerializeField] private float _wallRangeBonus = 1f;

        [Tooltip("AttackSpeedBoost 효과 적용 시 공격 속도 증가량")]
        [SerializeField] private float _wallAttackSpeedBonus = 0.5f;

        [Header("Modal UI Settings")]
        [Tooltip("모달 패널 너비 (픽셀)")]
        [SerializeField] private float _modalWidth = 420f;

        [Tooltip("모달 패널 높이 (픽셀)")]
        [SerializeField] private float _modalHeight = 380f;

        // -------------------------------------------------------
        // 내부 상태
        // -------------------------------------------------------

        private CraftingStep _step = CraftingStep.None;

        /// <summary>1단계에서 선택한 카드 종류</summary>
        private CardData.CardKind _selectedKind;

        /// <summary>2단계에서 표시할 WallType 선택지 (벽 카드 선택 시 랜덤 추출)</summary>
        private WallData.WallType[] _wallTypeOptions;

        /// <summary>2단계에서 표시할 TowerType 선택지 (타워 카드 선택 시 전체)</summary>
        private TowerData.TowerType[] _towerTypeOptions;

        /// <summary>2단계에서 선택한 WallType</summary>
        private WallData.WallType _selectedWallType;

        /// <summary>2단계에서 선택한 TowerType</summary>
        private TowerData.TowerType _selectedTowerType;

        /// <summary>3단계에서 표시할 WallEffectType 선택지 (랜덤 추출)</summary>
        private WallData.WallEffectType[] _wallEffectOptions;

        /// <summary>3단계에서 표시할 TowerEffectType 선택지 (랜덤 추출)</summary>
        private TowerData.TowerEffectType[] _towerEffectOptions;

        // -------------------------------------------------------
        // OnGUI 진입점
        // -------------------------------------------------------

        private void OnGUI()
        {
            DrawCostDisplay();

            if (_step == CraftingStep.None)
            {
                DrawOpenModalButton();
            }
            else
            {
                DrawOverlay();
                DrawModal();
            }
        }

        // -------------------------------------------------------
        // 항상 표시되는 UI
        // -------------------------------------------------------

        /// <summary>
        /// 화면 우측 상단 TowerPlacer 패널 바로 아래에 코스트와 카드 제작 버튼을 그린다.
        /// TowerPlacer UI 가 (Screen.width-200, 10, 190, 200) 을 차지하므로
        /// 그 아래 (Screen.width-200, 220) 에 배치해 겹침을 피한다.
        /// </summary>
        private void DrawCostDisplay()
        {
            int cost = _costManager != null ? _costManager.CurrentCost : 0;
            float x = Screen.width - 200f;
            GUILayout.BeginArea(new Rect(x, 220f, 190f, 75f));
            GUILayout.Label($"보유 코스트: {cost}");
            GUILayout.EndArea();
        }

        /// <summary>
        /// 카드 제작 모달을 여는 버튼을 그린다.
        /// </summary>
        private void DrawOpenModalButton()
        {
            float x = Screen.width - 200f;
            GUILayout.BeginArea(new Rect(x, 245f, 190f, 40f));
            if (GUILayout.Button("카드 제작", GUILayout.Height(32f)))
            {
                OpenModal();
            }
            GUILayout.EndArea();
        }

        // -------------------------------------------------------
        // 모달 UI
        // -------------------------------------------------------

        /// <summary>
        /// 모달 오픈 시 1단계 상태로 초기화한다.
        /// </summary>
        private void OpenModal()
        {
            _step = CraftingStep.SelectKind;
        }

        /// <summary>
        /// 화면 전체에 반투명 어두운 오버레이를 그린다.
        /// 모달이 열려 있음을 시각적으로 강조한다.
        /// </summary>
        private void DrawOverlay()
        {
            Color prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.45f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = prev;
        }

        /// <summary>
        /// 화면 중앙에 제작 모달 패널을 그린다.
        /// 매 OnGUI 호출마다 화면 크기 기반으로 위치를 재계산한다.
        /// </summary>
        private void DrawModal()
        {
            float x = (Screen.width - _modalWidth) * 0.5f;
            float y = (Screen.height - _modalHeight) * 0.5f;
            Rect modalRect = new Rect(x, y, _modalWidth, _modalHeight);

            GUI.Box(modalRect, "[ 카드 제작 ]");

            // 패널 내부 여백 적용
            Rect innerRect = new Rect(modalRect.x + 15f, modalRect.y + 30f,
                                      modalRect.width - 30f, modalRect.height - 45f);
            GUILayout.BeginArea(innerRect);

            switch (_step)
            {
                case CraftingStep.SelectKind:
                    DrawSelectKindStep();
                    break;
                case CraftingStep.SelectType:
                    DrawSelectTypeStep();
                    break;
                case CraftingStep.SelectEffect:
                    DrawSelectEffectStep();
                    break;
            }

            GUILayout.Space(12f);

            if (GUILayout.Button("닫기", GUILayout.Height(28f)))
            {
                CloseModal();
            }

            GUILayout.EndArea();
        }

        // -------------------------------------------------------
        // 1단계: 카드 종류 선택
        // -------------------------------------------------------

        /// <summary>
        /// 1단계: 벽 카드 또는 타워 카드를 선택한다.
        /// </summary>
        private void DrawSelectKindStep()
        {
            GUILayout.Label("1단계: 카드 종류를 선택하세요.");
            GUILayout.Space(10f);

            if (GUILayout.Button("벽 카드", GUILayout.Height(42f)))
            {
                _selectedKind = CardData.CardKind.Wall;
                PrepareWallTypeOptions();
                _step = CraftingStep.SelectType;
            }

            GUILayout.Space(6f);

            if (GUILayout.Button("타워 카드", GUILayout.Height(42f)))
            {
                _selectedKind = CardData.CardKind.Tower;
                PrepareTowerTypeOptions();
                _step = CraftingStep.SelectType;
            }
        }

        // -------------------------------------------------------
        // 2단계: 세부 종류 선택
        // -------------------------------------------------------

        /// <summary>
        /// 2단계: 랜덤으로 제시된 3개의 종류 중 하나를 선택한다.
        /// </summary>
        private void DrawSelectTypeStep()
        {
            if (_selectedKind == CardData.CardKind.Wall)
            {
                GUILayout.Label("2단계: 벽 종류를 선택하세요. (랜덤 3종)");
                GUILayout.Space(8f);

                foreach (WallData.WallType type in _wallTypeOptions)
                {
                    if (GUILayout.Button(type.ToString(), GUILayout.Height(36f)))
                    {
                        _selectedWallType = type;
                        PrepareWallEffectOptions();
                        _step = CraftingStep.SelectEffect;
                    }
                }
            }
            else
            {
                GUILayout.Label("2단계: 타워 종류를 선택하세요.");
                GUILayout.Space(8f);

                foreach (TowerData.TowerType type in _towerTypeOptions)
                {
                    if (GUILayout.Button(type.ToString(), GUILayout.Height(36f)))
                    {
                        _selectedTowerType = type;
                        PrepareTowerEffectOptions();
                        _step = CraftingStep.SelectEffect;
                    }
                }
            }

            GUILayout.Space(10f);

            if (GUILayout.Button("← 이전 단계", GUILayout.Height(28f)))
            {
                _step = CraftingStep.SelectKind;
            }
        }

        // -------------------------------------------------------
        // 3단계: 효과 선택
        // -------------------------------------------------------

        /// <summary>
        /// 3단계: 랜덤으로 제시된 3개의 효과 중 하나를 선택해 카드를 제작한다.
        /// 코스트가 부족한 선택지는 버튼이 비활성화된다.
        /// </summary>
        private void DrawSelectEffectStep()
        {
            if (_selectedKind == CardData.CardKind.Wall)
            {
                GUILayout.Label($"3단계: 효과를 선택하세요. ({_selectedWallType} 벽)");
                GUILayout.Space(8f);

                foreach (WallData.WallEffectType effect in _wallEffectOptions)
                {
                    bool hasEffect = effect != WallData.WallEffectType.None;
                    int cost = _baseCraftCost + (hasEffect ? _effectExtraCost : 0);
                    DrawEffectButton(effect.ToString(), cost, () =>
                    {
                        CraftWallCard(_selectedWallType, effect, cost);
                    });
                }
            }
            else
            {
                GUILayout.Label($"3단계: 효과를 선택하세요. ({_selectedTowerType} 타워)");
                GUILayout.Space(8f);

                foreach (TowerData.TowerEffectType effect in _towerEffectOptions)
                {
                    bool hasEffect = effect != TowerData.TowerEffectType.None;
                    int cost = _baseCraftCost + (hasEffect ? _effectExtraCost : 0);
                    DrawEffectButton(effect.ToString(), cost, () =>
                    {
                        CraftTowerCard(_selectedTowerType, effect, cost);
                    });
                }
            }

            GUILayout.Space(10f);

            if (GUILayout.Button("← 이전 단계", GUILayout.Height(28f)))
            {
                _step = CraftingStep.SelectType;
            }
        }

        /// <summary>
        /// 효과 선택 버튼 하나를 그린다.
        /// 코스트 부족 시 비활성화하고, 클릭 시 onConfirm 콜백을 호출한다.
        /// </summary>
        private void DrawEffectButton(string effectLabel, int cost, System.Action onConfirm)
        {
            int currentCost = _costManager != null ? _costManager.CurrentCost : 0;
            bool canAfford = currentCost >= cost;

            GUI.enabled = canAfford;
            string label = $"{effectLabel}  ({cost} 코스트)";
            if (GUILayout.Button(label, GUILayout.Height(36f)))
            {
                onConfirm?.Invoke();
            }
            GUI.enabled = true;
        }

        // -------------------------------------------------------
        // 선택지 랜덤 준비
        // -------------------------------------------------------

        /// <summary>
        /// WallType 전체(7종)에서 3개를 랜덤 추출해 2단계 선택지로 준비한다.
        /// </summary>
        private void PrepareWallTypeOptions()
        {
            WallData.WallType[] all = (WallData.WallType[])System.Enum.GetValues(typeof(WallData.WallType));
            _wallTypeOptions = SampleArray(all, 3);
        }

        /// <summary>
        /// TowerType 전체(3종)를 그대로 2단계 선택지로 준비한다.
        /// </summary>
        private void PrepareTowerTypeOptions()
        {
            _towerTypeOptions = (TowerData.TowerType[])System.Enum.GetValues(typeof(TowerData.TowerType));
        }

        /// <summary>
        /// 3단계 벽 효과 선택지를 준비한다.
        /// None은 항상 첫 번째에 고정하고, 나머지 효과(AttackBoost 등)에서 3개를 랜덤 추출해 총 4개를 제공한다.
        /// </summary>
        private void PrepareWallEffectOptions()
        {
            List<WallData.WallEffectType> nonNone = new List<WallData.WallEffectType>();
            foreach (WallData.WallEffectType e in System.Enum.GetValues(typeof(WallData.WallEffectType)))
            {
                if (e != WallData.WallEffectType.None) nonNone.Add(e);
            }

            WallData.WallEffectType[] sampled = SampleArray(nonNone.ToArray(), 3);

            // None을 맨 앞에 고정, 그 뒤에 랜덤 3개
            _wallEffectOptions = new WallData.WallEffectType[sampled.Length + 1];
            _wallEffectOptions[0] = WallData.WallEffectType.None;
            System.Array.Copy(sampled, 0, _wallEffectOptions, 1, sampled.Length);
        }

        /// <summary>
        /// 3단계 타워 효과 선택지를 준비한다.
        /// None은 항상 첫 번째에 고정하고, 나머지 효과(ExtraDamage 등)에서 3개를 랜덤 추출해 총 4개를 제공한다.
        /// </summary>
        private void PrepareTowerEffectOptions()
        {
            List<TowerData.TowerEffectType> nonNone = new List<TowerData.TowerEffectType>();
            foreach (TowerData.TowerEffectType e in System.Enum.GetValues(typeof(TowerData.TowerEffectType)))
            {
                if (e != TowerData.TowerEffectType.None) nonNone.Add(e);
            }

            TowerData.TowerEffectType[] sampled = SampleArray(nonNone.ToArray(), 3);

            // None을 맨 앞에 고정, 그 뒤에 랜덤 3개
            _towerEffectOptions = new TowerData.TowerEffectType[sampled.Length + 1];
            _towerEffectOptions[0] = TowerData.TowerEffectType.None;
            System.Array.Copy(sampled, 0, _towerEffectOptions, 1, sampled.Length);
        }

        /// <summary>
        /// 배열에서 count개를 중복 없이 랜덤 추출한다. (Fisher-Yates 셔플)
        /// count가 배열 크기보다 크면 전체를 반환한다.
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
            cardData.Initialize(wallData);

            _hand.AddCard(cardData);

            Debug.Log($"[CardCraftingUI] 벽 카드 제작 완료: {type} / {effect} (비용: {cost})");

            CloseModal();
        }

        /// <summary>
        /// 선택한 TowerType과 TowerEffectType으로 TowerData와 CardData를 런타임 생성해
        /// 손패에 추가하고 코스트를 차감한다.
        /// TowerData 스탯은 해당 타입의 템플릿에서 복사한다.
        /// </summary>
        private void CraftTowerCard(TowerData.TowerType type, TowerData.TowerEffectType effect, int cost)
        {
            TowerData template = FindTowerTemplate(type);
            if (template == null)
            {
                Debug.LogWarning($"[CardCraftingUI] TowerType '{type}'에 해당하는 템플릿이 없습니다. Tower Templates 리스트를 확인하세요.");
                return;
            }

            if (_costManager == null || !_costManager.SpendCost(cost)) return;

            TowerData towerData = ScriptableObject.CreateInstance<TowerData>();
            towerData.Initialize(template, effect);

            CardData cardData = ScriptableObject.CreateInstance<CardData>();
            cardData.Initialize(towerData);

            _hand.AddCard(cardData);

            Debug.Log($"[CardCraftingUI] 타워 카드 제작 완료: {type} / {effect} (비용: {cost})");

            CloseModal();
        }

        /// <summary>
        /// _towerTemplates 리스트에서 지정한 TowerType에 해당하는 템플릿을 반환한다.
        /// 일치하는 항목이 없으면 null을 반환한다.
        /// </summary>
        private TowerData FindTowerTemplate(TowerData.TowerType type)
        {
            foreach (TowerData template in _towerTemplates)
            {
                if (template != null && template.Type == type)
                    return template;
            }
            return null;
        }

        /// <summary>
        /// 모달을 닫고 초기 상태로 돌아간다.
        /// </summary>
        private void CloseModal()
        {
            _step = CraftingStep.None;
        }
    }
}
