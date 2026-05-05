using NGAME;
using NGAME.Editor;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Source - https://stackoverflow.com/q/71498153
// Posted by dw218192
// Retrieved 2026-04-05, License - CC BY-SA 4.0

[CustomEditor(typeof(ScenePreviewCapture))]
public class ScenePreviewCaptureInspector : Editor
{
    

    Camera _cam = null;
    RenderTexture _renderTexture;
    Texture2D _tex2d;
    Scene _scene;

    // preview variables
    // world space (orthographicSize)
    int _renderTextureHeight;// = 1080;

    SceneData _currentSceneData;
   

    void DrawRefScene()
    {
        float aspectRatio = _currentSceneData.Bounds.AspectRatio;
        _renderTexture = new RenderTexture(Mathf.RoundToInt(aspectRatio * _renderTextureHeight), _renderTextureHeight, 16);
        _cam.targetTexture = _renderTexture;
        _cam.Render();
        _tex2d = new Texture2D(_renderTexture.width, _renderTexture.height, TextureFormat.RGBA32, false);
        _tex2d.Apply(false);
        Graphics.CopyTexture(_renderTexture, _tex2d);
    }

    Vector2 GetGUIPreviewSize()
    {
        float aspectRatio = _currentSceneData.Bounds.AspectRatio;
        float height = _currentSceneData.Bounds.Height();
        Vector2 camSizeWorld = new Vector2(height * aspectRatio, height);
        float scaleFactor = EditorGUIUtility.currentViewWidth / camSizeWorld.x;
        return new Vector2(EditorGUIUtility.currentViewWidth, scaleFactor * camSizeWorld.y);
    }

    #region Init
    void OnEnable()
    {
        void OpenSceneDelay()
        {
            EditorApplication.delayCall -= OpenSceneDelay;
            DrawRefScene();
        }
        ScenePreviewCapture scenePreviewTest = target as ScenePreviewCapture;

        //SerializedProperty aspectRatioProperty = serializedObject.FindProperty("AspectRatio");
        //_aspectChoiceIdx = (SupportedAspects)aspectRatioProperty.intValue;

        _scene = EditorSceneManager.OpenPreviewScene(scenePreviewTest.scenePath);
        _currentSceneData = CreateSceneData(_scene, "", scenePreviewTest.scenePath);
        InitPreviewCamera();

        SerializedProperty renderProperty = serializedObject.FindProperty("RenderTextureHeight");
        _renderTextureHeight = renderProperty.intValue;

        EditorApplication.delayCall += OpenSceneDelay;
    }

    void InitPreviewCamera()
    {
        if(_currentSceneData == null)
        {
            _cam = null;
            return;
        }
        _cam = _scene.GetRootGameObjects()[0].GetComponentInChildren<Camera>();

        _cam.cameraType = CameraType.Preview;
        _cam.orthographic = true;

        Vector2 position2d = _currentSceneData.Bounds.CenterPoint;
        Vector3 camPosition = new Vector3(position2d.x, 25.0f, position2d.y);
        Quaternion camRotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);

        _cam.scene = _scene;
        _cam.transform.SetPositionAndRotation(camPosition, camRotation);
        _cam.aspect = _currentSceneData.Bounds.AspectRatio;

        _cam.orthographicSize = _currentSceneData.Bounds.Height();
    }

    void OnDisable()
    {
        EditorSceneManager.ClosePreviewScene(_scene);
    }
    #endregion

    void OnCamSettingChange(ScenePreviewCapture sceneToPreview)
    {
        _renderTextureHeight = Math.Max(_renderTextureHeight, 2);
        SerializedProperty renderHeightProperty = serializedObject.FindProperty("RenderTextureHeight");
        renderHeightProperty.intValue = _renderTextureHeight;
        

        EditorUtility.SetDirty(serializedObject.targetObject);
        AssetDatabase.SaveAssetIfDirty(serializedObject.targetObject);

        if(_cam == null)
        {
            return;
        }
        InitPreviewCamera();
        DrawRefScene();
    }

    public override void OnInspectorGUI()
    {
        // draw serializedObject fields
        // ....

        ScenePreviewCapture sceneToPreview = target as ScenePreviewCapture;
        SceneAsset oldScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(sceneToPreview.scenePath);

        serializedObject.Update();

        EditorGUI.BeginChangeCheck();
        SceneAsset newScene = EditorGUILayout.ObjectField("scene", oldScene, typeof(SceneAsset), false) as SceneAsset;

        if (EditorGUI.EndChangeCheck())
        {
            string newPath = AssetDatabase.GetAssetPath(newScene);
            SerializedProperty scenePathProperty = serializedObject.FindProperty("scenePath");
            scenePathProperty.stringValue = newPath;
            EditorSceneManager.ClosePreviewScene(_scene);

            _scene = EditorSceneManager.OpenPreviewScene(newPath);
            _currentSceneData = CreateSceneData(_scene, "", newPath);
            
            OnCamSettingChange(sceneToPreview);
        }

        // display options
        
        using (var scope = new EditorGUI.ChangeCheckScope())
        {

            _renderTextureHeight = EditorGUILayout.IntField("Render Texture Height", _renderTextureHeight);

            if (scope.changed)
            {
                OnCamSettingChange(sceneToPreview);
            }
        }

        if (_tex2d != null)
        {
            _tex2d.filterMode = FilterMode.Point;
            Vector2 size = GetGUIPreviewSize();
            Rect guiRect = EditorGUILayout.GetControlRect(false,
                GUILayout.Height(size.y),
                GUILayout.ExpandHeight(false));
            EditorGUI.DrawPreviewTexture(guiRect, _tex2d);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private SceneData CreateSceneData(Scene aScene, string sceneGuid, string filePath)
    {
        SceneData result = new();
        result.Name = aScene.name;
        result.Guid = sceneGuid;
        result.FilePath = filePath;

        List<RegionConnectionData> conectionObjects = new();
        List<SpawnerData> spawners = new();

        bool bConnectionsFound = false;
        bool bSpawnersFound = false;

        GameObject[] rootObjects = aScene.GetRootGameObjects();

        foreach (GameObject obj in rootObjects)
        {
            // connection data
            IEncounterRegionConnector[] connectorComponent = obj.GetComponentsInChildren<IEncounterRegionConnector>();
            if (connectorComponent.Length > 0) bConnectionsFound = true;

            foreach (IEncounterRegionConnector connection in connectorComponent)
            {
                //connections.Add(component.GetRegionConnectionData());
                RegionConnectionData data = connection.GetRegionConnectionData();
                conectionObjects.Add(data);
            }

            // spawner data

            ISpawnPoint[] spawnerComponents = obj.GetComponentsInChildren<ISpawnPoint>();
            if (spawnerComponents.Length > 0) bSpawnersFound = true;

            foreach (ISpawnPoint spawner in spawnerComponents)
            {
                spawners.Add(spawner.GetSpawnerData());
            }

        }

        if (!bConnectionsFound && !bSpawnersFound)
        {
            Debug.Log("No IEncounterRegionConnector or ISpawnPoint components found in scene: " + aScene.name);
        }
        else
        {
            Debug.Log("Scene: " + aScene.name + " contains target data types");
        }

        result.UniqueConnectionObjects = conectionObjects;
        result.SpawnPoints = spawners;
        result.Bounds = new(conectionObjects, spawners);

        return result;
    }
}

