using System;
using UnityEngine;
using UnityEngine.EventSystems;
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
    /// 드래그 이벤트 처리 방식:
    ///   PortCircle(Button)이 자식에 있을 때, PortCircle에서 드래그를 시작하면
    ///   Unity EventSystem이 IBeginDragHandler를 구현한 가장 가까운 부모(이 컴포넌트)를
    ///   찾아 호출한다. PortConnectHandler가 이 이벤트를 구독해 연결선을 그린다.
    /// </summary>
    public class PortView : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Tooltip("연결 시작 버튼. 프리팹의 PortCircle 오브젝트를 연결한다.")]
        [SerializeField] private Button _portButton;

        [Tooltip("자원명과 잔량을 표시하는 텍스트. 프리팹의 PortLabel을 연결한다.")]
        [SerializeField] private TextMeshProUGUI _portLabel;

        private Port _port;

        /// <summary>이 뷰가 표시하는 포트 데이터.</summary>
        public Port Port => _port;

        /// <summary>포트 원형 버튼의 세계 좌표. PortConnectHandler가 연결선 끝점으로 사용한다.</summary>
        public Vector3 PortWorldPosition => _portButton.transform.position;

        /// <summary>포트 원형 버튼이 클릭됐을 때 발생하는 이벤트.</summary>
        public event Action<PortView> OnPortButtonClicked;

        /// <summary>포트 위에서 드래그를 시작했을 때 발생하는 이벤트.</summary>
        public event Action<PortView, PointerEventData> OnPortDragBegin;

        /// <summary>드래그 중 매 프레임 발생하는 이벤트.</summary>
        public event Action<PortView, PointerEventData> OnPortDragUpdate;

        /// <summary>드래그를 끝냈을 때 발생하는 이벤트.</summary>
        public event Action<PortView, PointerEventData> OnPortDragEnd;

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

        /// <summary>드래그 시작. PortCircle에서 발생한 이벤트가 이 컴포넌트로 버블링된다.</summary>
        public void OnBeginDrag(PointerEventData eventData)
            => OnPortDragBegin?.Invoke(this, eventData);

        /// <summary>드래그 중 매 프레임 호출된다.</summary>
        public void OnDrag(PointerEventData eventData)
            => OnPortDragUpdate?.Invoke(this, eventData);

        /// <summary>드래그 종료. 마우스를 뗐을 때 호출된다.</summary>
        public void OnEndDrag(PointerEventData eventData)
            => OnPortDragEnd?.Invoke(this, eventData);
    }
}
