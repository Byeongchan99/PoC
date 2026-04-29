using UnityEngine;
using POC5.Data;

namespace POC5.Graph
{
    /// <summary>포트의 방향을 나타내는 열거형.</summary>
    public enum PortDirection
    {
        Input,  // 자원이 들어오는 입력 포트
        Output  // 자원이 나가는 출력 포트
    }

    /// <summary>
    /// 노드의 입력 또는 출력 포트.
    /// 특정 종류의 자원을 용량 한도 내에서 버퍼링한다.
    /// </summary>
    public class Port
    {
        /// <summary>포트 방향 (Input / Output).</summary>
        public PortDirection Direction { get; }

        /// <summary>이 포트가 처리하는 자원 종류.</summary>
        public ResourceType ResourceType { get; }

        /// <summary>최대 저장 용량.</summary>
        public int Capacity { get; }

        private int _currentAmount;

        /// <summary>현재 저장된 자원 수량.</summary>
        public int CurrentAmount => _currentAmount;

        /// <summary>포트가 가득 찼는지 여부.</summary>
        public bool IsFull => _currentAmount >= Capacity;

        /// <summary>포트가 비어 있는지 여부.</summary>
        public bool IsEmpty => _currentAmount <= 0;

        /// <summary>남은 여유 공간.</summary>
        public int FreeSpace => Capacity - _currentAmount;

        /// <summary>
        /// 포트를 초기화한다.
        /// </summary>
        public Port(PortDirection direction, ResourceType resourceType, int capacity)
        {
            Direction = direction;
            ResourceType = resourceType;
            Capacity = Mathf.Max(1, capacity);
        }

        /// <summary>
        /// 자원을 포트에 추가한다.
        /// 포트가 가득 찼으면 추가하지 않고 false를 반환한다.
        /// </summary>
        public bool TryAdd(int amount)
        {
            if (IsFull) return false;
            _currentAmount = Mathf.Min(_currentAmount + amount, Capacity);
            return true;
        }

        /// <summary>
        /// 포트에서 자원을 꺼낸다.
        /// 요청량보다 자원이 적으면 있는 만큼만 꺼내고 실제 꺼낸 양을 반환한다.
        /// </summary>
        public int Take(int amount)
        {
            int taken = Mathf.Min(amount, _currentAmount);
            _currentAmount -= taken;
            return taken;
        }
    }
}
