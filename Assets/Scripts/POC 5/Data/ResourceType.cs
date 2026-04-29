namespace POC5.Data
{
    /// <summary>
    /// 게임 내 존재하는 자원의 종류를 정의하는 열거형.
    /// 새 자원을 추가할 때는 이 enum에 항목을 추가하면 된다.
    /// </summary>
    public enum ResourceType
    {
        Water,  // 물 - 양수기에서 생산
        Seed,   // 씨앗 - 재배기에서 생산
        Crop,   // 작물 - 농장에서 생산
        Food,   // 식량 - 주방에서 생산
        Money   // 돈 - 시장에서 생산
    }
}
