using UnityEngine;

namespace POC4
{
    /// <summary>
    /// 벽 배치 관련 UI를 담당하는 클래스.
    ///
    /// - 테스트 팔레트: 7종 테트로미노를 선택하는 버튼 표시
    ///   (카드 시스템(4단계) 구현 전까지 사용하는 임시 UI)
    /// - 상태 표시: 현재 배치 상태 안내 텍스트
    /// - 확정/취소 버튼: Placing/Dropped 상태에서 표시
    ///
    /// IMGUI(OnGUI) 방식 사용 (POC에서 Canvas 없이 빠르게 구현).
    /// </summary>
    public class WallPlacementUI : MonoBehaviour
    {
        // -------------------------------------------------------
        // Inspector 노출 필드
        // -------------------------------------------------------

        [Header("References")]
        [SerializeField] private WallPlacer _wallPlacer;

        [Header("Wall Data Assets (각 종류별 ScriptableObject 연결)")]
        [SerializeField] private WallData _wallDataI;
        [SerializeField] private WallData _wallDataO;
        [SerializeField] private WallData _wallDataT;
        [SerializeField] private WallData _wallDataS;
        [SerializeField] private WallData _wallDataZ;
        [SerializeField] private WallData _wallDataL;
        [SerializeField] private WallData _wallDataJ;

        // -------------------------------------------------------
        // 내부 상태
        // -------------------------------------------------------

        // OnGUI에서 그린 UI 영역 (WallPlacer가 월드 클릭과 구분하는 데 사용)
        private Rect _uiRect = new Rect(10, 10, 190, 520);

        // -------------------------------------------------------
        // 프로퍼티
        // -------------------------------------------------------

        /// <summary>
        /// 마우스가 UI 영역 위에 있는지 여부.
        /// Input.mousePosition은 좌측 하단 기준이므로 GUI 좌표(좌측 상단)로 변환한다.
        /// </summary>
        public bool IsMouseOverUI
        {
            get
            {
                Vector2 guiMouse = new Vector2(
                    Input.mousePosition.x,
                    Screen.height - Input.mousePosition.y
                );
                return _uiRect.Contains(guiMouse);
            }
        }

        // -------------------------------------------------------
        // IMGUI 렌더링
        // -------------------------------------------------------

        private void OnGUI()
        {
            if (_wallPlacer == null) return;

            GUILayout.BeginArea(_uiRect);

            if (_wallPlacer.State == WallPlacer.PlacerState.Idle)
            {
                DrawPalette();
            }
            else
            {
                DrawPlacingUI();
            }

            GUILayout.EndArea();
        }

        // -------------------------------------------------------
        // 팔레트 UI (Idle 상태)
        // -------------------------------------------------------

        /// <summary>
        /// 7종 테트로미노 버튼을 그린다.
        /// WallData 에셋이 연결되지 않은 경우 해당 버튼은 비활성화.
        /// </summary>
        private void DrawPalette()
        {
            GUILayout.Label("[ 벽 선택 (테스트 팔레트) ]");
            GUILayout.Space(4);

            DrawWallButton("I형 벽 (4칸 일자)", _wallDataI);
            DrawWallButton("O형 벽 (2×2)", _wallDataO);
            DrawWallButton("T형 벽 (T자)", _wallDataT);
            DrawWallButton("S형 벽 (S자)", _wallDataS);
            DrawWallButton("Z형 벽 (Z자)", _wallDataZ);
            DrawWallButton("L형 벽 (L자)", _wallDataL);
            DrawWallButton("J형 벽 (J자, 역L)", _wallDataJ);

            GUILayout.Space(8);
            GUILayout.Label("우클릭: 90도 회전");
            GUILayout.Label("좌클릭: 드롭 (겹침 없을 때)");
        }

        /// <summary>
        /// 단일 벽 선택 버튼을 그린다.
        /// data가 null이면 버튼을 비활성화한다.
        /// </summary>
        private void DrawWallButton(string label, WallData data)
        {
            GUI.enabled = data != null;
            if (GUILayout.Button(label) && data != null)
            {
                _wallPlacer.StartPlacing(data);
            }
            GUI.enabled = true;
        }

        // -------------------------------------------------------
        // 배치 중 UI (Placing / Dropped 상태)
        // -------------------------------------------------------

        /// <summary>
        /// 현재 배치 상태에 따른 안내 텍스트와 확정/취소 버튼을 그린다.
        /// </summary>
        private void DrawPlacingUI()
        {
            // 상태 안내 텍스트
            if (_wallPlacer.State == WallPlacer.PlacerState.Placing)
            {
                GUILayout.Label("[ 배치 중 ]");
                GUILayout.Label("초록: 드롭 가능");
                GUILayout.Label("빨강: 겹침 / 범위 초과");
                GUILayout.Label("우클릭: 회전");
                GUILayout.Label("좌클릭: 드롭");
            }
            else // Dropped
            {
                GUILayout.Label("[ 드롭됨 - 경로 검증 완료 ]");

                if (_wallPlacer.IsCurrentValid)
                    GUILayout.Label("초록: 설치 가능 (경로 열림)");
                else
                    GUILayout.Label("빨강: 경로 차단 - 위치 조정 필요");

                GUILayout.Label("좌클릭: 다시 들어올리기");
            }

            GUILayout.Space(8);

            // 확정 버튼: Dropped이고 유효할 때만 활성화
            bool canConfirm = _wallPlacer.State == WallPlacer.PlacerState.Dropped
                              && _wallPlacer.IsCurrentValid;
            GUI.enabled = canConfirm;
            if (GUILayout.Button("설치 확정"))
            {
                _wallPlacer.Confirm();
            }
            GUI.enabled = true;

            GUILayout.Space(4);

            if (GUILayout.Button("취소"))
            {
                _wallPlacer.Cancel();
            }
        }
    }
}
