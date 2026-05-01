using UnityEngine;
using UnityEngine.InputSystem;

namespace POC6
{
    /// <summary>
    /// NodePlacer 동작 확인용 임시 테스트 스크립트입니다.
    /// 숫자 키 1~9로 해당 슬롯의 NodeData 배치 모드를 시작합니다.
    /// 테스트 완료 후 삭제해도 됩니다.
    /// </summary>
    public class NodePlacerTester : MonoBehaviour
    {
        [Tooltip("배치할 NodeData 에셋 목록. 인덱스 0 = 숫자키 1, 인덱스 1 = 숫자키 2 ...")]
        [SerializeField] private NodeData[] _testNodes;

        [SerializeField] private NodePlacer _nodePlacer;

        private void Update()
        {
            // 숫자 키 1~9로 해당 슬롯의 노드 배치 시작
            for (int i = 0; i < 9; i++)
            {
                if (Keyboard.current[Key.Digit1 + i].wasPressedThisFrame)
                {
                    TryBeginPlacement(i);
                    break;
                }
            }
        }

        /// <summary>
        /// 지정한 인덱스의 NodeData로 배치 모드를 시작합니다.
        /// </summary>
        private void TryBeginPlacement(int index)
        {
            if (_nodePlacer == null)
            {
                Debug.LogWarning("[NodePlacerTester] NodePlacer가 연결되지 않았습니다.");
                return;
            }

            if (_testNodes == null || index >= _testNodes.Length || _testNodes[index] == null)
            {
                Debug.LogWarning($"[NodePlacerTester] 슬롯 {index + 1}에 NodeData가 없습니다.");
                return;
            }

            _nodePlacer.BeginPlacement(_testNodes[index]);
            Debug.Log($"[NodePlacerTester] '{_testNodes[index].NodeName}' 배치 모드 시작 (키 {index + 1}). R=회전 / ESC=취소");
        }
    }
}
