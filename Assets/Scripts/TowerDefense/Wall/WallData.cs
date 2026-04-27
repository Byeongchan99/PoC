using UnityEngine;

namespace POC4
{
    /// <summary>
    /// 벽 카드 하나의 데이터를 담는 ScriptableObject.
    /// 테트로미노 종류와 벽 효과를 에디터에서 설정할 수 있다.
    /// Create 메뉴: POC4 > Wall Data
    /// </summary>
    [CreateAssetMenu(fileName = "WallData", menuName = "POC4/Wall Data")]
    public class WallData : ScriptableObject
    {
        // -------------------------------------------------------
        // 테트로미노 종류 열거형
        // -------------------------------------------------------

        public enum WallType { I, O, T, S, Z, L, J }

        // -------------------------------------------------------
        // 벽 효과 열거형
        // -------------------------------------------------------

        public enum WallEffectType
        {
            None,           // 효과 없음
            AttackBoost,    // 위에 올린 타워의 공격력 증가
            RangeBoost,     // 위에 올린 타워의 사거리 증가
            AttackSpeedBoost // 위에 올린 타워의 공격 속도 증가
        }

        // -------------------------------------------------------
        // Inspector 노출 필드
        // -------------------------------------------------------

        [Header("Shape")]
        [SerializeField] private WallType _wallType = WallType.I;

        [Header("Effect")]
        [SerializeField] private WallEffectType _effectType = WallEffectType.None;

        [Header("Effect Values (모두 Inspector 수정 가능)")]
        [SerializeField] private float _attackBonus = 5f;
        [SerializeField] private float _rangeBonus = 1f;
        [SerializeField] private float _attackSpeedBonus = 0.5f;

        // -------------------------------------------------------
        // 프로퍼티
        // -------------------------------------------------------

        public WallType Type => _wallType;
        public WallEffectType EffectType => _effectType;
        public float AttackBonus => _attackBonus;
        public float RangeBonus => _rangeBonus;
        public float AttackSpeedBonus => _attackSpeedBonus;

        // -------------------------------------------------------
        // 형태 오프셋 반환
        // -------------------------------------------------------

        /// <summary>
        /// 기본(회전 0) 셀 오프셋 배열을 반환한다.
        /// 좌표 기준점(pivot)은 앵커 셀(0, 0)이며, 각 테트로미노의 중심 근처에 위치한다.
        /// </summary>
        public Vector2Int[] GetBaseOffsets()
        {
            return _wallType switch
            {
                // 4칸 일자
                WallType.I => new[]
                {
                    new Vector2Int(-1, 0), new Vector2Int(0, 0),
                    new Vector2Int(1, 0),  new Vector2Int(2, 0)
                },
                // 2x2 정사각형
                WallType.O => new[]
                {
                    new Vector2Int(0, 0), new Vector2Int(1, 0),
                    new Vector2Int(0, 1), new Vector2Int(1, 1)
                },
                // T자
                WallType.T => new[]
                {
                    new Vector2Int(-1, 0), new Vector2Int(0, 0),
                    new Vector2Int(1, 0),  new Vector2Int(0, 1)
                },
                // S자
                WallType.S => new[]
                {
                    new Vector2Int(0, 0),  new Vector2Int(1, 0),
                    new Vector2Int(-1, 1), new Vector2Int(0, 1)
                },
                // Z자
                WallType.Z => new[]
                {
                    new Vector2Int(-1, 0), new Vector2Int(0, 0),
                    new Vector2Int(0, 1),  new Vector2Int(1, 1)
                },
                // L자
                WallType.L => new[]
                {
                    new Vector2Int(-1, 0), new Vector2Int(0, 0),
                    new Vector2Int(1, 0),  new Vector2Int(1, 1)
                },
                // J자 (L의 거울)
                WallType.J => new[]
                {
                    new Vector2Int(-1, 0), new Vector2Int(0, 0),
                    new Vector2Int(1, 0),  new Vector2Int(-1, 1)
                },
                _ => new[] { new Vector2Int(0, 0) }
            };
        }

        // -------------------------------------------------------
        // 런타임 초기화 (카드 제작 시스템 전용)
        // -------------------------------------------------------

        /// <summary>
        /// 카드 제작 시스템이 ScriptableObject.CreateInstance 로 생성한 인스턴스에 데이터를 설정한다.
        /// 에디터에서 만든 에셋(WallData 파일)에는 절대 호출하지 말 것.
        /// </summary>
        public void Initialize(WallType type, WallEffectType effectType,
            float attackBonus, float rangeBonus, float attackSpeedBonus)
        {
            _wallType = type;
            _effectType = effectType;
            _attackBonus = attackBonus;
            _rangeBonus = rangeBonus;
            _attackSpeedBonus = attackSpeedBonus;
        }

        // -------------------------------------------------------
        // 형태 오프셋 반환
        // -------------------------------------------------------

        /// <summary>
        /// rotationSteps(0~3)번만큼 시계 방향 90도 회전한 오프셋 배열을 반환한다.
        /// 회전 공식: (x, y) → (y, -x) (1회 CW)
        /// </summary>
        public Vector2Int[] GetRotatedOffsets(int rotationSteps)
        {
            // 음수나 4 이상의 값도 안전하게 처리
            rotationSteps = ((rotationSteps % 4) + 4) % 4;
            Vector2Int[] offsets = GetBaseOffsets();

            for (int step = 0; step < rotationSteps; step++)
            {
                for (int i = 0; i < offsets.Length; i++)
                {
                    // 90도 CW: x' = y, y' = -x
                    offsets[i] = new Vector2Int(offsets[i].y, -offsets[i].x);
                }
            }

            return offsets;
        }
    }
}
