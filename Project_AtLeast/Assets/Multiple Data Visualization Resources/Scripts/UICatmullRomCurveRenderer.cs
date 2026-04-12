using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class UICatmullRomCurveRenderer : Graphic
{
    public List<Transform> controlPointsObjects = new List<Transform>();
    private List<Vector2> controlPoints = new List<Vector2>();
    public float thickness = 2f;
    private int segmentsPerCurve = 20;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        UpdateControlPoints();
        if (controlPoints.Count < 2) return;

        // 添加虚拟控制点
        List<Vector2> extendedPoints = new List<Vector2>(controlPoints);
        extendedPoints.Insert(0, controlPoints[0]); // 在开始添加一个额外的点
        extendedPoints.Add(controlPoints[controlPoints.Count - 1]); // 在结束添加一个额外的点

        List<Vector2> points = new List<Vector2>();

        for (int i = 0; i < extendedPoints.Count - 3; i++)
        {
            Vector2 p0 = extendedPoints[i];
            Vector2 p1 = extendedPoints[i + 1];
            Vector2 p2 = extendedPoints[i + 2];
            Vector2 p3 = extendedPoints[i + 3];

            if (i == 0) // 第一段曲线，添加第一个控制点
            {
                points.Add(CalculateCatmullRomPosition(0, p0, p1, p2, p3));
            }

            for (int j = 1; j <= segmentsPerCurve; j++)
            {
                float t = j / (float)segmentsPerCurve;
                points.Add(CalculateCatmullRomPosition(t, p0, p1, p2, p3));
            }
        }

        DrawCurve(vh, points);
    }

    void Update()
    {
        SetVerticesDirty(); // 实时更新曲线
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

    private Vector2 CalculateCatmullRomPosition(float t, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        Vector2 a = 0.5f * (2f * p1);
        Vector2 b = 0.5f * (p2 - p0);
        Vector2 c = 0.5f * (2f * p0 - 5f * p1 + 4f * p2 - p3);
        Vector2 d = 0.5f * (-p0 + 3f * p1 - 3f * p2 + p3);

        Vector2 pos = a + (b * t) + (c * t * t) + (d * t * t * t);

        return pos;
    }

    private void DrawCurve(VertexHelper vh, List<Vector2> points)
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
