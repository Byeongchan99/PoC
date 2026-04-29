namespace POC5.Graph
{
    /// <summary>
    /// 출력 포트와 입력 포트를 잇는 연결선.
    /// 자원이 이 Connection을 통해 한 노드에서 다른 노드로 흐른다.
    ///
    /// float 누적 방식:
    ///   1개의 자원을 여러 연결로 나누면 소수가 생긴다.
    ///   소수를 _pendingAmount에 누적하다가 1.0 이상이 되면 입력 포트에 정수 1개씩 전달한다.
    ///   이는 Factorio의 splitter 동작 방식과 유사하다.
    /// </summary>
    public class Connection
    {
        /// <summary>자원이 나오는 출력 포트.</summary>
        public Port OutputPort { get; }

        /// <summary>자원이 들어가는 입력 포트.</summary>
        public Port InputPort { get; }

        // 아직 전달되지 못한 소수 자원량 누적값
        private float _pendingAmount;

        public Connection(Port outputPort, Port inputPort)
        {
            OutputPort = outputPort;
            InputPort = inputPort;
        }

        /// <summary>
        /// 소수 자원량을 누적하고, 1.0 이상이 될 때마다 입력 포트에 정수 1개씩 전달한다.
        /// NodeGraph의 균등 분배 계산 결과를 받아 처리한다.
        /// </summary>
        /// <param name="amountToAdd">이번 틱에 이 연결에 배분된 자원량 (소수 가능).</param>
        public void AccumulateAndTransfer(float amountToAdd)
        {
            _pendingAmount += amountToAdd;

            // 1.0 이상 누적될 때마다 입력 포트에 1개씩 전달
            while (_pendingAmount >= 1f)
            {
                if (InputPort.IsFull) break; // 입력 포트가 가득 차면 보류
                InputPort.TryAdd(1);
                _pendingAmount -= 1f;
            }
        }

        /// <summary>
        /// 이 연결의 입력 포트가 자원을 더 받을 수 있는지 여부.
        /// 가득 찬 포트는 균등 분배에서 제외된다 (적응형 분배).
        /// </summary>
        public bool CanReceive() => !InputPort.IsFull;
    }
}
