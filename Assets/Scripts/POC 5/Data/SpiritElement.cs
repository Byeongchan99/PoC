namespace POC5.Data
{
    /// <summary>
    /// 스피릿의 속성(원소) 종류를 정의하는 열거형.
    /// 각 속성은 특정 설비에만 배치할 수 있다.
    /// </summary>
    public enum SpiritElement
    {
        Water,  // 물 속성 - 양수기에 배치 가능
        Grass,  // 풀 속성 - 재배기에 배치 가능
        Fire    // 불 속성 - 주방에 배치 가능
    }
}
