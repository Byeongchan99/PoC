using System;
using System.Collections.Generic;

namespace POC6
{
    /// <summary>
    /// 단일 동력 연결 한 가닥의 직렬화 데이터입니다.
    /// </summary>
    [Serializable]
    public class PowerConnectionData
    {
        /// <summary>연결 출발 노드의 그리드 위치 (X)</summary>
        public int fromGridX;
        /// <summary>연결 출발 노드의 그리드 위치 (Y)</summary>
        public int fromGridY;
        /// <summary>연결 도착 노드의 그리드 위치 (X)</summary>
        public int toGridX;
        /// <summary>연결 도착 노드의 그리드 위치 (Y)</summary>
        public int toGridY;
    }

    /// <summary>
    /// 웨이브 시작 직전의 게임 전체 상태를 저장하는 스냅샷입니다.
    /// 웨이브 실패 시 이 스냅샷을 복원하여 이전 웨이브로 돌아갑니다.
    /// </summary>
    [Serializable]
    public class WaveSnapshot
    {
        /// <summary>스냅샷이 저장된 시점의 웨이브 번호</summary>
        public int waveNumber;

        /// <summary>그리드에 배치된 모든 노드의 직렬화 데이터</summary>
        public List<PlacedNodeData> nodes = new();

        /// <summary>동력 그래프의 모든 연결 직렬화 데이터</summary>
        public List<PowerConnectionData> connections = new();

        /// <summary>획득한 카드들의 이름 목록 (복원 시 CardData 에셋 로드에 사용)</summary>
        public List<string> deckCardNames = new();

        /// <summary>저장 시점의 골드</summary>
        public int gold;
    }
}
