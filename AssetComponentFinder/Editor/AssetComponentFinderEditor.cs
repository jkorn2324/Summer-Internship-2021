using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public struct AssetComponentData
{
    public string objectName;
    public string objectPath;
}

public class AssetComponentFinderEditor : EditorWindow
{

    private List<AssetComponentData> assetComponentsSearched = new List<AssetComponentData>();
    private List<AssetComponentData> assetComponentsDisplayed = new List<AssetComponentData>();

    private string searchedComponent = "None";
    private string pathToSearch = "";
    private string searchedPrefab = "";

    private Vector2 scrollViewPosition = Vector2.zero;

    [MenuItem("Tools/Asset Component Finder")]
    public static void ShowEditor()
    {
        EditorWindow.GetWindow(typeof(AssetComponentFinderEditor));
    }

    protected void OnGUI()
    {
        float calculatedMinSizeY = 175.0f;
        float calculatedMaxSizeX = 500.0f;

        GUILayout.Label("Component:", EditorStyles.boldLabel);
        GUILayout.Label("Type the component that you are searching for within your assets.");

        EditorGUILayout.Space();
        this.searchedComponent = EditorGUILayout.TextField(this.searchedComponent);
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        GUILayout.Label("Path:", EditorStyles.boldLabel);
        GUILayout.Label("Input the path that you specifically want to search.");
        EditorGUILayout.Space();
        this.pathToSearch = EditorGUILayout.TextField(this.pathToSearch);

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        bool searchedButton = GUILayout.Button("Search Assets That Use Component");

        if(this.searchedComponent != "None"
            || this.searchedComponent.Length > 0)
        {
            if(searchedButton)
            {
                SearchAssets();
            }

            if(this.assetComponentsSearched.Count > 0)
            {
                EditorGUILayout.Space();

                GUILayout.Label("Search:", EditorStyles.boldLabel);
                GUILayout.Label("Input the prefab that you want to search from results.");
                EditorGUILayout.Space();

                this.searchedPrefab = EditorGUILayout.TextField(this.searchedPrefab);

                EditorGUILayout.Space();
                EditorGUILayout.Space();

                EditorGUILayout.BeginVertical();
                this.scrollViewPosition = EditorGUILayout.BeginScrollView(this.scrollViewPosition);

                GUILayout.Label("Prefabs List", EditorStyles.boldLabel);
                EditorGUILayout.Space();

                DisplayPrefabsList(this.searchedPrefab);

                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.Space();
                GUILayout.Label("No prefabs were found using this component.", EditorStyles.boldLabel);
                calculatedMinSizeY += 30.0f;
            }
        }

        Vector2 minSize = this.maxSize;
        minSize.x = calculatedMaxSizeX;
        if (this.assetComponentsDisplayed.Count > 0)
        {
            calculatedMinSizeY = Mathf.Lerp(calculatedMinSizeY + 35.0f, 700.0f,
                (float)this.assetComponentsDisplayed.Count / 13);
        }
        minSize.y = calculatedMinSizeY;
        this.minSize = this.maxSize = minSize;
    }

    private void SearchAssets()
    {
        this.assetComponentsSearched.Clear();

        string[] objectMetaGUIDs = AssetDatabase.FindAssets("t:Object", new[] { this.pathToSearch });

        foreach(string gUID in objectMetaGUIDs)
        {
            string objectPath = AssetDatabase.GUIDToAssetPath(gUID);
            if (!objectPath.Contains(".prefab"))
            {
                continue;
            }
            int lastIndex = objectPath.LastIndexOf('/') + 1;
            string objectName = objectPath.Substring(
                lastIndex, objectPath.Length - lastIndex);
            Object[] objectAssets = AssetDatabase.LoadAllAssetsAtPath(objectPath);
            foreach(Object obj in objectAssets)
            {
                if(obj is null)
                {
                    continue;
                }
                string type = obj.GetType().Name;
                if(type == this.searchedComponent)
                {
                    AssetComponentData componentData = new AssetComponentData();
                    componentData.objectPath = objectPath;
                    componentData.objectName = objectName;
                    this.assetComponentsSearched.Add(componentData);
                }
            }
        }
    }

    private void DisplayPrefabsList()
    {
        this.assetComponentsDisplayed.Clear();

        foreach(AssetComponentData componentData in this.assetComponentsSearched)
        {
            this.assetComponentsDisplayed.Add(componentData);

            GUILayout.Label("Name: <b>" + componentData.objectName + "</b>");
            GUILayout.Label("Path: <b>" + componentData.objectPath + "</b>");

            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }
    }

    private void DisplayPrefabsList(string searchedPrefab)
    {
        if(searchedPrefab.Trim().Length <= 0)
        {
            DisplayPrefabsList();
            return;
        }

        this.assetComponentsDisplayed.Clear();

        foreach (AssetComponentData componentData in this.assetComponentsSearched)
        {
            if(!componentData.objectName.Contains(searchedPrefab))
            {
                continue;
            }

            this.assetComponentsDisplayed.Add(componentData);

            GUILayout.Label("Name: <b>" + componentData.objectName + "</b>");
            GUILayout.Label("Path: <b>" + componentData.objectPath + "</b>");

            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }

        if(assetComponentsDisplayed.Count <= 0)
        {
            GUILayout.Label("No Assets Found With Name '" + searchedPrefab + "'");
            
            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }
    }
}
