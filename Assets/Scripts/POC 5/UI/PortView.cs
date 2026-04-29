using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using POC5.Graph;

namespace POC5.UI
{
    /// <summary>
    /// 포트 하나의 UI 행을 담당한다.
    /// 프리팹(PortRowInput / PortRowOutput)에 이 컴포넌트를 붙이고
    /// Inspector에서 PortCircle 버튼과 PortLabel 텍스트를 연결한다.
    ///
    /// 원형 버튼의 위치·색상은 프리팹 자체에서 설정하므로
    /// 이 스크립트는 데이터 바인딩과 클릭 이벤트만 처리한다.
    /// </summary>
    public class PortView : MonoBehaviour
    {
        [Tooltip("연결 시작 버튼. 프리팹의 PortCircle 오브젝트를 연결한다.")]
        [SerializeField] private Button _portButton;

        [Tooltip("자원명과 잔량을 표시하는 텍스트. 프리팹의 PortLabel을 연결한다.")]
        [SerializeField] private TextMeshProUGUI _portLabel;

        private Port _port;

        /// <summary>이 뷰가 표시하는 포트 데이터.</summary>
        public Port Port => _port;

        /// <summary>
        /// 포트 원형 버튼이 클릭됐을 때 발생하는 이벤트.
        /// 4단계에서 PortConnectHandler가 이 이벤트를 구독해 연결선을 그린다.
        /// </summary>
        public event Action<PortView> OnPortButtonClicked;

        /// <summary>
        /// 포트 데이터를 바인딩하고 버튼 클릭 이벤트를 등록한다.
        /// FacilityNodeView가 포트 행을 인스턴스화한 직후 호출한다.
        /// </summary>
        public void Initialize(Port port)
        {
            _port = port;
            _portButton.onClick.AddListener(() => OnPortButtonClicked?.Invoke(this));
            RefreshAmount();
        }

        /// <summary>
        /// 포트의 현재 잔량을 텍스트에 반영한다.
        /// FacilityNodeView의 타이머에서 주기적으로 호출된다.
        /// </summary>
        public void RefreshAmount()
        {
            if (_portLabel != null)
                _portLabel.text = $"{_port.ResourceType}  {_port.CurrentAmount}/{_port.Capacity}";
        }
    }
}
