using System.Collections.Generic;
using POC5.Data;

namespace POC5.Graph
{
    /// <summary>
    /// 모든 그래프 노드의 추상 기반 클래스.
    /// MonoBehaviour에 의존하지 않는 순수 C# 클래스로 게임 로직만 담당한다.
    /// 서브클래스에서 OnTick()을 구현해 자원 생산/소비 로직을 정의한다.
    /// </summary>
    public abstract class NodeBase
    {
        /// <summary>노드를 식별하는 고유 ID.</summary>
        public string NodeId { get; }

        protected readonly List<Port> _inputPorts = new List<Port>();
        protected readonly List<Port> _outputPorts = new List<Port>();

        /// <summary>이 노드의 입력 포트 목록 (읽기 전용).</summary>
        public IReadOnlyList<Port> InputPorts => _inputPorts;

        /// <summary>이 노드의 출력 포트 목록 (읽기 전용).</summary>
        public IReadOnlyList<Port> OutputPorts => _outputPorts;

        protected NodeBase(string nodeId)
        {
            NodeId = nodeId;
        }

        /// <summary>
        /// 매 틱마다 NodeGraph에 의해 호출된다.
        /// 서브클래스에서 자원 생산/소비 로직을 구현한다.
        /// </summary>
        public abstract void OnTick();

        /// <summary>
        /// 특정 자원 종류의 입력 포트를 반환한다. 없으면 null.
        /// </summary>
        public Port GetInputPort(ResourceType resourceType)
        {
            foreach (var port in _inputPorts)
                if (port.ResourceType == resourceType) return port;
            return null;
        }

        /// <summary>
        /// 특정 자원 종류의 출력 포트를 반환한다. 없으면 null.
        /// </summary>
        public Port GetOutputPort(ResourceType resourceType)
        {
            foreach (var port in _outputPorts)
                if (port.ResourceType == resourceType) return port;
            return null;
        }
    }
}
