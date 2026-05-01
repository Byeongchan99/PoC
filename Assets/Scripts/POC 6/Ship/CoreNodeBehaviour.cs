using UnityEngine;

namespace POC6
{
    /// <summary>
    /// 코어 노드임을 나타내는 컴포넌트입니다.
    /// NodeVisualFactory가 Core 타입 노드 생성 시 자동으로 부착합니다.
    /// 현재는 마킹 역할이며, 추후 동력 공급 시각화나 코어 파괴 시 특수 처리 등에 활용할 수 있습니다.
    /// </summary>
    public class CoreNodeBehaviour : MonoBehaviour
    {
        private PlacedNode _placedNode;

        /// <summary>
        /// 노드 생성 시 NodeVisualFactory에서 호출합니다.
        /// </summary>
        public void Initialize(PlacedNode node)
        {
            _placedNode = node;
        }
    }
}
