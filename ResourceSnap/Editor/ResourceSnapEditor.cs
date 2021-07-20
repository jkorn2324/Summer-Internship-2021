using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ResourceSnapEditor : EditorWindow
{

    private Dictionary<Transform, Vector3> offsets = new Dictionary<Transform, Vector3>();
    private List<Transform> currentSelection = new List<Transform>();
    private Vector3 masterOffset = Vector3.zero;

    private Vector3 scrollViewPosition = Vector3.zero;


    [MenuItem("Tools/Resource Snap")]
    public static void ShowEditor()
    {
        EditorWindow.GetWindow(typeof(ResourceSnapEditor));
    }

    protected void OnGUI()
    {
        this.maxSize = new Vector2(500, 700);
        if (this.currentSelection.Count >= 13)
        {
            this.minSize = this.maxSize;
        }
        else
        {
            float calculatedMinSizeY = 75.0f;
            Vector2 minSize = this.maxSize;

            if (this.currentSelection.Count > 0)
            {
                calculatedMinSizeY = Mathf.Lerp(110.0f, 700.0f,
                    (float)this.currentSelection.Count / 13);
            }
            minSize.y = calculatedMinSizeY;
            this.minSize = this.maxSize = minSize;
        }

        if (GUILayout.Button("Snap Selected To Ground"))
        {
            SnapSelection();
        }
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        GUILayout.Label("Master Snap Offset", EditorStyles.boldLabel);
        this.masterOffset = EditorGUILayout.Vector3Field("", this.masterOffset);
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if (this.currentSelection.Count > 0)
        {
            EditorGUILayout.BeginVertical();
            this.scrollViewPosition = EditorGUILayout.BeginScrollView(this.scrollViewPosition);

            GUILayout.Label("Individual Offsets", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            DrawLabels();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
    }
    private void DrawLabels()
    {
        foreach (Transform transform in this.currentSelection)
        {
            if(!transform)
            {
                continue;
            }
            if (!this.offsets.ContainsKey(transform))
            {
                this.offsets.Add(transform, Vector3.zero);
            }
            GUILayout.Label("<b>" + transform.name + "</b>");
            this.offsets[transform] = EditorGUILayout.Vector3Field("", this.offsets[transform]);
            EditorGUILayout.Space();
        }
    }

    protected void OnSelectionChange()
    {
        ReloadSelectedTransforms();
    }

    private void SnapSelection()
    {
        ReloadSelectedTransforms();

        for (int i = 0; i < this.currentSelection.Count; i++)
        {
            Transform t = this.currentSelection[i];
            if (t && t.gameObject)
            {
                SnapTransform(t);
            }
        }
    }

    private void ReloadSelectedTransforms()
    {
        ClearTransforms();

        foreach (Transform selectedTransform in Selection.transforms)
        {
            AddTransformToSelection(selectedTransform);
        }
    }

    private void SnapTransform(Transform transform)
    {
        Vector3 offset = this.masterOffset;
        if(this.offsets.ContainsKey(transform))
        {
            offset += this.offsets[transform];
        }
        GameObjectSnapper.SnapPosition(transform, offset);
    }

    private void AddTransformToSelection(Transform selectedTransform)
    {
        if(selectedTransform)
        {
            if(!this.offsets.ContainsKey(selectedTransform))
            {
                this.offsets.Add(selectedTransform, Vector3.zero);
            }
            this.currentSelection.Add(selectedTransform);
        }
    }

    private void ClearTransforms()
    {
        for(int i = this.currentSelection.Count - 1; i >= 0; i--)
        {
            this.currentSelection.RemoveAt(i);
        }
    }
}
