using System;
using UnityEngine;
using POC5.Data;

namespace POC5.Graph
{
    /// <summary>
    /// 설비 노드의 게임 로직을 담당하는 순수 C# 클래스.
    /// MonoBehaviour에 의존하지 않으므로 UI 없이도 단독으로 테스트할 수 있다.
    ///
    /// FacilityData를 참조해 포트를 초기화하고,
    /// 매 틱 FacilityType에 따른 생산/소비/저장 로직을 실행한다.
    /// </summary>
    public class FacilityGraphNode : NodeBase
    {
        /// <summary>이 노드가 참조하는 설비 메타데이터.</summary>
        public FacilityData Data { get; }

        // 틱 간 소수 생산량 누적값.
        // 생산 속도가 1 미만일 때 여러 틱에 걸쳐 정수 1개씩 생산하기 위해 사용한다.
        private float _productionAccumulator;

        // 현재 배치된 스피릿. 배치 없으면 null.
        private SpiritData _assignedSpirit;

        /// <summary>현재 배치된 스피릿 데이터 (없으면 null).</summary>
        public SpiritData AssignedSpirit => _assignedSpirit;

        public FacilityGraphNode(FacilityData data)
            : base(Guid.NewGuid().ToString())
        {
            Data = data;
            InitializePorts();

            // 설정 확인용 로그. 각 설비의 FacilityType과 포트 수가 올바른지 검증한다.
            UnityEngine.Debug.Log(
                $"[FacilityGraphNode] {data.DisplayName} 생성 — " +
                $"FacilityType={data.FacilityType}, " +
                $"입력포트={_inputPorts.Count}개, 출력포트={_outputPorts.Count}개");
        }

        /// <summary>
        /// FacilityData의 InputPorts/OutputPorts 정의를 바탕으로 Port 객체를 생성한다.
        /// </summary>
        private void InitializePorts()
        {
            if (Data.InputPorts != null)
                foreach (var def in Data.InputPorts)
                    _inputPorts.Add(new Port(PortDirection.Input, def.resourceType, def.capacity));

            if (Data.OutputPorts != null)
                foreach (var def in Data.OutputPorts)
                    _outputPorts.Add(new Port(PortDirection.Output, def.resourceType, def.capacity));
        }

        /// <summary>
        /// 스피릿을 이 설비에 배치한다.
        /// </summary>
        public void AssignSpirit(SpiritData spirit) => _assignedSpirit = spirit;

        /// <summary>
        /// 배치된 스피릿을 해제한다.
        /// </summary>
        public void UnassignSpirit() => _assignedSpirit = null;

        /// <summary>
        /// 매 틱 NodeGraph에서 호출된다.
        /// FacilityType에 따라 적합한 생산 로직을 실행한다.
        /// </summary>
        public override void OnTick()
        {
            switch (Data.FacilityType)
            {
                case FacilityType.Pump:
                case FacilityType.Cultivator:
                    // 스피릿이 있으면 매 틱 출력 포트에 자원을 생산한다
                    TryProduceAsGenerator();
                    break;

                case FacilityType.Farm:
                    // 입력 자원을 소비해 출력 자원 생산 (스피릿 불필요)
                    TryProduceAsProcessor(requiresSpirit: false);
                    break;

                case FacilityType.Kitchen:
                    // 입력 자원을 소비해 출력 자원 생산 (스피릿 필요)
                    TryProduceAsProcessor(requiresSpirit: true);
                    break;

                case FacilityType.Warehouse:
                    // 입력 포트 → 출력 포트로 자원을 이동시켜 버퍼 역할을 한다
                    PassThroughStorage();
                    break;

                case FacilityType.Market:
                    // 입력 자원을 소비하고 돈으로 변환한다
                    TryConvertToMoney();
                    break;
            }
        }

        /// <summary>
        /// [Pump / Cultivator 전용]
        /// 스피릿이 배치되어 있으면 매 틱 출력 포트에 자원을 생산한다.
        /// 실제 생산량 = BaseProductionPerTick * 스피릿의 WorkPower.
        /// </summary>
        private void TryProduceAsGenerator()
        {
            if (_assignedSpirit == null) return;

            _productionAccumulator += Data.BaseProductionPerTick * _assignedSpirit.WorkPower;

            while (_productionAccumulator >= 1f)
            {
                bool anyProduced = false;
                foreach (var port in _outputPorts)
                {
                    if (port.TryAdd(1))
                    {
                        anyProduced = true;
                        Debug.Log($"[{Data.DisplayName}] {port.ResourceType} +1 생산 " +
                                  $"({port.CurrentAmount}/{port.Capacity})");
                    }
                }
                // 출력 포트가 모두 가득 차면 생산을 멈춘다
                if (!anyProduced) break;
                _productionAccumulator -= 1f;
            }
        }

        /// <summary>
        /// [Farm / Kitchen 전용]
        /// 모든 입력 포트에 재료가 1개 이상 있을 때 소비하고 출력 포트에 1개씩 생산한다.
        /// </summary>
        /// <param name="requiresSpirit">true이면 스피릿이 없을 때 작동하지 않는다.</param>
        private void TryProduceAsProcessor(bool requiresSpirit)
        {
            if (requiresSpirit && _assignedSpirit == null) return;

            float workPower = (requiresSpirit && _assignedSpirit != null)
                ? _assignedSpirit.WorkPower
                : 1f;

            _productionAccumulator += Data.BaseProductionPerTick * workPower;

            while (_productionAccumulator >= 1f)
            {
                // 모든 입력 포트에 재료가 있는지 확인
                bool allInputsAvailable = true;
                foreach (var port in _inputPorts)
                    if (port.IsEmpty) { allInputsAvailable = false; break; }

                // 모든 출력 포트에 공간이 있는지 확인
                bool allOutputsHaveSpace = true;
                foreach (var port in _outputPorts)
                    if (port.IsFull) { allOutputsHaveSpace = false; break; }

                if (!allInputsAvailable || !allOutputsHaveSpace) break;

                // 입력 소비 → 출력 생산
                foreach (var port in _inputPorts) port.Take(1);
                foreach (var port in _outputPorts)
                {
                    port.TryAdd(1);
                    Debug.Log($"[{Data.DisplayName}] {port.ResourceType} +1 생산 " +
                              $"({port.CurrentAmount}/{port.Capacity})");
                }
                _productionAccumulator -= 1f;
            }
        }

        /// <summary>
        /// [Warehouse 전용]
        /// 입력 포트의 자원을 매 틱 출력 포트로 옮긴다.
        /// 이를 통해 창고는 대용량 버퍼 역할을 하면서 자원 흐름을 이어준다.
        /// </summary>
        private void PassThroughStorage()
        {
            int portCount = Mathf.Min(_inputPorts.Count, _outputPorts.Count);
            for (int i = 0; i < portCount; i++)
            {
                var input = _inputPorts[i];
                var output = _outputPorts[i];
                while (!input.IsEmpty && !output.IsFull)
                {
                    input.Take(1);
                    output.TryAdd(1);
                }
            }
        }

        /// <summary>
        /// [Market 전용]
        /// 입력 포트의 자원을 소비하고 돈으로 변환한다.
        /// POC에서는 Debug.Log로만 기록한다. 실제 돈 추적은 6단계에서 추가된다.
        /// </summary>
        private void TryConvertToMoney()
        {
            foreach (var port in _inputPorts)
            {
                while (!port.IsEmpty)
                {
                    port.Take(1);
                    Debug.Log($"[{Data.DisplayName}] {port.ResourceType} 판매 → 돈 획득");
                }
            }
        }
    }
}
