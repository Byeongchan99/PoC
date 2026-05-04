using System;

namespace POC6
{
    // 게임 전체에서 사용하는 열거형 정의 모음

    /// <summary>
    /// 노드의 종류를 구분합니다.
    /// </summary>
    public enum NodeType
    {
        /// <summary>동력을 공급하는 시작점 노드</summary>
        Core,
        /// <summary>공격 노드에 특수 효과를 부여하는 중계 노드</summary>
        Special,
        /// <summary>적에게 발사체를 쏘는 종착점 노드</summary>
        Attack,
        /// <summary>체력에 기여하고 구조적 다리 역할을 하는 노드</summary>
        Normal
    }

    /// <summary>
    /// 공격 노드의 발사 방향을 나타냅니다.
    /// 직사각형의 어느 면이 발사구인지를 정의합니다.
    /// </summary>
    public enum FaceDirection
    {
        /// <summary>노드 위쪽 면에서 발사</summary>
        Top,
        /// <summary>노드 아래쪽 면에서 발사</summary>
        Bottom,
        /// <summary>노드 왼쪽 면에서 발사</summary>
        Left,
        /// <summary>노드 오른쪽 면에서 발사</summary>
        Right
    }

    /// <summary>
    /// 적의 등급을 나타냅니다.
    /// </summary>
    public enum EnemyTier
    {
        /// <summary>기본 스탯, 다수 등장하는 일반 적</summary>
        Normal,
        /// <summary>체력이 높은 강화 적</summary>
        Elite,
        /// <summary>체력이 매우 높고 마지막 웨이브에 등장하는 보스</summary>
        Boss
    }

    /// <summary>
    /// 특수 노드가 공격 노드에 부여하는 효과 종류입니다.
    /// </summary>
    public enum SpecialEffectType
    {
        /// <summary>공격 시 발사체 개수를 증가시킵니다</summary>
        Multishot,
        /// <summary>발사체가 적을 통과해 여러 번 타격합니다</summary>
        Pierce
    }

    /// <summary>
    /// 게임의 현재 진행 상태를 나타냅니다.
    /// </summary>
    public enum GameState
    {
        /// <summary>초기화 단계</summary>
        Init,
        /// <summary>노드 배치 및 업그레이드를 하는 빌드 단계 (시간 정지)</summary>
        BuildPhase,
        /// <summary>적과 전투하는 실시간 단계</summary>
        CombatPhase,
        /// <summary>웨이브 결과를 보여주는 단계 (클리어/실패 판정)</summary>
        WaveResult,
        /// <summary>카드 3장 중 1장을 선택하는 단계</summary>
        CardSelection
    }
}
