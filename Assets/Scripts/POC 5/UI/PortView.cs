using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using POC5.Graph;

namespace POC5.UI
{
    /// <summary>
    /// 포트 하나의 UI 행을 담당한다.
    /// 자원 타입에 맞는 원형 버튼(클릭으로 연결 시작)과 자원명·잔량 텍스트로 구성된다.
    ///
    /// 원형 버튼 배치 규칙:
    ///   입력 포트 — 빨간 원이 카드 왼쪽 경계 밖으로 절반 튀어나온다.
    ///   출력 포트 — 초록 원이 카드 오른쪽 경계 밖으로 절반 튀어나온다.
    ///
    /// 카드에 RectMask2D를 붙이지 않아야 원이 경계 밖으로 보인다.
    /// </summary>
    public class PortView : MonoBehaviour
    {
        /// <summary>원형 버튼의 지름 (픽셀).</summary>
        private const float CircleDiameter = 22f;

        /// <summary>이 행의 고정 높이 (픽셀). LayoutElement로 지정한다.</summary>
        private const float RowHeight = 26f;

        private Port _port;
        private TextMeshProUGUI _portLabel;

        /// <summary>이 뷰가 표시하는 포트 데이터.</summary>
        public Port Port => _port;

        /// <summary>
        /// 포트 원형 버튼이 클릭됐을 때 발생하는 이벤트.
        /// 4단계에서 PortConnectHandler가 이 이벤트를 구독해 연결선을 그린다.
        /// </summary>
        public event Action<PortView> OnPortButtonClicked;

        /// <summary>
        /// 포트 데이터를 받아 UI를 구성한다.
        /// FacilityNodeView.Initialize() 내부에서 호출된다.
        /// </summary>
        public void Initialize(Port port)
        {
            _port = port;

            // VerticalLayoutGroup이 이 행의 높이를 올바르게 처리하도록 LayoutElement를 추가한다
            var le = gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = RowHeight;

            BuildRow();
        }

        /// <summary>
        /// 포트의 현재 잔량을 텍스트에 반영한다.
        /// FacilityNodeView의 타이머에서 주기적으로 호출된다.
        /// </summary>
        public void RefreshAmount()
        {
            if (_portLabel != null)
                _portLabel.text = BuildLabelText();
        }

        /// <summary>
        /// "자원명  현재량/최대량" 형식의 텍스트를 반환한다.
        /// </summary>
        private string BuildLabelText()
            => $"{_port.ResourceType}  {_port.CurrentAmount}/{_port.Capacity}";

        /// <summary>
        /// 포트 방향에 따라 원형 버튼과 텍스트 레이블을 배치한다.
        /// </summary>
        private void BuildRow()
        {
            bool isInput = _port.Direction == PortDirection.Input;
            Color circleColor = isInput
                ? new Color(1f, 0.3f, 0.3f)     // 입력 포트: 빨간색
                : new Color(0.3f, 0.85f, 0.3f);  // 출력 포트: 초록색

            BuildCircleButton(isInput, circleColor);
            BuildLabel(isInput);
        }

        /// <summary>
        /// 원형 버튼을 생성하고 카드 경계 밖으로 절반이 나오도록 배치한다.
        /// anchoredPosition.x 를 ±반지름으로 설정해 절반이 카드 테두리 밖에 위치하게 한다.
        /// </summary>
        private void BuildCircleButton(bool isInput, Color color)
        {
            var go = new GameObject("PortCircle",
                typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(transform, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(CircleDiameter, CircleDiameter);
            rt.pivot = new Vector2(0.5f, 0.5f);

            // 입력: 왼쪽(x=0) 앵커에서 -반지름 → 원의 절반이 카드 왼쪽 밖으로 나온다
            // 출력: 오른쪽(x=1) 앵커에서 +반지름 → 원의 절반이 카드 오른쪽 밖으로 나온다
            rt.anchorMin = rt.anchorMax = isInput
                ? new Vector2(0f, 0.5f)
                : new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(
                isInput ? -(CircleDiameter * 0.5f) : CircleDiameter * 0.5f, 0f);

            go.GetComponent<Image>().color = color;

            // 클릭 시 이벤트를 발행한다. 4단계에서 연결 핸들러가 구독한다
            go.GetComponent<Button>().onClick
                .AddListener(() => OnPortButtonClicked?.Invoke(this));
        }

        /// <summary>
        /// 자원명과 잔량을 표시하는 텍스트 레이블을 생성한다.
        /// 원형 버튼과 겹치지 않도록 해당 방향 안쪽에 여백을 준다.
        /// </summary>
        private void BuildLabel(bool isInput)
        {
            var go = new GameObject("PortLabel",
                typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;

            // 원형 버튼 반지름 + 4px 여백만큼 해당 쪽 안으로 밀어 겹침을 방지한다
            float inset = CircleDiameter * 0.5f + 4f;
            rt.offsetMin = new Vector2(isInput ? inset : 4f, 0f);
            rt.offsetMax = new Vector2(isInput ? -4f : -inset, 0f);

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = BuildLabelText();
            tmp.fontSize = 11f;
            tmp.color = Color.white;
            tmp.alignment = isInput
                ? TextAlignmentOptions.MidlineLeft
                : TextAlignmentOptions.MidlineRight;

            _portLabel = tmp;
        }
    }
}
