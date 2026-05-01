using UnityEngine;

namespace POC6
{
    /// <summary>
    /// 노드 비주얼 게임오브젝트를 생성하는 유틸리티 클래스입니다.
    /// 프리팹이 없을 때 SpriteRenderer 기반 2D 오브젝트를 만듭니다.
    /// NodePlacer, DefaultShipSetup 등 여러 곳에서 공통으로 사용합니다.
    /// </summary>
    public static class NodeVisualFactory
    {
        // 1x1 흰색 스프라이트를 한 번만 생성해서 재사용
        private static Sprite _whiteSprite;

        /// <summary>
        /// PlacedNode의 위치/회전/크기에 맞는 2D 노드 비주얼을 생성합니다.
        /// NodeData에 VisualPrefab이 있으면 인스턴스화하고, 없으면 SpriteRenderer로 대체합니다.
        /// </summary>
        public static GameObject CreateNodeVisual(PlacedNode node, ShipGrid grid, Transform parent)
        {
            Vector3 worldPos = grid.NodeCenterToWorld(node);
            Quaternion rotation = grid.transform.rotation
                * Quaternion.Euler(0f, 0f, node.RotationStep * 90f);

            GameObject obj;

            if (node.Data.VisualPrefab != null)
            {
                obj = Object.Instantiate(node.Data.VisualPrefab, worldPos, rotation, parent);
            }
            else
            {
                obj = CreateSpriteObject(
                    worldPos,
                    rotation,
                    parent,
                    node.GetRotatedSize(),
                    grid.CellSize,
                    node.Data.TintColor
                );
            }

            obj.name = $"Node_{node.Data.NodeName}_{node.GridPosition}";
            return obj;
        }

        /// <summary>
        /// 미리보기용 반투명 2D 오브젝트를 생성합니다.
        /// initialColor의 알파값으로 반투명하게 시작합니다.
        /// 콜라이더 없이 SpriteRenderer만 가집니다.
        /// </summary>
        public static GameObject CreatePreviewVisual(NodeData nodeData, float cellSize, Color initialColor)
        {
            if (nodeData.VisualPrefab != null)
            {
                var prefabInstance = Object.Instantiate(nodeData.VisualPrefab);

                // 콜라이더 제거
                foreach (var col in prefabInstance.GetComponentsInChildren<Collider2D>())
                    Object.Destroy(col);

                // 프리팹의 모든 SpriteRenderer에 반투명 색상 적용
                foreach (var sr in prefabInstance.GetComponentsInChildren<SpriteRenderer>())
                    sr.color = initialColor;

                return prefabInstance;
            }

            var obj = CreateSpriteObject(
                Vector3.zero,
                Quaternion.identity,
                null,
                nodeData.Size,
                cellSize,
                initialColor  // 처음부터 반투명 색상으로 생성
            );

            // 미리보기는 콜라이더 불필요
            var boxCol = obj.GetComponent<BoxCollider2D>();
            if (boxCol != null) Object.Destroy(boxCol);

            return obj;
        }

        /// <summary>
        /// SpriteRenderer 컴포넌트를 가진 2D 오브젝트를 생성합니다.
        /// </summary>
        private static GameObject CreateSpriteObject(
            Vector3 position, Quaternion rotation, Transform parent,
            Vector2Int size, float cellSize, Color color)
        {
            var obj = new GameObject();
            obj.transform.SetParent(parent);
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.transform.localScale = new Vector3(
                size.x * cellSize * 0.9f,
                size.y * cellSize * 0.9f,
                1f
            );

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = GetWhiteSprite();
            sr.color = color;

            obj.AddComponent<BoxCollider2D>();
            return obj;
        }

        /// <summary>
        /// 1x1 흰색 Sprite를 반환합니다. 최초 호출 시 한 번만 생성됩니다.
        /// </summary>
        private static Sprite GetWhiteSprite()
        {
            if (_whiteSprite != null) return _whiteSprite;

            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();

            _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return _whiteSprite;
        }
    }
}
