using UnityEngine;

namespace POC4
{
    /// <summary>
    /// 벽 배치 중 그리드 위에 표시되는 반투명 미리보기.
    /// 배치 가능이면 초록색, 불가능이면 빨간색으로 각 셀을 표시한다.
    /// 테트로미노는 항상 4칸이므로 4개의 SpriteRenderer를 미리 생성해 재사용한다.
    /// </summary>
    public class WallPreview : MonoBehaviour
    {
        // -------------------------------------------------------
        // Inspector 노출 필드
        // -------------------------------------------------------

        [SerializeField] private Color _validColor = new Color(0f, 1f, 0f, 0.5f);
        [SerializeField] private Color _invalidColor = new Color(1f, 0f, 0f, 0.5f);

        // -------------------------------------------------------
        // 내부 상태
        // -------------------------------------------------------

        // 테트로미노는 항상 4칸
        private const int CellCount = 4;
        private SpriteRenderer[] _cellRenderers;

        // -------------------------------------------------------
        // 유니티 생명주기
        // -------------------------------------------------------

        private void Awake()
        {
            InitializeCellRenderers();
        }

        // -------------------------------------------------------
        // 초기화
        // -------------------------------------------------------

        /// <summary>
        /// 4개의 셀 렌더러를 미리 생성한다.
        /// 매 프레임 Instantiate/Destroy 없이 활성/비활성으로 재사용.
        /// </summary>
        private void InitializeCellRenderers()
        {
            Sprite sprite = CreateWhiteSprite();
            _cellRenderers = new SpriteRenderer[CellCount];

            for (int i = 0; i < CellCount; i++)
            {
                GameObject cellObj = new GameObject($"PreviewCell_{i}");
                cellObj.transform.SetParent(transform);

                SpriteRenderer sr = cellObj.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                // 미리보기는 벽보다 위에 표시 (sortingOrder 높게)
                sr.sortingOrder = 10;

                cellObj.SetActive(false);
                _cellRenderers[i] = sr;
            }
        }

        // -------------------------------------------------------
        // 미리보기 갱신 / 숨기기
        // -------------------------------------------------------

        /// <summary>
        /// 미리보기를 갱신한다.
        /// anchorCell + offsets[i] 위치에 셀을 배치하고, 유효 여부에 따라 색상을 변경한다.
        /// </summary>
        public void Show(Vector2Int[] offsets, Vector2Int anchorCell, bool isValid, GridSystem gridSystem)
        {
            Color color = isValid ? _validColor : _invalidColor;
            float scale = gridSystem.CellSize * 0.9f;

            for (int i = 0; i < CellCount; i++)
            {
                if (i < offsets.Length)
                {
                    Vector2Int targetCell = anchorCell + offsets[i];
                    GameObject cellObj = _cellRenderers[i].gameObject;
                    cellObj.SetActive(true);
                    cellObj.transform.position = gridSystem.GridToWorldPosition(targetCell);
                    cellObj.transform.localScale = Vector3.one * scale;
                    _cellRenderers[i].color = color;
                }
                else
                {
                    _cellRenderers[i].gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 미리보기를 숨긴다. 배치 취소 또는 확정 후 호출.
        /// </summary>
        public void Hide()
        {
            foreach (SpriteRenderer sr in _cellRenderers)
            {
                sr.gameObject.SetActive(false);
            }
        }

        // -------------------------------------------------------
        // 스프라이트 생성
        // -------------------------------------------------------

        /// <summary>
        /// 4×4 픽셀 흰색 텍스처로 스프라이트를 생성한다.
        /// pixelsPerUnit = 4이므로 스프라이트 크기 = 1×1 Unity 단위.
        /// </summary>
        private Sprite CreateWhiteSprite()
        {
            const int size = 4;
            Texture2D tex = new Texture2D(size, size) { filterMode = FilterMode.Point };
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
