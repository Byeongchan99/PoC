using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using POC5.Runtime;
using POC5.Graph;

namespace POC5.UI
{
    /// <summary>
    /// 설비 노드 카드 전체의 UI를 담당한다.
    /// 헤더(레벨·이름), 아이콘, 입출력 포트 행, 업그레이드 버튼을 코드로 생성한다.
    ///
    /// 카드 구조 (위→아래):
    ///   [헤더: Lv.1 | 설비명]
    ///   [아이콘]
    ///   [입력 포트 행 × n] ← 빨간 원이 카드 왼쪽 밖으로 튀어나옴
    ///   [출력 포트 행 × n] ← 초록 원이 카드 오른쪽 밖으로 튀어나옴
    ///   [업그레이드 버튼 (비활성)]
    ///
    /// VerticalLayoutGroup + ContentSizeFitter를 사용해 포트 수에 맞게 카드 높이가 자동 조절된다.
    /// </summary>
    public class FacilityNodeView : MonoBehaviour
    {
        // 카드 레이아웃 고정 수치 (픽셀)
        private const float CardWidth     = 180f;
        private const float HeaderHeight  = 36f;
        private const float IconHeight    = 60f;
        private const float UpgradeHeight = 40f;

        // 카드 색상 팔레트
        private static readonly Color CardBgColor   = new Color(0.12f, 0.16f, 0.24f, 1f);
        private static readonly Color HeaderBgColor = new Color(0.18f, 0.23f, 0.32f, 1f);
        private static readonly Color IconBgColor   = new Color(0.10f, 0.14f, 0.20f, 1f);
        private static readonly Color UpgradeBgColor= new Color(0.08f, 0.10f, 0.16f, 1f);

        [Tooltip("포트 잔량 텍스트를 갱신하는 주기 (초). 낮을수록 자주 갱신된다.")]
        [SerializeField] private float _refreshInterval = 0.5f;

        private FacilityNode _facilityNode;
        private readonly List<PortView> _portViews = new List<PortView>();
        private float _refreshTimer;

        /// <summary>
        /// 이 카드에 포함된 모든 PortView.
        /// 4단계에서 PortConnectHandler가 포트 버튼에 접근할 때 사용한다.
        /// </summary>
        public IReadOnlyList<PortView> PortViews => _portViews;

        /// <summary>
        /// 설비 데이터를 받아 카드 UI 전체를 구성한다.
        /// GameSceneManager에서 AddComponent 직후 호출해야 한다.
        /// </summary>
        public void Initialize(FacilityNode facilityNode)
        {
            _facilityNode = facilityNode;
            SetupCardRoot();
            BuildContent();
        }

        private void Update()
        {
            // 매 프레임이 아닌 일정 간격으로 잔량을 갱신해 불필요한 텍스트 갱신을 줄인다
            _refreshTimer -= Time.deltaTime;
            if (_refreshTimer > 0f) return;
            _refreshTimer = _refreshInterval;
            RefreshPortAmounts();
        }

        /// <summary>모든 포트 뷰의 잔량 텍스트를 즉시 갱신한다.</summary>
        public void RefreshPortAmounts()
        {
            foreach (var pv in _portViews)
                pv.RefreshAmount();
        }

        /// <summary>
        /// 루트 RectTransform에 카드 배경 이미지, 세로 레이아웃 그룹,
        /// 높이 자동 조절 컴포넌트를 추가한다.
        /// </summary>
        private void SetupCardRoot()
        {
            var rt = GetComponent<RectTransform>();
            // 너비 고정. 높이는 ContentSizeFitter가 자식 합계에 맞춰 자동으로 설정한다
            rt.sizeDelta = new Vector2(CardWidth, 0f);

            // 카드 배경 이미지. Raycast Target = true 이므로 드래그 히트박스도 겸한다
            var bg = gameObject.AddComponent<Image>();
            bg.color = CardBgColor;

            // 자식 요소를 위→아래로 나열하는 레이아웃 그룹
            var vlg = gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(0, 0, 0, 0);
            vlg.spacing = 2f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth      = true;
            vlg.childControlHeight     = false;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;

            // 자식 높이 합계 + 간격에 맞춰 카드 높이를 자동으로 조절한다
            var csf = gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        /// <summary>
        /// 카드 내부 요소를 순서대로 생성한다.
        /// 헤더 → 아이콘 → 포트 행 → 업그레이드 버튼
        /// </summary>
        private void BuildContent()
        {
            var data = _facilityNode.GraphNode.Data;

            BuildHeader(data.DisplayName);
            BuildIcon(data.Icon);
            BuildPortRows();
            BuildUpgradeSection(data.PurchasePrice);
        }

        /// <summary>
        /// 레벨 텍스트와 설비 이름을 가로로 배치하는 헤더 패널을 생성한다.
        /// </summary>
        private void BuildHeader(string displayName)
        {
            var panel = CreatePanel("Header", transform, HeaderHeight, HeaderBgColor);

            var hlg = panel.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(8, 8, 0, 0);
            hlg.spacing = 4f;
            hlg.childAlignment       = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = true;

            // 레벨 텍스트: 고정 너비로 왼쪽에 위치한다
            var levelGo = CreateTextObject("LevelText", panel.transform, "Lv.1", 10f,
                TextAlignmentOptions.Midline);
            levelGo.AddComponent<LayoutElement>().preferredWidth = 32f;

            // 설비명 텍스트: 남은 공간을 모두 차지한다
            var nameGo = CreateTextObject("NameText", panel.transform, displayName, 12f,
                TextAlignmentOptions.Midline);
            nameGo.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }

        /// <summary>
        /// 설비 아이콘을 중앙에 표시하는 패널을 생성한다.
        /// 아이콘 스프라이트가 없으면 회색 사각형 플레이스홀더를 사용한다.
        /// </summary>
        private void BuildIcon(Sprite icon)
        {
            var panel = CreatePanel("IconPanel", transform, IconHeight, IconBgColor);

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(panel.transform, false);

            var iconRT = iconGo.GetComponent<RectTransform>();
            iconRT.anchorMin = iconRT.anchorMax = new Vector2(0.5f, 0.5f);
            iconRT.pivot = new Vector2(0.5f, 0.5f);
            // 패널 높이보다 20px 작게 해 여백을 확보한다
            iconRT.sizeDelta = new Vector2(IconHeight - 20f, IconHeight - 20f);
            iconRT.anchoredPosition = Vector2.zero;

            var img = iconGo.GetComponent<Image>();
            if (icon != null)
            {
                img.sprite = icon;
                img.preserveAspect = true;
            }
            else
            {
                // 아이콘이 없을 때 회색 플레이스홀더를 표시한다
                img.color = new Color(0.4f, 0.4f, 0.45f, 1f);
            }
        }

        /// <summary>
        /// 그래프 노드의 입력 포트를 먼저, 출력 포트를 나중에 각각 PortView 행으로 생성한다.
        /// </summary>
        private void BuildPortRows()
        {
            var graphNode = _facilityNode.GraphNode;

            foreach (var port in graphNode.InputPorts)
                AddPortRow(port);

            foreach (var port in graphNode.OutputPorts)
                AddPortRow(port);
        }

        /// <summary>
        /// 포트 하나에 대한 PortView 행을 생성하고 목록에 등록한다.
        /// </summary>
        private void AddPortRow(Port port)
        {
            var rowGo = new GameObject("PortRow", typeof(RectTransform));
            rowGo.transform.SetParent(transform, false);

            var portView = rowGo.AddComponent<PortView>();
            portView.Initialize(port);
            _portViews.Add(portView);
        }

        /// <summary>
        /// 비활성화된 업그레이드 버튼이 있는 하단 섹션을 생성한다.
        /// 버튼은 6단계에서 실제 구매 로직이 연결될 때 활성화된다.
        /// </summary>
        private void BuildUpgradeSection(int purchasePrice)
        {
            var panel = CreatePanel("UpgradePanel", transform, UpgradeHeight, UpgradeBgColor);

            var btnGo = new GameObject("UpgradeBtn",
                typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(panel.transform, false);

            var btnRT = btnGo.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.08f, 0.15f);
            btnRT.anchorMax = new Vector2(0.92f, 0.85f);
            btnRT.offsetMin = btnRT.offsetMax = Vector2.zero;

            btnGo.GetComponent<Image>().color = new Color(0.22f, 0.22f, 0.25f, 1f);

            // 현재 단계에서는 비활성 상태. 6단계에서 활성화된다
            btnGo.GetComponent<Button>().interactable = false;

            var labelGo = CreateTextObject("BtnLabel", btnGo.transform,
                $"업그레이드  {purchasePrice}G", 10f, TextAlignmentOptions.Midline);
            var labelRT = labelGo.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = labelRT.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// 배경색이 있는 패널 GameObject를 생성하고 LayoutElement로 높이를 지정한다.
        /// </summary>
        private static GameObject CreatePanel(string name, Transform parent,
            float preferredHeight, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            go.AddComponent<LayoutElement>().preferredHeight = preferredHeight;
            return go;
        }

        /// <summary>
        /// TextMeshProUGUI 컴포넌트가 붙은 GameObject를 생성한다.
        /// </summary>
        private static GameObject CreateTextObject(string name, Transform parent,
            string text, float fontSize, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.alignment = alignment;
            return go;
        }
    }
}
