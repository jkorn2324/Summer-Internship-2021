using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameAreaCircle : IGameAreaShape
{

    private float _radius;
    private GameAreaPoint _centerPoint;

    public GameAreaCircle(GameAreaPoint point, float radius)
    {
        _centerPoint = point;
        _radius = radius;
    }

    public void ApplyOffset(Vector3 offset)
    {
        _centerPoint.XOffset = offset.x;
        _centerPoint.ZOffset = offset.z;
    }

    public bool IsWithinShape(Vector3 point)
    {
        return IsWithinShape(new GameAreaPoint(point.x, point.z));
    }

    public bool IsWithinShape(GameAreaPoint point)
    {
        Vector3 pointCenter = point.ToVector3();
        float distance = Vector3.Distance(pointCenter, _centerPoint.ToVector3());
        return distance <= _radius;
    }

    public Vector3? RandomPointInShape(float yPosition = 0.0f)
    {
        float randomValue = Random.value;
        float theta = 2.0f * Mathf.PI * randomValue;
        float sqrtOfRandom = Mathf.Sqrt(randomValue);
        return new Vector3(
            _centerPoint.CalculatedX + sqrtOfRandom * _radius * Mathf.Cos(theta),
            yPosition,
            _centerPoint.CalculatedZ + sqrtOfRandom * _radius * Mathf.Sin(theta));
    }
}
