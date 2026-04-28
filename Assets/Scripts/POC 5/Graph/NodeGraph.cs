using System.Collections.Generic;
using UnityEngine;

namespace POC5.Graph
{
    /// <summary>
    /// 전체 노드 그래프의 상태를 관리하는 클래스.
    /// 노드와 연결선 목록을 보유하고, 매 틱 자원 생산 및 흐름을 처리한다.
    ///
    /// 사용법:
    ///   1. AddNode()로 노드를 등록한다.
    ///   2. TryConnect()로 포트 간 연결을 만든다.
    ///   3. ResourceFlowSystem이 주기적으로 Tick()을 호출한다.
    /// </summary>
    public class NodeGraph
    {
        private readonly List<NodeBase> _nodes = new List<NodeBase>();
        private readonly List<Connection> _connections = new List<Connection>();

        public IReadOnlyList<NodeBase> Nodes => _nodes;
        public IReadOnlyList<Connection> Connections => _connections;

        /// <summary>
        /// 노드를 그래프에 등록한다. 이미 등록된 노드는 중복 추가되지 않는다.
        /// </summary>
        public void AddNode(NodeBase node)
        {
            if (!_nodes.Contains(node))
                _nodes.Add(node);
        }

        /// <summary>
        /// 노드를 그래프에서 제거한다.
        /// 이 노드의 포트가 관여된 모든 Connection도 함께 제거된다.
        /// </summary>
        public void RemoveNode(NodeBase node)
        {
            _nodes.Remove(node);
            // LINQ 없이 직접 순회해 노드 포트가 관여된 연결을 제거한다
            _connections.RemoveAll(c => IsPortInNode(c.InputPort, node) || IsPortInNode(c.OutputPort, node));
        }

        /// <summary>
        /// 특정 포트가 해당 노드에 속하는지 확인한다.
        /// IReadOnlyList는 Contains가 없으므로 직접 루프로 비교한다.
        /// </summary>
        private static bool IsPortInNode(Port port, NodeBase node)
        {
            foreach (var p in node.InputPorts)
                if (p == port) return true;
            foreach (var p in node.OutputPorts)
                if (p == port) return true;
            return false;
        }

        /// <summary>
        /// 두 포트 사이에 연결을 생성한다.
        /// 출력→입력 방향이어야 하고, 자원 타입이 일치해야 한다.
        /// </summary>
        /// <param name="outputPort">자원을 내보내는 출력 포트.</param>
        /// <param name="inputPort">자원을 받는 입력 포트.</param>
        /// <param name="connection">생성된 Connection 객체 (실패 시 null).</param>
        /// <returns>연결 성공 여부.</returns>
        public bool TryConnect(Port outputPort, Port inputPort, out Connection connection)
        {
            connection = null;

            if (outputPort == null || inputPort == null)
            {
                Debug.LogWarning("[NodeGraph] 연결 실패: 포트가 null입니다.");
                return false;
            }
            if (outputPort.Direction != PortDirection.Output ||
                inputPort.Direction != PortDirection.Input)
            {
                Debug.LogWarning("[NodeGraph] 연결 실패: 포트 방향이 올바르지 않습니다. (출력 → 입력 순서여야 함)");
                return false;
            }
            if (outputPort.ResourceType != inputPort.ResourceType)
            {
                Debug.LogWarning($"[NodeGraph] 연결 실패: 자원 타입 불일치 " +
                                 $"({outputPort.ResourceType} → {inputPort.ResourceType})");
                return false;
            }

            connection = new Connection(outputPort, inputPort);
            _connections.Add(connection);
            Debug.Log($"[NodeGraph] 연결 생성: {outputPort.ResourceType} 출력 → 입력");
            return true;
        }

        /// <summary>
        /// 연결을 그래프에서 제거한다.
        /// </summary>
        public void RemoveConnection(Connection connection)
        {
            _connections.Remove(connection);
        }

        /// <summary>
        /// 틱을 1회 실행한다.
        ///   1단계: 각 노드의 OnTick()을 호출해 자원을 생산/소비한다.
        ///   2단계: 연결선을 통해 출력 포트의 자원을 입력 포트로 분배한다.
        /// </summary>
        public void Tick()
        {
            foreach (var node in _nodes)
                node.OnTick();

            DistributeResources();
        }

        /// <summary>
        /// 모든 출력 포트의 자원을 연결된 입력 포트에 균등 분배한다.
        ///
        /// 규칙:
        ///   - 한 출력 포트에 N개의 연결이 있으면 1틱 1개를 1/N씩 나눈다.
        ///   - 이미 가득 찬 입력 포트는 분배 대상에서 제외한다 (적응형 분배).
        ///   - 소수 분배량은 Connection이 누적해 1.0 이상이 되면 정수로 전달한다.
        /// </summary>
        private void DistributeResources()
        {
            var byOutputPort = BuildOutputPortConnectionMap();

            foreach (var pair in byOutputPort)
            {
                Port outputPort = pair.Key;
                List<Connection> connections = pair.Value;

                if (outputPort.IsEmpty) continue;

                // 받을 수 있는 연결만 선택 (적응형 분배)
                var receivable = new List<Connection>();
                foreach (var conn in connections)
                    if (conn.CanReceive()) receivable.Add(conn);

                if (receivable.Count == 0) continue;

                // 1틱에 1개를 꺼내 receivable 수로 균등 분배
                outputPort.Take(1);
                float amountPerConnection = 1f / receivable.Count;
                foreach (var conn in receivable)
                    conn.AccumulateAndTransfer(amountPerConnection);
            }
        }

        /// <summary>
        /// 연결 목록을 출력 포트 기준으로 묶어 Dictionary로 반환한다.
        /// </summary>
        private Dictionary<Port, List<Connection>> BuildOutputPortConnectionMap()
        {
            var dict = new Dictionary<Port, List<Connection>>();
            foreach (var conn in _connections)
            {
                if (!dict.ContainsKey(conn.OutputPort))
                    dict[conn.OutputPort] = new List<Connection>();
                dict[conn.OutputPort].Add(conn);
            }
            return dict;
        }
    }
}
