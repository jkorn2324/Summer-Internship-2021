using System.Collections;
using System.Collections.Generic;
using UnityEngine;


#region editor

public enum GameAreaEditMode
{
    VIEW_MODE,
    EDIT_POINTS_MODE,
    ADD_POINTS_MODE,
    DELETE_POINTS_MODE
}

public enum GameAreaShapeType
{
    SHAPE_POLYGON,
    SHAPE_CIRCLE
}

[System.Serializable]
public class GameAreaCreationEditorData
{
    [SerializeField]
    public GameAreaEditMode areaEditMode = GameAreaEditMode.VIEW_MODE;
    [SerializeField]
    public GameAreaShapeType areaShapeType = GameAreaShapeType.SHAPE_POLYGON;
}

#endregion

[System.Serializable]
public class GameAreaPolygonData
{

    [SerializeField]
    private List<GameAreaPoint> gameAreaPoints;

    public List<GameAreaPoint> GameAreaPoints => gameAreaPoints;

    public GameAreaPoint AddPoint(Vector3 point, Vector3 parentTransformPos, float distanceThreshold = 0.0f)
    {
        GameAreaPoint newPoint = new GameAreaPoint(
            point.x - parentTransformPos.x, point.z - parentTransformPos.z);
        newPoint.XOffset = parentTransformPos.x;
        newPoint.ZOffset = parentTransformPos.z;

        for (int i = 0; i < gameAreaPoints.Count - 1; i++)
        {
            GameAreaPoint prev = gameAreaPoints[i];
            GameAreaPoint next = gameAreaPoints[i + 1];

            if(GameAreaPoint.IsOnSegment(newPoint, prev, next, distanceThreshold))
            {
                gameAreaPoints.Insert(i + 1, newPoint);
                return newPoint;
            }
        }
        gameAreaPoints.Add(newPoint);
        return newPoint;
    }

    public void RemovePoint(GameAreaPoint point)
    {
        if(gameAreaPoints.Contains(point))
        {
            gameAreaPoints.Remove(point);
        }
    }

    public void ApplyOffsets(Vector3 offset)
    {
        if(gameAreaPoints == null)
        {
            return;
        }
        foreach(GameAreaPoint point in gameAreaPoints)
        {
            if(point != null)
            {
                point.XOffset = offset.x;
                point.ZOffset = offset.z;
            }
        }
    }

    /// <summary>
    /// Gets the closest GameZonePoint to the point.
    /// </summary>
    /// <param name="point">The point.</param>
    /// <param name="distance">The closest distance.</param>
    /// <returns>A Game Zone point.</returns>
    public GameAreaPoint GetClosestPoint(Vector3 point, out float distance)
    {
        if(gameAreaPoints.Count <= 0)
        {
            distance = 0.0f;
            return null;
        }

        Vector3 pointAtZeroY = point;
        pointAtZeroY.y = 0.0f;
        GameAreaPoint firstPoint = gameAreaPoints[0];
        distance = Vector3.Distance(pointAtZeroY, firstPoint.ToVector3());

        if (gameAreaPoints.Count == 1)
        {
            return firstPoint;
        }

        for(int i = 1; i < gameAreaPoints.Count; i++)
        {
            GameAreaPoint testPoint = gameAreaPoints[i];
            float distanceToInitial = Vector3.Distance(pointAtZeroY, testPoint.ToVector3());

            if(distanceToInitial < distance)
            {
                distance = distanceToInitial;
                firstPoint = testPoint;
            }
        }
        return firstPoint;
    }

    /// <summary>
    /// Creates a game zone polygon from points.
    /// </summary>
    /// <returns>A game zone polygon.</returns>
    public GameAreaPolygon CreateFromPoints()
    {
        GameAreaPolygon polygon = new GameAreaPolygon();
        if(gameAreaPoints != null)
        {
            foreach (GameAreaPoint gameZonePoint in gameAreaPoints)
            {
                polygon.AddPoint(gameZonePoint);
            }
        }
        polygon.SetPolygonComplete();
        return polygon;
    }
}

[System.Serializable]
public class GameAreaCircleData
{
    [SerializeField]
    private float areaRadius = 1.0f;
    [SerializeField]
    private GameAreaPoint areaCenter;

    public GameAreaPoint AreaCenterPoint
        => areaCenter;

    public float AreaRadius => areaRadius;
    public void ApplyOffset(Vector3 offset)
    {
        areaCenter.XOffset = offset.x;
        areaCenter.ZOffset = offset.z;
    }

    public GameAreaCircle CreateCircle()
    {
        return new GameAreaCircle(areaCenter, areaRadius);
    }

}