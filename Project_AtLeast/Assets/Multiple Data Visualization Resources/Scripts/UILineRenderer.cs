using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(CanvasRenderer))]
public class UILineRenderer : Graphic
{
    public List<Transform> controlPointsObjects = new List<Transform>(); // 控制点对象列表
    private List<Vector2> controlPoints = new List<Vector2>(); // 控制点位置列表
    public float thickness = 2f; // 线条粗细

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear(); // 清除之前的顶点数据
        UpdateControlPoints(); // 更新控制点位置
        if (controlPoints.Count < 2) return; // 至少需要两个点来绘制线段

        DrawLines(vh, controlPoints);
    }

    void Update()
    {
        SetVerticesDirty(); // 实时更新线段
    }

    private void UpdateControlPoints()
    {
        controlPoints.Clear();
        foreach (Transform controlPointObject in controlPointsObjects)
        {
            if (controlPointObject != null)
            {
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, controlPointObject.position);
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, canvas.worldCamera, out localPoint);
                controlPoints.Add(localPoint);
            }
        }
    }

    private void DrawLines(VertexHelper vh, List<Vector2> points)
    {
        for (int i = 0; i < points.Count - 1; i++)
        {
            AddVerticesForLineSegment(vh, points[i], points[i + 1], thickness);
        }
    }

    private void AddVerticesForLineSegment(VertexHelper vh, Vector2 start, Vector2 end, float thickness)
    {
        Vector2 direction = (end - start).normalized;
        Vector2 normal = new Vector2(-direction.y, direction.x) * thickness / 2;
        vh.AddVert(start + normal, color, new Vector2(0, 0));
        vh.AddVert(start - normal, color, new Vector2(0, 1));
        vh.AddVert(end - normal, color, new Vector2(1, 1));
        vh.AddVert(end + normal, color, new Vector2(1, 0));

        int baseIndex = vh.currentVertCount;
        vh.AddTriangle(baseIndex - 4, baseIndex - 3, baseIndex - 2);
        vh.AddTriangle(baseIndex - 2, baseIndex - 1, baseIndex - 4);
    }
}
