using UnityEngine;
using UnityEngine.UI;

// 스프라이트 없이 아래를 향한 역삼각형을 그리는 UI 그래픽.
// 질문창 서랍의 손잡이(탭)로 쓰인다. 메시로 직접 그리므로 크기를 아무리 키워도 계단현상이 없다.
[AddComponentMenu("UI/Triangle Graphic")]
public class TriangleGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect r = GetPixelAdjustedRect();
        Color32 c = color;

        // 좌상 - 우상 - 하단중앙 순서로 삼각형 하나
        vh.AddVert(new Vector3(r.xMin, r.yMax), c, new Vector2(0f, 1f));
        vh.AddVert(new Vector3(r.xMax, r.yMax), c, new Vector2(1f, 1f));
        vh.AddVert(new Vector3(r.center.x, r.yMin), c, new Vector2(0.5f, 0f));
        vh.AddTriangle(0, 1, 2);
    }
}
