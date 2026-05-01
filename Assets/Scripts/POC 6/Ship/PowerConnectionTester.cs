using UnityEngine;
using UnityEngine.InputSystem;

namespace POC6
{
    /// <summary>
    /// PowerGraph 및 PowerConnectionDragger 동작 확인용 임시 테스트 스크립트입니다.
    /// 마우스 클릭 위치의 노드를 감지해서 드래그를 시작합니다.
    /// 연결 완료 후 Space 키로 각 공격 노드의 유효 스탯을 로그로 출력합니다.
    /// 테스트 완료 후 삭제해도 됩니다.
    /// </summary>
    public class PowerConnectionTester : MonoBehaviour
    {
        [SerializeField] private ShipGrid _shipGrid;
        [SerializeField] private PowerGraph _powerGraph;
        [SerializeField] private PowerConnectionDragger _dragger;
        [SerializeField] private Camera _mainCamera;

        private void Awake()
        {
            if (_mainCamera == null)
                _mainCamera = Camera.main;
        }

        private void Update()
        {
            // 좌클릭: 클릭 위치의 노드로 드래그 시작
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                PlacedNode node = GetNodeAtMouse();
                if (node != null)
                {
                    Debug.Log($"[PowerConnectionTester] 드래그 시작: {node.Data.NodeName} ({node.Data.NodeType})");
                    _dragger.BeginDrag(node);
                }
            }

            // Space 키: 모든 공격 노드의 유효 스탯 출력
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
                LogAllAttackNodeStats();
        }

        /// <summary>
        /// 마우스 위치의 그리드 셀에 있는 노드를 반환합니다.
        /// </summary>
        private PlacedNode GetNodeAtMouse()
        {
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector3 pos = new Vector3(mouseScreenPos.x, mouseScreenPos.y, Mathf.Abs(_mainCamera.transform.position.z));
            Vector3 worldPos = _mainCamera.ScreenToWorldPoint(pos);

            Vector2Int cell = _shipGrid.WorldToGrid(worldPos);
            return _shipGrid.GetNodeAt(cell);
        }

        /// <summary>
        /// 현재 그리드의 모든 공격 노드에 대해 PowerGraph가 계산한 유효 스탯을 로그로 출력합니다.
        /// 코어와 연결된 노드는 스탯이 0보다 크게 나와야 합니다.
        /// </summary>
        private void LogAllAttackNodeStats()
        {
            bool found = false;

            foreach (var node in _shipGrid.PlacedNodes)
            {
                if (node.Data.NodeType != NodeType.Attack) continue;

                found = true;
                AttackStats stats = _powerGraph.GetEffectiveStats(node);

                Debug.Log($"[PowerConnectionTester] 공격 노드 '{node.Data.NodeName}' 유효 스탯 | " +
                          $"Damage: {stats.Damage:F1} | FireRate: {stats.FireRate:F2} | " +
                          $"Range: {stats.AttackRange:F1} | Projectiles: {stats.ProjectileCount} | Pierce: {stats.PierceCount}");
            }

            if (!found)
                Debug.Log("[PowerConnectionTester] 배치된 공격 노드가 없습니다.");
        }
    }
}
