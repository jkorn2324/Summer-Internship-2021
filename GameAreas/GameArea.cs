using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class GameArea : MonoBehaviour
{
    [SerializeField]
    public GameAreaCreationEditorData editorData;
    [SerializeField]
    private GameAreaPolygonData polygonData;
    [SerializeField]
    private GameAreaCircleData circleData;

    private IGameAreaShape _areaShape;

    private Vector3? _randomPoint = null;

    public GameAreaPolygonData PolygonData
    => polygonData;

    public IGameAreaShape CurrentShape
        => _areaShape;

    private void Start()
    {
        _areaShape = GenerateAreaShape();
        RandomPoint();
    }

    private IGameAreaShape GenerateAreaShape()
    {
        switch (editorData.areaShapeType)
        {
            case GameAreaShapeType.SHAPE_CIRCLE:
                return circleData.CreateCircle();
            case GameAreaShapeType.SHAPE_POLYGON:
                return polygonData.CreateFromPoints();
        }
        return null;
    }
    private void Update()
    {
        if (Application.isEditor
            && !Application.isPlaying)
        {
            UpdateEditor();
        }
        _areaShape?.ApplyOffset(this.transform.position);
    }
    private void UpdateEditor()
    {
        _areaShape = GenerateAreaShape();
    }

    public Vector3? RandomPoint()
    {
        _randomPoint = _areaShape.RandomPointInShape(this.transform.position.y);
        return _randomPoint;
    }

    #region gizmos

#if UNITY_EDITOR

    /// <summary>
    /// Draws the gizmos for the game zone.
    /// </summary>
    private void OnDrawGizmos()
    {
        switch (editorData.areaShapeType)
        {
            case GameAreaShapeType.SHAPE_POLYGON:
                DrawPolygon();
                break;
            case GameAreaShapeType.SHAPE_CIRCLE:
                DrawCircle();
                break;
        }

        DrawRandomPoint();
    }

    private void DrawPolygon()
    {
        if (polygonData == null
            || polygonData.GameAreaPoints == null)
        {
            return;
        }
        polygonData.ApplyOffsets(this.transform.position);

        GameAreaPoint prevPoint = null;
        foreach (GameAreaPoint point in polygonData.GameAreaPoints)
        {
            Gizmos.color = point.color;
            Gizmos.DrawSphere(point.ToVector3(), 0.5f);
            DrawPointLine(prevPoint, point);
            prevPoint = point;
        }

        if (editorData.areaEditMode != GameAreaEditMode.ADD_POINTS_MODE
            && polygonData.GameAreaPoints.Count > 0)
        {
            DrawPointLine(prevPoint, polygonData.GameAreaPoints[0]);
        }
    }

    private void DrawPointLine(GameAreaPoint prev, GameAreaPoint next)
    {
        if (prev != null)
        {
            Gizmos.color = Color.black;
            Vector3 prevPosition = prev.ToVector3();
            Vector3 nextPosition = next.ToVector3();
            Gizmos.DrawLine(prevPosition, nextPosition);
        }
    }

    private void DrawCircle()
    {
        circleData.ApplyOffset(this.transform.position);

        GameAreaPoint point = circleData.AreaCenterPoint;
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(point.ToVector3(), 0.5f);

        Handles.color = Color.black;
        Handles.DrawWireDisc(point.ToVector3(), Vector3.up, circleData.AreaRadius);
    }

    private void DrawRandomPoint()
    {
        if(_randomPoint.HasValue)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(_randomPoint.Value, 0.5f);
        }
    }

#endif

    #endregion
}
