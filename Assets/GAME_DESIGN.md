# 노드 기반 우주선 게임 - POC 설계 문서

> Claude Code 작업용 통합 설계 문서. 이 문서는 POC(Proof of Concept) 단계 기준으로 작성되었으며, 메카닉 검증이 목적입니다.

---

## 목차

1. [게임 개요](#1-게임-개요)
2. [핵심 메카닉](#2-핵심-메카닉)
3. [게임 진행 흐름](#3-게임-진행-흐름)
4. [시스템 아키텍처](#4-시스템-아키텍처)
5. [데이터 구조 설계](#5-데이터-구조-설계)
6. [까다로운 부분 사전 경고](#6-까다로운-부분-사전-경고)
7. [작업 분할 (Claude Code용 체크리스트)](#7-작업-분할-claude-code용-체크리스트)
8. [코딩 컨벤션](#8-코딩-컨벤션)

---

## 1. 게임 개요

### 한 줄 요약
탑다운 시점에서 노드를 조립해 우주선을 만들고, 5웨이브의 적을 막아내는 로그라이크 덱빌딩 슈터의 POC.

### 장르
- 노드 기반 덱빌딩 + 타워디펜스 + 탑다운 슈터
- 인크리멘탈 로그라이크 (실패 시 이전 웨이브로 복귀)

### 타겟
- POC 단계: 메카닉 검증
- 최종 목표: 한 판 15-30분, 접근성 높은 난이도

### 레퍼런스
- **Reassembly**: 모듈 조립으로 우주선 만들기
- **Vampire Survivors**: 탑다운 + 자동 공격 + 로그라이크
- **Slay the Spire**: 카드 선택 진행

### POC 범위 - 만드는 것
- 우주선 노드 배치 시스템 (그리드 기반, 가변 크기)
- 노드 인접 + 동력 연결 분리 시스템
- 4종 노드 (코어, 특수x2, 공격, 일반)
- 우주선 조작 (이동/회전)
- 적 3종 (일반, 엘리트, 보스)
- 5웨이브 진행
- 카드 선택 (3장 중 1장)
- 골드 획득 + 노드 업그레이드
- 웨이브 실패 시 이전 웨이브 복귀

### POC 범위 - 안 만드는 것 (나중에)
- 메타 진행 (영구 강화)
- 카드 제거/강화/변환
- 카드 레어도
- 상점 시스템
- 노드/적 종류 추가
- 사실적 비주얼 (POC는 프리미티브 도형)
- 사운드/이펙트

---

## 2. 핵심 메카닉

### 2-1. 노드 시스템

#### 노드 종류 (4가지)
| 종류 | 역할 | 동력 연결 참여 |
|------|------|----------------|
| **코어** | 동력 공급원 | 시작점 |
| **특수** | 공격 노드에 효과 부여 | 중계점 |
| **공격** | 적에게 발사 | 종착점 |
| **일반** | 체력 기여 + 구조적 다리 | 참여 안 함 |

#### 노드 속성
- **크기**: 가변 (1x1, 1x2, 2x2 등)
- **체력 기여도**: 노드별 고정값. 모든 노드의 합 = 우주선 최대 체력
- **방향성** (공격 노드만): 직사각형의 한 면이 발사 방향. 그 면이 향하는 180도 전방으로 발사

#### 두 가지 그래프 - 매우 중요
**노드는 "물리적 인접"과 "동력 연결" 두 가지 그래프로 관리됩니다. 이 둘은 별개입니다.**

```
[물리적 인접 그래프]              [동력 연결 그래프]
- 그리드 배치 시 강제             - 플레이어가 드래그로 직접 연결
- 한 면 이상이 닿아야 배치 가능    - 코어 → 공격 또는 코어 → 특수 → 공격
- 모든 노드가 참여                 - 일반 노드는 참여 안 함
- 우주선의 구조를 결정             - 공격 노드의 능력을 결정
```

#### 동력 시스템
- 코어는 동력 용량 보유 (예: 100 유닛)
- 코어에 연결된 공격 노드들에 균등 분배 (거리 무관)
- 공격 노드는 받은 동력에 비례해 공격력/공속 결정 (구체 공식은 밸런스 단계에서 조정)
- **체인 연결 가능**: `코어 → 특수 → 공격`으로 연결하면 특수 효과 + 동력 모두 적용
- 같은 공격 노드에 여러 코어 동력이 모이는 것은 합산

#### 특수 노드 종류 (POC에서는 2종)
- **다중 발사**: 공격 시 발사체 개수 증가
- **관통**: 발사체가 적을 통과해 여러 번 타격

### 2-2. 우주선 조작

| 입력 | 동작 |
|------|------|
| 마우스 위치 | 우주선이 마우스 방향으로 회전 (좌우 회전) |
| 마우스 클릭 | 우주선이 현재 바라보는 방향으로 전진 |

- 카메라는 탑다운, 우주선을 따라다님 (플레이어 중심)
- 회전은 부드럽게 (즉각 회전 X)
- 이동은 가속/관성 있는 물리 기반 추천 (POC에서도 우주 느낌 살리기)

### 2-3. 전투

#### 공격 노드 발사
- 자동 공격 (사거리 안에 적이 있으면)
- 발사 방향: `노드의 발사 면 방향 + 우주선 회전` 합성
- 가장 가까운 적 자동 조준 (단, 발사 가능 각도 내에서만)

#### 적 AI
- 화면 밖 360도에서 스폰
- 우주선의 가장 가까운 노드를 향해 이동
- 사거리 안에 들어오면 그 노드를 향해 원거리 공격
- 스탯: 사거리, 공격력, 체력, 공격 속도, 이동 속도

#### 적 종류 (3가지)
| 종류 | 특징 |
|------|------|
| **일반** | 기본 스탯, 다수 등장 |
| **엘리트** | 체력 ↑, 외형 차별화 |
| **보스** | 체력 대폭 ↑, 마지막 웨이브에 등장 |

전부 원거리 공격이며, 차이는 체력과 외형뿐입니다 (POC 범위).

#### 체력 시스템
- 단일 풀 (Single Pool)
- 최대 체력 = 모든 노드의 체력 기여도 합
- 노드는 파괴되지 않음 (체력만 깎임)
- 0이 되면 웨이브 실패

### 2-4. 진행 시스템

#### 게임 흐름
```
[Build Phase] → [Combat Phase] → [Wave Result]
     ↑                                  ↓
     └──────── 웨이브 클리어 ─────────────┘
                       또는
                  ↓ 웨이브 실패
              [이전 웨이브로 복귀]
```

#### 웨이브 보상
- 웨이브 클리어 시 카드 3장 제시 (중복 없이 랜덤)
- 1장 선택 → 덱에 추가
- 적 처치 시마다 골드 드롭 (적 종류별로 양 다름)

#### 골드 사용처
- Build Phase에서 노드 클릭 → 업그레이드 버튼
- 노드 업그레이드 (POC: 단순히 스탯 증가)

#### 웨이브 실패 시
- **현재 우주선/덱/골드 상태는 유지**
- 위치만 이전 웨이브로 (3웨이브 실패 → 2웨이브 시작)
- Build Phase부터 다시 시작 가능 (카드 추가 배치, 골드로 업그레이드)

### 2-5. 시작 조건
- 우주선: 미리 만들어둔 기본 형태로 시작
- 덱: 고정 시작 덱
- 골드: 0 (또는 작은 시작 골드)

---

## 3. 게임 진행 흐름

### 상태 머신 다이어그램

```
        [Init]
           ↓
     [BuildPhase]  ←─────┐
           ↓              │
     [CombatPhase]        │
           ↓              │
   ┌── [WaveResult] ──┐   │
   ↓                  ↓   │
[CardSelection]   [Failed]│
   ↓                  ↓   │
   └──────┬───────────┘   │
          ↓               │
    (다음 웨이브)──────────┘
          또는
   (이전 웨이브로)
```

### 각 상태 설명

| 상태 | 시간 | 가능한 동작 |
|------|------|-------------|
| **BuildPhase** | 정지 (Time.timeScale = 0) | 노드 배치, 동력 연결, 노드 업그레이드, 다음 웨이브 시작 버튼 |
| **CombatPhase** | 실시간 | 우주선 조작, 자동 전투 |
| **WaveResult** | 정지 | 클리어/실패 판정, 보상 표시 |
| **CardSelection** | 정지 | 3장 중 1장 선택 |

---

## 4. 시스템 아키텍처

### 4-1. 시스템 다이어그램

```
┌─────────────────────────────────────────────────────────┐
│                    GameManager                          │
│              (상태 머신, 전체 흐름 제어)                  │
└────────┬─────────────────────┬──────────────┬───────────┘
         │                     │              │
    ┌────▼────┐          ┌─────▼──────┐  ┌────▼─────┐
    │  Ship   │          │   Combat   │  │   Deck   │
    │ System  │          │   System   │  │  System  │
    └────┬────┘          └─────┬──────┘  └────┬─────┘
         │                     │              │
    ┌────┴─────┐          ┌────┴────┐    ┌────┴─────┐
    │ShipGrid  │          │EnemySpwn│    │DeckMgr   │
    │NodeGraph │          │Enemy[]  │    │CardSelUI │
    │PowerGraph│          │Projctile│    └──────────┘
    │ShipCtrl  │          │WaveMgr  │
    │HealthSys │          └─────────┘
    └──────────┘

    별도:
    - InputManager (마우스 입력)
    - CameraController (탑다운 추적)
    - GoldSystem
    - UIManager (HP 바, 골드, 웨이브 표시 등)
```

### 4-2. 시스템별 책임

#### **GameManager** (싱글톤 - DontDestroy 사용 안 함)
- 게임 상태 머신 (BuildPhase / CombatPhase / WaveResult / CardSelection)
- 웨이브 진행 (현재 웨이브 번호, 전체 웨이브 데이터)
- 웨이브 시작/종료 신호 발행
- **웨이브 시작 시 스냅샷 저장** (실패 시 복원용)

#### **Ship 관련**

##### `ShipGrid`
- 그리드 데이터 보관 (어느 셀에 어떤 노드가 있는지)
- 노드 배치 가능 여부 검증 (인접 규칙 + 충돌 체크)
- 가변 크기 노드 지원
- 그리드 좌표 ↔ 우주선 로컬 좌표 변환

##### `NodeGraph` (인접 그래프)
- 어느 노드가 어느 노드와 물리적으로 닿아있는지
- 그리드 변경 시 자동 재계산
- 동력 연결 시 사용 안 함, 디버그/시각화용

##### `PowerGraph` (동력 연결 그래프)
- 플레이어가 드래그로 만든 동력 연결
- `코어 → 공격`, `코어 → 특수`, `특수 → 공격`만 허용
- 각 공격 노드가 어느 코어/특수와 연결되어 있는지 추적
- 동력 분배 계산 (코어 용량을 연결된 공격 노드들에 균등 분배)

##### `ShipController`
- 마우스 입력 → 회전/이동
- Rigidbody2D 기반 물리 이동 추천 (관성 살리기)

##### `HealthSystem`
- 단일 체력 풀
- 최대 체력 = 모든 노드 체력 기여도 합 (NodeGraph 또는 ShipGrid 참조)
- 데미지 받기, 0 이하 시 GameManager에 패배 신호

#### **Combat 관련**

##### `EnemySpawner`
- WaveData 기반 스폰 (시간/조건 트리거)
- 화면 밖 360도 랜덤 위치에서 스폰

##### `Enemy`
- 가장 가까운 노드 추적 (월드 좌표 기준)
- 사거리 안 들어오면 정지 + 공격
- 스탯은 EnemyData에서 주입

##### `Projectile`
- Object Pool 사용 (성능)
- 공격 노드/적 모두 사용

##### `WaveManager`
- 현재 웨이브의 적이 모두 처치되었는지 감시
- WaveData 인스펙터 편집 가능

#### **Deck 관련**

##### `DeckManager`
- 덱(전체 카드 풀), 손패는 POC에서는 사용 안 함 (카드 선택 시 매번 덱에서 3장 뽑음)
- 정확히는: "획득한 카드 = 덱"이고, 웨이브 클리어 시 미리 정의된 카드 풀에서 3장 제시

##### `CardSelectionUI`
- 3장 카드 표시
- 선택 시 덱에 추가, BuildPhase로 이동

#### **기타**

##### `InputManager`
- 마우스 위치 (월드 좌표 변환)
- 마우스 클릭
- BuildPhase / CombatPhase에 따라 입력 라우팅

##### `CameraController`
- 우주선 부드럽게 추적 (Cinemachine 추천)

##### `GoldSystem`
- 적 처치 시 골드 추가
- 노드 업그레이드 비용 차감

##### `UIManager`
- HP 바, 골드 표시, 웨이브 번호, Build/Combat 페이즈 UI 전환

### 4-3. 시스템 간 통신 방식
**이벤트 기반**을 추천합니다. 이유:
- 시스템 간 결합도 낮춤 (SOLID 원칙의 D - 의존성 역전)
- 추후 기능 추가 시 다른 시스템 수정 최소화

UnityEvent보다는 C# `event` + `Action<T>` 방식 추천 (인스펙터에서 보이는 게 필요하면 SerializeField로 따로 노출).

```csharp
// 예시: GameManager에서 발행
public static event Action<int> OnWaveStarted;
public static event Action<int> OnWaveCleared;
public static event Action OnWaveFailed;

// Enemy에서 발행
public static event Action<int> OnGoldDropped;
```

---

## 5. 데이터 구조 설계

### 5-1. ScriptableObject 목록

#### `NodeData`
```
- string nodeName
- NodeType type (Core / Special / Attack / Normal)
- Vector2Int size (예: (1,1), (1,2), (2,2))
- int healthContribution (체력 기여도)
- (코어 전용) int powerCapacity
- (공격 전용) FaceDirection attackFace (Top/Bottom/Left/Right)
- (공격 전용) AttackStats baseAttackStats
- (특수 전용) SpecialEffectType effect (Multishot / Pierce)
- (특수 전용) float effectMagnitude
- GameObject visualPrefab (POC: 프리미티브 도형)
- Sprite icon (UI용)
- Color tintColor
```

#### `CardData`
```
- string cardName
- NodeData nodeToGive
- Sprite cardArtwork
- string description
```

#### `EnemyData`
```
- string enemyName
- EnemyTier tier (Normal / Elite / Boss)
- int maxHealth
- float attackRange
- float attackDamage
- float attackInterval
- float moveSpeed
- int goldDropAmount
- GameObject visualPrefab
```

#### `WaveData`
```
- int waveNumber
- WaveSpawnInfo[] spawnInfos
  └ EnemyData enemyType, int count, float spawnDelay, float spawnInterval
- bool isBossWave
```
인스펙터에서 직접 편집 가능. 5개 만들면 됨.

#### `GameConfig`
```
- int startingGold
- AnimationCurve waveDifficultyCurve (참고용)
- float buildPhaseTimeScale (기본 0)
- 그리드 셀 크기 등 전역 상수
```

### 5-2. 런타임 데이터 (ScriptableObject 아님)

#### `PlacedNode` (그리드에 배치된 노드의 인스턴스)
```
- NodeData data
- Vector2Int gridPosition (좌상단 기준)
- int rotationStep (0/90/180/270, 노드 회전)
- int currentUpgradeLevel
- GameObject worldInstance
```

#### `PowerConnection` (동력 연결 한 가닥)
```
- PlacedNode from
- PlacedNode to
- (런타임 계산값) float deliveredPower
```

### 5-3. 그래프 자료구조

```csharp
// 의사 코드
public class PowerGraph
{
    private Dictionary<PlacedNode, List<PlacedNode>> outgoing; // from -> to[]
    private Dictionary<PlacedNode, List<PlacedNode>> incoming; // to -> from[]

    // 주요 메서드
    bool TryAddConnection(PlacedNode from, PlacedNode to);
    void RemoveConnection(PlacedNode from, PlacedNode to);
    void RecalculatePowerDistribution();
    AttackStats GetEffectiveStats(PlacedNode attackNode);
}
```

---

## 6. 까다로운 부분 사전 경고

이 게임에서 구현이 까다로울 부분들을 미리 알려드립니다. **이 부분들은 충분히 시간을 들여 설계 후 구현**하시기 바랍니다.

### 6-1. 그리드 ↔ 월드 좌표 변환 (★★★)
우주선이 회전하기 때문에 단순한 변환이 아닙니다.

```
그리드 좌표 (우주선 기준 로컬) → 우주선의 회전 + 위치 적용 → 월드 좌표
```

- 적 AI가 "가장 가까운 노드"를 찾을 때 월드 좌표 필요
- 발사체 발사 위치/방향 계산할 때 월드 좌표 필요
- 마우스 클릭으로 노드 배치할 때 역변환 필요 (월드 → 그리드)

**해결**: ShipGrid에 변환 메서드를 두고, 우주선 Transform을 참조해 매번 계산. 자주 쓰면 캐싱 고려.

### 6-2. 가변 크기 노드의 인접성 판정 (★★)
1x2 노드와 2x2 노드가 어느 면에서 닿는지 판정하는 로직.

**해결**: 각 노드가 차지하는 셀 목록을 가지고, 셀 단위로 인접성 검사. 인접 셀(상하좌우 4방향)이 다른 노드의 셀이면 인접.

```csharp
// 의사 코드
bool IsAdjacent(PlacedNode a, PlacedNode b)
{
    foreach (var cellA in a.OccupiedCells)
        foreach (var cellB in b.OccupiedCells)
            if (Mathf.Abs(cellA.x - cellB.x) + Mathf.Abs(cellA.y - cellB.y) == 1)
                return true;
    return false;
}
```

### 6-3. 동력 그래프 분리 관리 (★★)
물리 인접 그래프와 동력 그래프를 헷갈리지 않도록 명확히 분리.

**해결**: 클래스 자체를 분리 (`NodeGraph` vs `PowerGraph`). 변수명도 `adjacencyGraph` / `powerGraph`로 구분.

### 6-4. 드래그로 동력선 그리기 UX (★★)
- 노드 중심에서 시작 → 드래그 중 미리보기 라인 표시 → 다른 노드에 놓기
- 연결 가능한 노드 하이라이트 (코어 선택 시 연결 가능한 특수/공격만 강조)
- 잘못된 연결 시도 시 시각 피드백 (빨간 라인 등)

**구현 팁**: LineRenderer 사용. 드래그 중 임시 LineRenderer를 마우스까지 그리고, 확정 시 영구 LineRenderer로 교체.

### 6-5. 발사 방향 합성 (★★)
공격 노드 발사 방향 = `노드의 면 방향 + 노드 회전 + 우주선 회전`

```csharp
// 의사 코드
Vector2 GetWorldFireDirection(PlacedNode attackNode)
{
    Vector2 localFireDir = attackNode.AttackFace.ToVector2(); // (1,0), (0,1) 등
    Quaternion nodeRot = Quaternion.Euler(0, 0, attackNode.RotationStep * 90);
    Vector2 nodeRotated = nodeRot * localFireDir;
    Vector2 worldDir = ship.transform.TransformDirection(nodeRotated);
    return worldDir;
}
```

### 6-6. 웨이브 스냅샷 저장/복원 (★★)
실패 시 이전 웨이브로 돌아가기 위한 상태 저장.

**저장 대상:**
- 우주선 노드 배치 (그리드 데이터)
- 동력 연결 (PowerGraph)
- 노드 업그레이드 레벨
- 덱 카드 목록
- 골드

**해결**: 각 데이터를 직렬화 가능한 구조체(`WaveSnapshot`)에 담아 저장. 매 웨이브 시작 시 1회 저장.

```csharp
[Serializable]
public class WaveSnapshot
{
    public int waveNumber;
    public List<PlacedNodeData> nodes;
    public List<PowerConnectionData> connections;
    public List<string> deckCardIds;
    public int gold;
}
```

### 6-7. 360도 적 스폰 + 카메라 추적 (★)
카메라가 우주선을 따라다니므로, "화면 밖"의 정의가 매번 바뀝니다.

**해결**: 카메라 시야 반지름 + 여유값 거리에 우주선 기준 360도 랜덤 각도로 스폰.

```csharp
Vector3 spawnPos = ship.position + (Vector3)(Random.insideUnitCircle.normalized * spawnRadius);
```

### 6-8. 노드 배치 미리보기 (★)
마우스 위치에 반투명 노드를 표시 + 배치 가능 여부 시각 피드백 (초록/빨강).

**해결**: 미리보기 전용 GameObject 하나 만들어두고, 마우스 따라 이동 + 알파 0.5 머터리얼.

---

## 7. 작업 분할 (Claude Code용 체크리스트)

각 작업은 **이전 작업이 완료된 상태에서 독립적으로 테스트 가능**하도록 정렬했습니다. 각 단계가 끝날 때마다 Unity Play 모드에서 확인하시기 바랍니다.

### 페이즈 1: 데이터 기반
- [ ] **[1-1]** ScriptableObject 정의: `NodeData`, `CardData`, `EnemyData`, `WaveData`, `GameConfig`
- [ ] **[1-2]** Enum 정의: `NodeType`, `FaceDirection`, `EnemyTier`, `SpecialEffectType`, `GameState`
- [ ] **[1-3]** 샘플 ScriptableObject 에셋 생성 (코어 1개, 특수 2개, 공격 1개, 일반 1개, 적 3종, 카드 4종, 웨이브 5개)

### 페이즈 2: 그리드 시스템
- [ ] **[2-1]** `ShipGrid` 클래스: 그리드 데이터, 셀 점유 관리, 가변 크기 노드 지원
- [ ] **[2-2]** 인접 판정 로직 (셀 단위 4방향 검사)
- [ ] **[2-3]** 좌표 변환 메서드 (그리드 ↔ 우주선 로컬 ↔ 월드)
- [ ] **[2-4]** 인스펙터에서 그리드 시각화 (Gizmos)

### 페이즈 3: 노드 배치 시스템
- [ ] **[3-1]** `PlacedNode` 클래스 + 노드 인스턴스 생성/제거
- [ ] **[3-2]** 노드 배치 미리보기 (반투명 + 가능/불가능 색상)
- [ ] **[3-3]** 마우스 클릭으로 배치 (인접 규칙 + 충돌 검증)
- [ ] **[3-4]** 노드 회전 (배치 전 R 키 등으로 90도 회전)
- [ ] **[3-5]** 미리 만들어둔 기본 우주선 형태 자동 배치 (게임 시작 시)

### 페이즈 4: 동력 연결 시스템
- [ ] **[4-1]** `PowerGraph` 클래스 + 연결 추가/제거
- [ ] **[4-2]** 연결 규칙 검증 (코어→공격, 코어→특수, 특수→공격)
- [ ] **[4-3]** 드래그 UI: 노드 클릭 → 드래그 → 다른 노드에 드롭
- [ ] **[4-4]** 연결선 시각화 (LineRenderer)
- [ ] **[4-5]** 동력 분배 계산 (BFS로 체인 추적, 균등 분배)
- [ ] **[4-6]** 공격 노드 EffectiveStats 계산 (베이스 + 특수 효과 + 동력)

### 페이즈 5: 우주선 조작
- [ ] **[5-1]** `ShipController`: 마우스 위치 추적 → 회전
- [ ] **[5-2]** 마우스 클릭 → 전진 (Rigidbody2D 물리 기반)
- [ ] **[5-3]** `CameraController`: 우주선 추적 (Cinemachine 또는 직접)
- [ ] **[5-4]** Build/Combat 페이즈에 따라 조작 활성/비활성

### 페이즈 6: 전투 시스템 - 기본
- [ ] **[6-1]** `HealthSystem`: 단일 체력 풀, 최대 체력 = 노드 합
- [ ] **[6-2]** `Projectile` 클래스 + Object Pool
- [ ] **[6-3]** 공격 노드 자동 발사 (가장 가까운 적 조준)
- [ ] **[6-4]** 발사 방향 합성 (노드 면 + 노드 회전 + 우주선 회전)

### 페이즈 7: 적 시스템
- [ ] **[7-1]** `Enemy` 클래스: 가장 가까운 노드 추적, 사거리 내 정지
- [ ] **[7-2]** 적 원거리 공격 (Projectile로 우주선 노드 타격)
- [ ] **[7-3]** `EnemySpawner`: 화면 밖 360도 스폰
- [ ] **[7-4]** 적 사망 처리 (골드 드롭 이벤트 발행)

### 페이즈 8: 웨이브 + 게임 흐름
- [ ] **[8-1]** `GameManager` 상태 머신 (BuildPhase / CombatPhase / WaveResult / CardSelection)
- [ ] **[8-2]** `WaveManager`: 웨이브 진행, 적 전멸 감지
- [ ] **[8-3]** `WaveSnapshot` 저장/복원 시스템
- [ ] **[8-4]** 웨이브 실패 시 이전 웨이브 복귀

### 페이즈 9: 카드 + 골드
- [ ] **[9-1]** `DeckManager`: 시작 덱 고정 + 카드 추가
- [ ] **[9-2]** `CardSelectionUI`: 3장 표시, 1장 선택
- [ ] **[9-3]** `GoldSystem`: 적 처치 시 골드 추가
- [ ] **[9-4]** 노드 업그레이드 UI (Build Phase에서 노드 클릭 → 버튼)

### 페이즈 10: UI + 마무리
- [ ] **[10-1]** HP 바, 골드 표시, 웨이브 번호 UI
- [ ] **[10-2]** Build/Combat 페이즈 전환 시 UI 변경
- [ ] **[10-3]** "다음 웨이브 시작" 버튼
- [ ] **[10-4]** 게임오버 → 재시작 흐름
- [ ] **[10-5]** POC 통합 테스트 (5웨이브 끝까지 플레이 가능)

### Claude Code에 던질 프롬프트 예시

각 작업 단계마다 이런 식으로 던지면 됩니다:

```
[1-1 작업]
ScriptableObject들을 정의해줘. 다음 5개:
- NodeData
- CardData
- EnemyData
- WaveData
- GameConfig

각 필드는 GAME_DESIGN.md의 5-1 섹션 참고.
모든 필드는 [SerializeField]로 인스펙터에서 편집 가능해야 함.
파일 위치: Assets/Scripts/Data/
```

```
[2-1 작업]
ShipGrid 클래스를 만들어줘.
- 가변 크기 노드 지원 (Vector2Int size)
- 셀 점유 관리 (Dictionary<Vector2Int, PlacedNode> 추천)
- 노드 배치 가능 여부 검증 메서드
- 자세한 책임은 GAME_DESIGN.md 4-2의 ShipGrid 섹션, 까다로운 부분은 6-1, 6-2 참고
```

---

## 8. 코딩 컨벤션

### 네이밍
- 클래스: PascalCase (`ShipGrid`, `PowerGraph`)
- 메서드: PascalCase (`TryAddConnection`)
- 변수/필드: camelCase (`currentHealth`)
- private 필드 prefix 없음 (Unity 공식 가이드)
- public 상수: UPPER_SNAKE_CASE
- ScriptableObject 자산: PascalCase + 카테고리 prefix (`Node_Core`, `Enemy_Boss`)

### 구조
- **싱글톤은 사용하더라도 DontDestroyOnLoad 사용 안 함**
- **리플렉션 사용 안 함**
- 클래스/메서드는 작게 유지 (SOLID 원칙)
- 메서드 하나당 한 가지 책임만

### 인스펙터 노출
- 디버그/밸런싱 위해 가능한 한 모든 수치를 `[SerializeField]`로 노출
- 그룹화는 `[Header("...")]` 사용
- 범위 있는 값은 `[Range(min, max)]`

### 주석
- 메서드마다 무슨 메서드인지 자세히 설명
- 까다로운 로직에는 의도 설명
- 이상한 아이콘 사용 금지

### 이벤트
- C# `event` + `Action<T>` 방식 권장
- 이벤트 이름은 `On~~` 형식

### 성능
- Update에서 매 프레임 GetComponent 금지 (캐싱)
- 발사체는 Object Pool
- 적 AI의 "가장 가까운 노드" 검색은 매 프레임 X, 0.1~0.2초 간격으로 충분
- 가능하면 String 대신 Enum, Hash 사용

### Unity 6 특화
- 새 InputSystem 사용 권장 (구 Input Manager X)
- URP 사용 가정
- Awaitable, async/await 활용 가능 (코루틴 대신)

---

## 부록: 핵심 결정 사항 요약

| 항목 | 결정 |
|------|------|
| 게임 진행 | 인크리멘탈 로그라이크, 5웨이브 |
| 실패 시 | 이전 웨이브로, 우주선/덱/골드 유지 |
| 카드 획득 | 웨이브 클리어 후 3장 중 1장 |
| 노드 배치 | 그리드 기반, 가변 크기, 한 면 인접 필수 |
| 동력 연결 | 드래그로 별도 그리기, 체인 가능 (코어→특수→공격) |
| 동력 분배 | 코어당 용량 균등 분배, 거리 무관 |
| 체력 | 단일 풀 = 노드 체력 합, 노드 파괴 없음 |
| 발사 방향 | 노드 면 방향 + 노드 회전 + 우주선 회전 |
| 우주선 조작 | 마우스 위치 회전, 클릭 전진 |
| 카메라 | 탑다운, 우주선 추적 |
| 적 | 3종(일반/엘리트/보스), 모두 원거리 |
| 적 스폰 | 화면 밖 360도 |
| 골드 | 적 처치 시 드롭 |
| 골드 사용 | 노드 업그레이드 |
| 비주얼 | 프리미티브 + 아이콘/색상 |

---

**문서 끝.** 작업하다 추가로 결정해야 할 사항 생기면 이 문서 갱신해가며 진행하시면 됩니다.
