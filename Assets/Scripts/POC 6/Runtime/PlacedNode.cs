using System;
using System.Collections.Generic;
using UnityEngine;

namespace POC6
{
    /// <summary>
    /// 그리드에 실제로 배치된 노드의 런타임 인스턴스 데이터입니다.
    /// ScriptableObject인 NodeData와 달리, 배치 위치/회전/업그레이드 레벨 등
    /// 인스턴스별 상태를 저장합니다.
    /// </summary>
    public class PlacedNode
    {
        /// <summary>이 노드의 종류와 기본 스탯을 담은 원본 데이터</summary>
        public NodeData Data { get; private set; }

        /// <summary>그리드 상의 좌상단 셀 좌표</summary>
        public Vector2Int GridPosition { get; private set; }

        /// <summary>
        /// 노드 회전 단계 (0 = 0도, 1 = 90도, 2 = 180도, 3 = 270도)
        /// 공격 노드의 발사 방향에 영향을 줍니다.
        /// </summary>
        public int RotationStep { get; private set; }

        /// <summary>현재 업그레이드 레벨 (0 = 기본)</summary>
        public int CurrentUpgradeLevel { get; private set; }

        /// <summary>씬에 배치된 노드 게임오브젝트 참조</summary>
        public GameObject WorldInstance { get; set; }

        public PlacedNode(NodeData data, Vector2Int gridPosition, int rotationStep = 0)
        {
            Data = data;
            GridPosition = gridPosition;
            RotationStep = rotationStep;
            CurrentUpgradeLevel = 0;
        }

        /// <summary>
        /// 이 노드가 그리드에서 차지하는 모든 셀 좌표 목록을 반환합니다.
        /// 회전을 반영하여 실제로 점유하는 셀들을 계산합니다.
        /// </summary>
        public List<Vector2Int> GetOccupiedCells()
        {
            var cells = new List<Vector2Int>();
            Vector2Int size = GetRotatedSize();

            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    cells.Add(GridPosition + new Vector2Int(x, y));
                }
            }

            return cells;
        }

        /// <summary>
        /// 회전을 반영한 실제 노드 크기를 반환합니다.
        /// 90도 / 270도 회전 시 가로세로가 뒤바뀝니다.
        /// </summary>
        public Vector2Int GetRotatedSize()
        {
            // 90도 또는 270도 회전 시 가로세로 축이 교환됨
            bool isTransposed = (RotationStep % 2) == 1;
            return isTransposed
                ? new Vector2Int(Data.Size.y, Data.Size.x)
                : new Vector2Int(Data.Size.x, Data.Size.y);
        }

        /// <summary>
        /// 회전과 노드의 발사 방향(AttackFace)을 합산하여
        /// 우주선 로컬 좌표 기준 발사 방향 벡터를 반환합니다.
        /// 공격 노드에서만 유효합니다.
        /// </summary>
        public Vector2 GetLocalFireDirection()
        {
            // FaceDirection을 기본 벡터로 변환
            Vector2 baseDir = FaceDirectionToVector(Data.AttackFace);

            // RotationStep만큼 회전 적용 (각 step = 90도)
            float angle = RotationStep * 90f;
            Quaternion rot = Quaternion.Euler(0f, 0f, angle);
            return rot * baseDir;
        }

        /// <summary>
        /// FaceDirection 열거형 값을 2D 방향 벡터로 변환합니다.
        /// (Top = 위쪽 = (0, 1))
        /// </summary>
        private Vector2 FaceDirectionToVector(FaceDirection face)
        {
            return face switch
            {
                FaceDirection.Top => Vector2.up,
                FaceDirection.Bottom => Vector2.down,
                FaceDirection.Left => Vector2.left,
                FaceDirection.Right => Vector2.right,
                _ => Vector2.up
            };
        }

        /// <summary>
        /// 업그레이드 레벨을 1 증가시킵니다.
        /// GameConfig의 최대 레벨 체크는 호출하는 쪽(UpgradeSystem)에서 담당합니다.
        /// </summary>
        public void UpgradeLevel()
        {
            CurrentUpgradeLevel++;
        }

        /// <summary>
        /// 현재 업그레이드 레벨을 반영한 기본 공격 스탯을 반환합니다.
        /// bonusPerLevel: 레벨당 증가 비율 (예: 0.2 = 20% 증가)
        /// 공격 노드가 아니거나 레벨 0이면 기본 스탯을 그대로 반환합니다.
        /// </summary>
        public AttackStats GetUpgradedBaseStats(float bonusPerLevel)
        {
            if (CurrentUpgradeLevel == 0 || Data.NodeType != NodeType.Attack)
                return Data.BaseAttackStats;

            float multiplier = 1f + CurrentUpgradeLevel * bonusPerLevel;
            var b = Data.BaseAttackStats;

            // 데미지와 공격속도를 레벨에 따라 증가. 사거리와 발사체 속도는 유지.
            return new AttackStats(
                b.Damage * multiplier,
                b.FireRate * multiplier,
                b.AttackRange,
                b.ProjectileSpeed,
                b.ProjectileCount,
                b.PierceCount
            );
        }

        /// <summary>
        /// 스냅샷 복원 등 특수 상황에서 레벨을 직접 설정합니다.
        /// </summary>
        public void SetUpgradeLevel(int level)
        {
            CurrentUpgradeLevel = Mathf.Max(0, level);
        }
    }

    /// <summary>
    /// 웨이브 스냅샷 직렬화를 위한 PlacedNode의 데이터 전용 구조체입니다.
    /// ScriptableObject 참조는 에셋 경로(GUID)가 아닌 이름으로 저장합니다.
    /// </summary>
    [Serializable]
    public class PlacedNodeData
    {
        /// <summary>NodeData ScriptableObject 에셋의 이름 (복원 시 Resources.Load에 사용)</summary>
        public string nodeDataName;
        public int gridX;
        public int gridY;
        public int rotationStep;
        public int upgradeLevel;
    }
}
