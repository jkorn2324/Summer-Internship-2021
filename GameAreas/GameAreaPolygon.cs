using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a point within the game zone 2D defined object.
/// </summary>
[System.Serializable]
public class GameAreaPoint
{
    public enum PointOrientation
    {
        COUNTERCLOCKWISE,
        COLINEAR,
        CLOCKWISE
    }

    [SerializeField]
    public float x = 0.0f;
    [SerializeField]
    public float z = 0.0f;
    [SerializeField, HideInInspector]
    public Color color = Color.red;

    private float _xOffset = 0.0f;
    private float _zOffset = 0.0f;

    public float XOffset
    {
        get => _xOffset;
        set => _xOffset = value;
    }

    public float ZOffset
    {
        get => _zOffset;
        set => _zOffset = value;
    }

    public float CalculatedX
        => x + _xOffset;

    public float CalculatedZ
        => z + _zOffset;

    public GameAreaPoint(float x, float z)
    {
        this.x = x;
        this.z = z;
        this.color = Color.red;
    }

    public Vector3 ToVector3(float y = 0.0f)
    {
        return new Vector3(CalculatedX, y, CalculatedZ);
    }

    public bool IsOnSegment(GameAreaPoint segmentA, GameAreaPoint segmentB)
    {
        return IsOnSegment(this, segmentA, segmentB);
    }

    public PointOrientation GetOrientationOnSegment(GameAreaPoint segmentA, GameAreaPoint segmentB)
    {
        return GetOrientation(this, segmentA, segmentB);
    }
    public static bool SegmentsIntersect(GameAreaPoint segment1A, GameAreaPoint segment1B,
        GameAreaPoint segment2A, GameAreaPoint segment2B)
    {
        PointOrientation orientation1 = segment1A.GetOrientationOnSegment(
            segment1B, segment2A);
        PointOrientation orientation2 = segment1A.GetOrientationOnSegment(
            segment1B, segment2B);
        PointOrientation orientation3 = segment2A.GetOrientationOnSegment(
            segment2B, segment1A);
        PointOrientation orientation4 = segment2A.GetOrientationOnSegment(
            segment2B, segment1B);

        if(orientation1 != orientation2
            && orientation3 != orientation4)
        {
            return true;
        }

        if(orientation1 == PointOrientation.COLINEAR
            && segment1A.IsOnSegment(segment2A, segment1B))
        {
            return true;
        }

        if(orientation2 == PointOrientation.COLINEAR
            && segment1A.IsOnSegment(segment2B, segment1B))
        {
            return true;
        }

        if(orientation2 == PointOrientation.COLINEAR
            && segment2A.IsOnSegment(segment1A, segment2B))
        {
            return true;
        }

        if (orientation4 == PointOrientation.COLINEAR
            && segment2A.IsOnSegment(segment1B, segment2B))
        {
            return true;
        }

        return false;
    }

    public static bool IsOnSegment(GameAreaPoint point,
        GameAreaPoint segmentPointA, GameAreaPoint segmentPointB, float distanceThreshold = 0.0f)
    {
        bool passed = point.CalculatedX <= Mathf.Max(segmentPointA.CalculatedX, segmentPointB.CalculatedX)
            && point.CalculatedX >= Mathf.Min(segmentPointA.CalculatedX, segmentPointB.CalculatedX)
            && point.CalculatedZ <= Mathf.Max(segmentPointA.CalculatedZ, segmentPointB.CalculatedZ)
            && point.CalculatedZ >= Mathf.Min(segmentPointA.CalculatedZ, segmentPointB.CalculatedZ);
        if(distanceThreshold <= 0.0f)
        {
            return passed;
        }

        Vector3 segmentDirection = (segmentPointB.ToVector3() - segmentPointA.ToVector3()).normalized;
        Vector3 differenceVector = point.ToVector3() - segmentPointA.ToVector3();
        float scalarProjection = Vector3.Dot(differenceVector, segmentDirection);
        Vector3 pointOnSegment = segmentDirection * scalarProjection + segmentPointA.ToVector3();
        GameAreaPoint testPoint = new GameAreaPoint(pointOnSegment.x, pointOnSegment.z);
        if(!IsOnSegment(testPoint, segmentPointA, segmentPointB))
        {
            return false;
        }

        float distance = Vector3.Distance(pointOnSegment, point.ToVector3());
        return distance <= distanceThreshold;
    }

    public static PointOrientation GetOrientation(GameAreaPoint point,
        GameAreaPoint segmentPointA, GameAreaPoint segmentPointB)
    {
        float value = (segmentPointA.CalculatedZ - point.CalculatedZ) * (segmentPointB.CalculatedX - segmentPointA.CalculatedX)
            - (segmentPointA.CalculatedX - point.CalculatedX) * (segmentPointB.CalculatedZ - segmentPointA.CalculatedZ);
        int castedValue = (int)value;
        if (castedValue == 0 || System.Single.IsNaN(castedValue))
        {
            return PointOrientation.COLINEAR;
        }
        return (castedValue > 0) ? PointOrientation.CLOCKWISE 
            : PointOrientation.COUNTERCLOCKWISE;
    }
}

/// <summary>
/// Represents a polygon.
/// </summary>
public class GameAreaPolygon : IGameAreaShape
{
    public class GameAreaPointNode
    {
        public GameAreaPoint currentPoint;
        public GameAreaPointNode nextPoint;

        public bool PointIntersectsOnSegment(Vector3 point, GameAreaPointNode startPoint)
        {
            return PointIntersectsOnSegment(
                new GameAreaPoint(point.x, point.z), startPoint);
        }

        public bool PointIntersectsOnSegment(GameAreaPoint point, GameAreaPointNode startPoint)
        {
            GameAreaPoint nextPoint = this.nextPoint?.currentPoint ?? startPoint?.currentPoint;
            return point.IsOnSegment(currentPoint, nextPoint);
        }

        public bool SegmentIntersectsWithSegment(GameAreaPoint a, GameAreaPoint b, GameAreaPointNode startPoint)
        {
            GameAreaPoint nextPoint = this.nextPoint?.currentPoint ?? startPoint?.currentPoint;
            return GameAreaPoint.SegmentsIntersect(a, b, currentPoint, nextPoint);
        }

        public GameAreaPoint.PointOrientation PointOrientationOnSegment(GameAreaPoint point, GameAreaPointNode startPoint)
        {
            GameAreaPoint nextPoint = this.nextPoint?.currentPoint ?? startPoint?.currentPoint;
            return currentPoint.GetOrientationOnSegment(point, nextPoint);
        }
    }

    private GameAreaPointNode _startPoint = null;
    private GameAreaPointNode _currentAddedPoint = null;
    private bool _polygonComplete = false;

    private List<GameAreaPolygon> _triangles = new List<GameAreaPolygon>();

    private int _numberOfPoints = 0;
    private float _offsetX = 0.0f, _offsetZ = 0.0f;

    public bool IsValidPolygon => _numberOfPoints >= 3;

    public GameAreaPointNode CurrentAddedPoint => _currentAddedPoint;

    public GameAreaPoint StartPoint => _startPoint?.currentPoint;

    public bool PolygonCompleted => _polygonComplete;

    public void SetPolygonComplete()
    {
        if (_polygonComplete
            || !IsValidPolygon)
        {
            return;
        }

        if (_numberOfPoints > 3)
        {
            GameAreaPoint initialPoint = StartPoint;
            GameAreaPointNode secondPoint = _startPoint?.nextPoint;
            GameAreaPointNode thirdPoint = secondPoint?.nextPoint;
            while (thirdPoint != null)
            {
                GameAreaPolygon triangle = CreateTriangle(
                    initialPoint, secondPoint?.currentPoint, thirdPoint?.currentPoint);
                _triangles.Add(triangle);
                secondPoint = thirdPoint;
                thirdPoint = thirdPoint?.nextPoint;
            }
        }
        _polygonComplete = true;
    }

    public void ApplyOffset(Vector3 offset)
    {
        if(_offsetX != offset.x
            || _offsetZ != offset.z)
        {
            GameAreaPointNode pointNode = _startPoint;
            while (pointNode != null)
            {
                pointNode.currentPoint.XOffset = offset.x;
                pointNode.currentPoint.ZOffset = offset.z;
                pointNode = pointNode.nextPoint;
            }
        }
        _offsetX = offset.x;
        _offsetZ = offset.z;
    }

    public void AddPoint(Vector3 point)
    {
        AddPoint(new GameAreaPoint(point.x, point.z));
    }

    public void AddPoint(GameAreaPoint point)
    {
        if(!_polygonComplete)
        {
            AddPointForIncompletePolygon(point);
        }
        else
        {
            AddPointForCompletePolygon(point);
        }
    }

    private void AddPointForCompletePolygon(GameAreaPoint point)
    {
        GameAreaPointNode currentNode = _startPoint;
        while(currentNode != null)
        {
            if(currentNode.PointIntersectsOnSegment(point, _startPoint))
            {
                break;
            }
            currentNode = currentNode.nextPoint;
        }

        if(currentNode != null)
        {
            GameAreaPointNode addedNode = new GameAreaPointNode();
            addedNode.currentPoint = point;
            addedNode.nextPoint = currentNode.nextPoint;
            currentNode.nextPoint = addedNode;
            _currentAddedPoint = addedNode;
        }
    }
    private void AddPointForIncompletePolygon(GameAreaPoint point)
    {
        if (_currentAddedPoint == null)
        {
            _currentAddedPoint = new GameAreaPointNode();
            _currentAddedPoint.currentPoint = point;

            if (_startPoint == null)
            {
                _startPoint = _currentAddedPoint;
            }
            _numberOfPoints++;
            return;
        }

        GameAreaPointNode nextNode = new GameAreaPointNode();
        nextNode.currentPoint = point;
        _currentAddedPoint.nextPoint = nextNode;
        _currentAddedPoint = nextNode;
        _numberOfPoints++;
    }

    public Vector3? RandomPointInShape(float yPosition = 0.0f)
    {
        if(!IsValidPolygon)
        {
            return null;
        }

        if (_numberOfPoints == 3)
        {
            GameAreaPoint initialPoint = StartPoint;
            GameAreaPointNode secondPoint = _startPoint.nextPoint;
            GameAreaPointNode thirdPoint = secondPoint.nextPoint;
            float randomValue1 = Random.value;
            float randomValue2 = Random.value;
            float randomX = (1 - Mathf.Sqrt(randomValue1)) * initialPoint.CalculatedX +
                (Mathf.Sqrt(randomValue1) * (1 - randomValue2)) * secondPoint.currentPoint.CalculatedX +
                (Mathf.Sqrt(randomValue1) * randomValue2) * thirdPoint.currentPoint.CalculatedX;
            float randomZ = (1 - Mathf.Sqrt(randomValue1)) * initialPoint.CalculatedZ +
                (Mathf.Sqrt(randomValue1) * (1 - randomValue2)) * secondPoint.currentPoint.CalculatedZ +
                (Mathf.Sqrt(randomValue1) * randomValue2) * thirdPoint.currentPoint.CalculatedZ;
            return new Vector3(randomX, yPosition, randomZ);
        }
        GameAreaPolygon randomTriangle = _triangles[Random.Range(0, _triangles.Count)];
        return randomTriangle.RandomPointInShape(yPosition);
    }

    private GameAreaPolygon CreateTriangle(GameAreaPoint a, GameAreaPoint b, GameAreaPoint c)
    {
        GameAreaPolygon polygon = new GameAreaPolygon();
        polygon.AddPoint(a);
        polygon.AddPoint(b);
        polygon.AddPoint(c);
        polygon.SetPolygonComplete();
        return polygon;
    }

    public bool IsWithinShape(Vector3 point)
    {
        return IsWithinShape(new GameAreaPoint(point.x, point.z));
    }

    public bool IsWithinShape(GameAreaPoint point)
    {
        if (!IsValidPolygon)
        {
            return false;
        }

        // Segment from Point to endPoint.
        GameAreaPoint endPoint = new GameAreaPoint(point.x +
            1000000.0f, point.z);
        GameAreaPointNode currentPoint = _startPoint;
        int numberOfIntersections = 0;

        do
        {
            if (currentPoint.SegmentIntersectsWithSegment(point, endPoint, _startPoint))
            {
                GameAreaPoint.PointOrientation orientation = currentPoint.PointOrientationOnSegment(point, _startPoint);
                if (orientation == GameAreaPoint.PointOrientation.COLINEAR)
                {
                    return currentPoint.PointIntersectsOnSegment(point, _startPoint);
                }
                numberOfIntersections++;
            }
            currentPoint = currentPoint.nextPoint;
        } while (currentPoint != null);

        return numberOfIntersections % 2 == 1;
    }

    public IEnumerator<GameAreaPoint> GetEnumerator()
    {
        return new GameZonePolygonEnumerator(_startPoint);
    }

    /// <summary>
    /// The game zone polygon enumerator to iterate through the points like
    /// a foreach list.
    /// </summary>
    public class GameZonePolygonEnumerator : IEnumerator<GameAreaPoint>
    {

        private bool _startPointMoved = false;
        private GameAreaPointNode _currentPoint;
        private GameAreaPointNode _startPoint;
        public GameAreaPoint Current
            => _currentPoint?.currentPoint;

        object IEnumerator.Current
            => _currentPoint?.currentPoint;

        public GameZonePolygonEnumerator(GameAreaPointNode point)
        {
            _currentPoint = point;
            _startPoint = point;
        }

        public void Dispose() { }

        public bool MoveNext()
        {
            if(_currentPoint != null
                && _currentPoint.nextPoint != null)
            {
                // Makes sure that we iterate through the start
                // point as well in the foreach loop.
                if(!_startPointMoved)
                {
                    _startPointMoved = true;
                    return true;
                }
                _currentPoint = _currentPoint.nextPoint;
                return true;
            }
            return false;
        }

        public void Reset()
        {
            _startPointMoved = false;
            _currentPoint = _startPoint;
        }
    }
}