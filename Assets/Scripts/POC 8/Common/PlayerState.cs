namespace POC8
{
    /// <summary>
    /// 플레이어의 행동 상태를 나타내는 열거형.
    /// </summary>
    public enum PlayerState
    {
        /// <summary>링 내벽에 부착된 상태. 입력을 대기한다.</summary>
        Landed,

        /// <summary>클릭한 방향으로 직선 돌진 중인 상태.</summary>
        Dashing,
    }
}
