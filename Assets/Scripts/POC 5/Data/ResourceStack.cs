using System;
using UnityEngine;

namespace POC5.Data
{
    /// <summary>
    /// 자원의 종류와 수량을 묶은 직렬화 가능 구조체.
    /// 포트에 담기거나 연결선을 통해 이동하는 자원 "한 묶음"을 표현한다.
    ///
    /// 실무 팁: 구조체(struct)를 사용하면 힙 할당 없이 값 복사로 전달되어
    /// 자원 이동처럼 빈번하게 생성/소멸하는 데이터에 GC 부담을 줄일 수 있다.
    /// </summary>
    [Serializable]
    public struct ResourceStack
    {
        /// <summary>자원의 종류.</summary>
        [SerializeField] public ResourceType resourceType;

        /// <summary>자원의 정수 수량.</summary>
        [SerializeField] public int amount;

        /// <summary>
        /// 자원 종류와 수량을 지정해 초기화하는 생성자.
        /// </summary>
        /// <param name="resourceType">자원 종류.</param>
        /// <param name="amount">수량.</param>
        public ResourceStack(ResourceType resourceType, int amount)
        {
            this.resourceType = resourceType;
            this.amount = amount;
        }

        /// <summary>
        /// 디버그 출력용 문자열 반환.
        /// </summary>
        public override string ToString() => $"{resourceType} x{amount}";
    }
}
