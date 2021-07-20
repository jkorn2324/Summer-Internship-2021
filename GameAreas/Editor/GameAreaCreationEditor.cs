using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GameArea))]
[CanEditMultipleObjects]
public class GameAreaCreationEditor : Editor
{

    private const float MIN_DISTANCE_TO_POINT = 5.0f;

    #region fields

    private SerializedProperty _polygonData;
    private SerializedProperty _areaPoints;

    private SerializedProperty _circleData;
    private SerializedProperty _editorData;

    private GameAreaPoint _selectedPoint = null;

    #endregion

    #region properties
    private GameArea GameAreaTarget => target as GameArea;
    private bool HasSelectedPoint => _selectedPoint != null;

    #endregion

    private void OnEnable()
    {
        _circleData = serializedObject.FindProperty("circleData");
        _polygonData = serializedObject.FindProperty("polygonData");
        _areaPoints = _polygonData.FindPropertyRelative("gameAreaPoints");
        _editorData = serializedObject.FindProperty("editorData");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_editorData.FindPropertyRelative("areaShapeType"));

        bool generateRandomPoint = GUILayout.Button("Generate Random Point");
        if(generateRandomPoint)
        {
            GameAreaTarget.RandomPoint();
        }

        switch (GameAreaTarget.editorData.areaShapeType)
        {
            case GameAreaShapeType.SHAPE_POLYGON:
                DrawPolygon();
                break;
            case GameAreaShapeType.SHAPE_CIRCLE:
                DrawCircle();
                break;
        }
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawCircle()
    {
        EditorGUILayout.Separator();
        EditorGUILayout.PropertyField(_circleData, new GUIContent("Circle Area Data"), EditorStyles.boldFont);
    }

    private void DrawPolygon()
    {
        EditorGUILayout.Separator();
        EditorGUILayout.PropertyField(_editorData.FindPropertyRelative("areaEditMode"), new GUIContent("Polygon Edit Mode"));

        if (GameAreaTarget.editorData.areaEditMode != GameAreaEditMode.VIEW_MODE)
        {
            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(_areaPoints, new GUIContent("Area Points"));
        }
    }
    protected virtual void OnSceneGUI()
    {
        if (GameAreaTarget)
        {
            HandleInput();
        }
    }

    private void HandleInput()
    {
        Event currentEvent = Event.current;
        int controlID = GUIUtility.GetControlID(FocusType.Keyboard);

        switch (currentEvent.GetTypeForControl(controlID))
        {
            case EventType.MouseDown:
                HandleMouseDownInScene(controlID, currentEvent);
                break;
            case EventType.MouseUp:
                HandleMouseUpInScene(controlID, currentEvent);
                break;
            case EventType.MouseDrag:
                HandleMouseDragInScene(controlID, currentEvent);
                break;
        }
    }

    private void HandleMouseDownInScene(int controlID, Event @event)
    {
        if (@event.button != 0)
        {
            return;
        }

        Vector2 mousePos = @event.mousePosition;
        Vector3 worldPos = GetWorldMousePosition(mousePos);

        if (GameAreaTarget.editorData.areaEditMode == GameAreaEditMode.ADD_POINTS_MODE)
        {
            Undo.RegisterCompleteObjectUndo(GameAreaTarget, "Add Point to Area");
            _selectedPoint = GameAreaTarget.PolygonData.AddPoint(
                worldPos, GameAreaTarget.transform.position, MIN_DISTANCE_TO_POINT);
            GUIUtility.hotControl = controlID;
            @event.Use();
        }
        else if (GameAreaTarget.editorData.areaEditMode == GameAreaEditMode.EDIT_POINTS_MODE)
        {
            float minimumDistance;
            GameAreaPoint closestPoint = GameAreaTarget.PolygonData.GetClosestPoint(
                worldPos, out minimumDistance);
            if (minimumDistance <= MIN_DISTANCE_TO_POINT)
            {
                _selectedPoint = closestPoint;
                _selectedPoint.color = Color.green;
                GUIUtility.hotControl = controlID;
                @event.Use();
            }
            else if (HasSelectedPoint)
            {
                _selectedPoint.color = Color.red;
                _selectedPoint = null;
                GUIUtility.hotControl = controlID;
                @event.Use();
            }
        }
        else if (GameAreaTarget.editorData.areaEditMode == GameAreaEditMode.DELETE_POINTS_MODE)
        {
            float minimumDistance;
            GameAreaPoint closestPoint = GameAreaTarget.PolygonData.GetClosestPoint(
                worldPos, out minimumDistance);
            if (minimumDistance <= MIN_DISTANCE_TO_POINT)
            {
                Undo.RegisterCompleteObjectUndo(GameAreaTarget, "Remove Point in Area");
                GameAreaTarget.PolygonData.RemovePoint(closestPoint);
                GUIUtility.hotControl = controlID;
                @event.Use();
            }
        }
    }

    private void HandleMouseUpInScene(int controlID, Event @event)
    {
        if (@event.button != 0)
        {
            return;
        }

        if (GameAreaTarget.editorData.areaEditMode == GameAreaEditMode.EDIT_POINTS_MODE
            || GameAreaTarget.editorData.areaEditMode == GameAreaEditMode.ADD_POINTS_MODE)
        {
            if(HasSelectedPoint)
            {
                _selectedPoint.color = Color.red;
                _selectedPoint = null;
            }
            GUIUtility.hotControl = 0;
            @event.Use();
        }
    }

    private void HandleMouseDragInScene(int controlID, Event @event)
    {
        if (@event.button != 0)
        {
            return;
        }

        if (GameAreaTarget.editorData.areaEditMode == GameAreaEditMode.EDIT_POINTS_MODE
            || GameAreaTarget.editorData.areaEditMode == GameAreaEditMode.ADD_POINTS_MODE)
        {
            if(HasSelectedPoint)
            {
                Vector3 newPosition = GetWorldMousePosition(@event.mousePosition);
                float calculatedPositionX = newPosition.x - _selectedPoint.XOffset;
                float calculatedPositionZ = newPosition.z - _selectedPoint.ZOffset;
                
                _selectedPoint.x = calculatedPositionX;
                _selectedPoint.z = calculatedPositionZ;

                GUIUtility.hotControl = controlID;
                @event.Use();
            }
        }
    }

    private Vector3 GetWorldMousePosition(Vector3 mousePos)
    {
        Ray guiWorldRay = HandleUtility.GUIPointToWorldRay(mousePos);
        RaycastHit hit;
        if (Physics.Raycast(guiWorldRay, out hit, Mathf.Infinity))
        {
            return hit.point;
        }

        Plane plane = new Plane(Vector3.up, 1.0f);
        float distanceEnter;
        if(plane.Raycast(guiWorldRay, out distanceEnter))
        {
            return guiWorldRay.GetPoint(distanceEnter);
        }
        return Vector3.zero;
    }
}
