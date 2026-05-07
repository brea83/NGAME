using NGAME;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace NGAME.Editor
{
    /// <summary>
    /// Settings window for the Nodegraph Adventure Map Editor
    /// This is where you can see which scenes contain the data that is required to be compatible with this tool,
    ///  and toggle which scenes are included in the graph tool's scene dropdown.
    /// </summary>
    public class NGAMESettings : EditorWindow
    {
        private SO_NGAME_Settings m_Settings;

        [SerializeField] private int m_CompatibleListSelectedIndex = -1;
        private int m_IncompatibleListSelectedIndex = -1;
        private VisualElement m_RootScrollElement;
        //private VisualElement m_GraphPanel;

        private ListView m_CompatibleScenesListView;
        private VisualElement m_CompatibleScenesRightPane;
        
        private ListView m_IncompatibleScenesListView;
        private VisualElement m_IncompatibleScenesRightPane;

        private ObjectField m_SettingsSelectorField;

        private Dictionary<string, SceneInclusionData> m_GuidToSceneData = new();
        private Dictionary<string, string> m_GuidToDescriptionInSettings = new();

        private List<SceneInclusionData> m_CompatibleScenes = new();
        private List<SceneInclusionData> m_UncompatibleScenes = new();
        private List<string> m_SavedScenesNotInProject = new();

        private StyleSheet m_Styles;
        public SO_NGAME_Settings CurrentSettings { get => m_Settings; }
        //private bool m_SettingsAreLoaded = false;
        private static void ShowEditorFromSO()
        {
            NGAMESettings wnd = GetWindow<NGAMESettings>();
            wnd.titleContent = new GUIContent("NGAME Settings");

            // Limit size of the window.
            wnd.minSize = new Vector2(450, 200);
        }

        [MenuItem("NGAME/Settings")]
        public static void ShowMyEditor()
        {
            // This method is called when the user selects the menu item in the Editor.
            NGAMESettings wnd = GetWindow<NGAMESettings>();
            wnd.titleContent = new GUIContent("NGAME Settings");

            // Limit size of the window.
            wnd.minSize = new Vector2(450, 200);

            string[] settingsGuids = AssetDatabase.FindAssets("t:SO_NGAME_Settings");
            
            SO_NGAME_Settings settings = LoadOrCreateSettingsFile(settingsGuids.ToList());
            
            wnd.m_Settings = settings;
            wnd.m_SettingsSelectorField.SetValueWithoutNotify(settings);
            wnd.LoadSettingsObject();
            wnd.PopulateSceneList(wnd.m_CompatibleScenesListView, wnd.m_CompatibleScenes);
            wnd.PopulateSceneList(wnd.m_IncompatibleScenesListView, wnd.m_UncompatibleScenes);
        }

        private static SO_NGAME_Settings LoadOrCreateSettingsFile(List<string> settingsGuids)
        {
            string projectPath = null;
            foreach (string guid in settingsGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("Assets/"))
                {
                    projectPath = path;
                    break;
                }
            }

            if (!string.IsNullOrEmpty(projectPath))
                return AssetDatabase.LoadAssetAtPath<SO_NGAME_Settings>(projectPath);

            SO_NGAME_Settings settings = SO_NGAME_Settings.CreateInstance<SO_NGAME_Settings>();

            AssetDatabase.CreateAsset(settings, "Assets/NGAME_Settings.asset");
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssetIfDirty(settings);

            return settings;
        }

        [OnOpenAssetAttribute(1)]
        public static bool OpenEditorFromSO(UnityEngine.EntityId entityID, int line)
        {
            //SO_NGAME_Settings settings = EditorUtility.EntityIdToObject(entityID) as SO_NGAME_Settings;
            string filepath = AssetDatabase.GetAssetPath(entityID);
            System.Type assetType = AssetDatabase.GetMainAssetTypeAtPath(filepath);
            if (assetType == typeof(SO_NGAME_Settings))
            {
                ShowEditorFromSO();
                return false;
            }

            return false;
        }

        [OnOpenAssetAttribute(2)]
        public static bool InitEditorFromSO(UnityEngine.EntityId entityID, int line)
        {
            string filepath = AssetDatabase.GetAssetPath(entityID);
            System.Type assetType = AssetDatabase.GetMainAssetTypeAtPath(filepath);
            if (assetType == typeof(SO_NGAME_Settings))
            {
                SO_NGAME_Settings settings = AssetDatabase.LoadAssetAtPath<SO_NGAME_Settings>(filepath);
                if(settings != null)
                {
                    //Debug.Log("NGAME SETTINGS is recieving initial settings values");
                    NGAMESettings wnd = GetWindow<NGAMESettings>();
                    wnd.m_Settings = settings;
                    wnd.m_SettingsSelectorField.SetValueWithoutNotify(settings);
                    wnd.LoadSettingsObject();
                    wnd.PopulateSceneList(wnd.m_CompatibleScenesListView, wnd.m_CompatibleScenes);
                    wnd.PopulateSceneList(wnd.m_IncompatibleScenesListView, wnd.m_UncompatibleScenes);
                    return true;
                }
            }
            return false;
        }

        private void OnEnable()
        {
            if(m_Settings != null)
            {
                LoadSettingsObject();
                PopulateSceneList(m_CompatibleScenesListView, m_CompatibleScenes);
                PopulateSceneList(m_IncompatibleScenesListView, m_UncompatibleScenes);
            }
        }

        private void OnDestroy()
        {
            if(m_Settings != null)
            {
                //Debug.Log("SAVING SETTINGS ASSET");
                AssetDatabase.SaveAssetIfDirty(m_Settings);
            }
            //Debug.Log("NGAME SETTINGS WINDOW ON DESTROY");
        }

        public void CreateGUI()
        {
            //Debug.Log("NGAME SETTINGS . CreateGUI() called");
            

            m_RootScrollElement = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            rootVisualElement.Add(m_RootScrollElement);

            string[] guids = AssetDatabase.FindAssets("NGAMESettingsStyle  t:StyleSheet");
            if (guids.Length > 0)
            {
                m_Styles = AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            m_RootScrollElement.Add(CreateGeneralSettingsPanel());
            m_RootScrollElement.Add(CreateSceneSelectionPanel());
            m_RootScrollElement.Add(CreateIncompatibleScenesPanel());
        }

        private ObjectField CreateSettingsObjectField()
        {
            var objectField = new ObjectField("Settings File");
            objectField.objectType = typeof(SO_NGAME_Settings);

            objectField.SetValueWithoutNotify(m_Settings);

            objectField.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue is SO_NGAME_Settings)
                {
                    m_Settings = evt.newValue as SO_NGAME_Settings;
                    LoadSettingsObject();
                    PopulateSceneList(m_CompatibleScenesListView, m_CompatibleScenes);
                    PopulateSceneList(m_IncompatibleScenesListView, m_UncompatibleScenes);
                }

            });

            return objectField;
        }

        private void LoadSettingsObject()
        {
            if(m_Settings == null)
            {
                return;
            }

            if (m_GuidToSceneData == null)
            {
                m_GuidToSceneData = new Dictionary<string, SceneInclusionData>();
                //Debug.Log("Settings Window's Guid To Scene Data dictionary was null so making a new one");
            }
            else
                m_GuidToSceneData.Clear();

            if (m_Settings.Scenes == null)
            {
                //Debug.Log("Settings Object's SceneData list was null so making a new one");
                m_Settings.Scenes = new List<SceneInclusionData>();
            }
            if (m_Settings.Guids == null)
            {
                //Debug.Log("Settings Object's scene GUIDs list was null so making a new one");
                m_Settings.Guids = new List<string>();
            }

            if(m_Settings.Guids.Count() != m_Settings.Scenes.Count())
            {
                Debug.LogError("Somehow the list of guids and their associated scene data arent the same length, this shouldn't be possible");
                return;
            }
            // check for deleted or changed scenes
            for(int i = 0; i < m_Settings.Guids.Count(); i++)
            {
                string guidKey = m_Settings.Guids[i];
                SceneInclusionData data = m_Settings.Scenes[i];

                string filePath = AssetDatabase.GUIDToAssetPath(guidKey);
                data.FilePath = filePath;

                
                m_GuidToSceneData.Add(guidKey, data);
            }
            InitializeSceneDataLookups();

        }

        

        private VisualElement CreateGeneralSettingsPanel()
        {
            VisualElement panel = new VisualElement();
            panel.AddToClassList("settingsSubSection");
            
            if(m_Styles != null)
                panel.styleSheets.Add(m_Styles);

            Label title = new Label();
            title.text = "NGAME Settings";
            title.AddToClassList("title1");
            panel.Add(title);

            TextElement subtitle = new TextElement();
            subtitle.text = "Node Graph Adventure Map Editor";
            subtitle.AddToClassList("subtitle1");
            panel.Add(subtitle);

            Label header = new Label();
            header.text = "General Settings";
            header.AddToClassList("header1");
            panel.Add(header);

            m_SettingsSelectorField = CreateSettingsObjectField();
            m_SettingsSelectorField.AddToClassList("indentLevel1");
            m_SettingsSelectorField.SetEnabled(false);
            panel.Add(m_SettingsSelectorField);

            TextElement tempSetting = new TextElement();
            //tempSetting.text = "This is where settings about what classes to look for in scenes will go. Current Default is to look for Door and Spawner interfaces.";
            tempSetting.text = "Settings for which scenes the graph editor will include for use.";
            tempSetting.text += " Also shows a list of scenes that do not have the required interfaces on any of their objects.";
            tempSetting.text += " Currently the tool only requires the scene to have at least one IEncounterRegionConnector.";
            tempSetting.AddToClassList("indentLevel1");
            
            panel.Add(tempSetting);

            return panel;
        }

        private VisualElement CreateSceneSelectionPanel()
        {
            Foldout panel = new ();
            panel.text = "Scene Selection";
            if (m_Styles != null)
                panel.styleSheets.Add(m_Styles);
            panel.AddToClassList("header1");

            // Create a two-pane view with the left pane being fixed.
            var splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
            panel.Add(splitView);

            VisualElement leftPanel = new VisualElement();
            leftPanel.name = "leftPane";
            leftPanel.AddToClassList("leftPane");
            splitView.Add(leftPanel);

            Label CompatibleScenesLabel = new Label();
            CompatibleScenesLabel.name = "CompatibleScenesHeader";
            CompatibleScenesLabel.text = "Compatible Scenes";
            CompatibleScenesLabel.AddToClassList("header2");
            leftPanel.Add(CompatibleScenesLabel);

            m_CompatibleScenesListView = new ListView();
            leftPanel.Add(m_CompatibleScenesListView);

            VisualElement rightPanel = new VisualElement();
            splitView.Add(rightPanel);

            Label rightPanelLabel = new Label();
            rightPanelLabel.text = "Options";
            rightPanelLabel.AddToClassList("header2");
            rightPanel.Add(rightPanelLabel);

            m_CompatibleScenesRightPane = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            rightPanel.Add(m_CompatibleScenesRightPane);
            //m_ScenesRightPane.style.minHeight = 300;

            PopulateSceneList(m_CompatibleScenesListView, m_CompatibleScenes);
            m_CompatibleScenesListView.selectedIndex = m_CompatibleListSelectedIndex;
            m_CompatibleScenesListView.selectionChanged += (items) => 
            {
                OnSceneSelectionChanged(items, m_CompatibleScenesRightPane);
                m_CompatibleListSelectedIndex = m_CompatibleScenesListView.selectedIndex; 
            };

            

            return panel;
        }

        private VisualElement CreateIncompatibleScenesPanel()
        {
            Foldout panel = new();
            panel.text = "Incompatible Scenes";
            panel.AddToClassList("header1");
            if (m_Styles != null)
                panel.styleSheets.Add(m_Styles);

            // Create a two-pane view with the left pane being fixed.
            var splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
            panel.Add(splitView);

            VisualElement leftPanel = new VisualElement();
            leftPanel.name = "Incompatible Scenes Left Pane";
            m_IncompatibleScenesRightPane = new VisualElement();
            m_IncompatibleScenesRightPane.name = "Incompatible Scenes Right Pane";
            splitView.Add(leftPanel);
            splitView.Add(m_IncompatibleScenesRightPane);

            m_IncompatibleScenesListView = new ListView();
            leftPanel.Add(m_IncompatibleScenesListView);

            PopulateSceneList(m_IncompatibleScenesListView, m_UncompatibleScenes);
            m_IncompatibleScenesListView.selectedIndex = m_IncompatibleListSelectedIndex;
            m_IncompatibleScenesListView.selectionChanged += (items) =>
            {
                OnSceneSelectionChanged(items, m_IncompatibleScenesRightPane);
                m_IncompatibleListSelectedIndex = m_IncompatibleScenesListView.selectedIndex;
                
            };

            return panel;
        }
        private void PopulateSceneList(ListView listElement, List<SceneInclusionData> items)
        {
            if( m_Settings == null)
            {
                return;
            }

            if(listElement.childCount != 0)
            {
                listElement.Clear();
            }

            // Initialize the list view with all sprites' names.
            listElement.makeItem = () => new Label();
            listElement.bindItem = (item, index) => { (item as Label).text = items[index].Name; };
            listElement.itemsSource = items;
        }

        private void OnSceneSelectionChanged(IEnumerable<object> selectedItems, VisualElement detailsPanel)
        {
            // Clear all previous content from the pane.
            detailsPanel.Clear();

            var enumerator = selectedItems.GetEnumerator();
            if (enumerator.MoveNext())
            {
                var selectedScene = enumerator.Current as SceneInclusionData;
                if (selectedScene != null)
                {
                    DisplaySceneSettings(selectedScene, detailsPanel);
                }
            }
        }

        // does not include a null check because intended use is after a null check
        private void DisplaySceneSettings(SceneInclusionData sceneData, VisualElement detailPanel)
        {

            VisualElement settingsPane = new VisualElement();
            settingsPane.name = "Settings Panel";
            if (m_Styles != null)
            {
                settingsPane.styleSheets.Add(m_Styles);
            }
            //settingsPane.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/UI Toolkit/Styles/Editor/NGAMESettingsWindow.uss"));

            // toggle inclusion in graph tool
            if (m_CompatibleScenes.Contains(sceneData))
            {
                Toggle toggleIncludeInGraph = new Toggle();
                toggleIncludeInGraph.name = "bIncludeInGraph";
                toggleIncludeInGraph.label = "Include Scene in Graph Tool";
                toggleIncludeInGraph.value = sceneData.IncludeInGraphTool;
                settingsPane.Add(toggleIncludeInGraph);

                toggleIncludeInGraph.RegisterValueChangedCallback(evt =>
                {
                    sceneData.IncludeInGraphTool = evt.newValue;
                    string guid = sceneData.Guid;
                    //Debug.Log(sceneData.Name + ", has had its bool to include in graph set to: " + sceneData.IncludeInGraphTool.ToString());

                    if (sceneData.IncludeInGraphTool && !m_Settings.Guids.Contains(guid))
                    {
                        m_Settings.Guids.Add(guid);
                        m_Settings.Scenes.Add(sceneData);
                    }
                    else if (!sceneData.IncludeInGraphTool && m_Settings.Guids.Contains(guid))
                    {
                        int index = m_Settings.Guids.IndexOf(guid);
                        m_Settings.Guids.Remove(guid);
                        m_Settings.Scenes.RemoveAt(index);
                    }

                    EditorUtility.SetDirty(m_Settings);
                    //Debug.Log("EditorUtility.SetDirty(m_Settings), for toggling inclusion in graph tool for sceneData: " + sceneData.Name);
                    //AssetDatabase.SaveAssets();
                });
            }
            // add description of found elements.
            TextElement descriptionElement = new TextElement();
            descriptionElement.name = "Description Element";
            if (m_GuidToDescriptionInSettings.ContainsKey(sceneData.Guid))
                descriptionElement.text = m_GuidToDescriptionInSettings[sceneData.Guid];
            else
                descriptionElement.text = "description not found";
                //descriptionElement.visible = sceneData.IncludeInGraphTool;
            settingsPane.Add(descriptionElement);

            detailPanel.Add(settingsPane);
        }


        
        private void /* List<SceneData>*/ InitializeSceneDataLookups()
        {
            //List<SceneData> results = new List<SceneData>();
            string[] allObjectGuids = AssetDatabase.FindAssets("t:Scene");
            

            foreach (string guid in allObjectGuids)
            {
                string filePath = AssetDatabase.GUIDToAssetPath(guid);

                Scene aScene = EditorSceneManager.OpenPreviewScene(filePath);
                if (!aScene.IsValid())
                {
                    //Debug.Log("Invalid scene found");

                    if(m_GuidToSceneData.ContainsKey(guid))
                    {
                        m_GuidToSceneData[guid].Description = "Scene Not Found";
                        if (!m_SavedScenesNotInProject.Contains(guid))
                            m_SavedScenesNotInProject.Add(guid);
                    }
                    else
                    {
                        SceneInclusionData data = new();
                        data.Guid = guid;
                        data.FilePath = filePath;
                        m_UncompatibleScenes.Add(data);
                    }
                    EditorSceneManager.ClosePreviewScene(aScene);
                    continue;
                }


                SceneInclusionData currentSceneData = null;
                //results.Add(currentSceneData);
                if (!m_GuidToSceneData.ContainsKey(guid)) 
                {
                    currentSceneData = new SceneInclusionData();
                    currentSceneData.Name = aScene.name;
                    currentSceneData.Guid = guid;
                    currentSceneData.FilePath = filePath;
                    
                    m_GuidToSceneData.Add(guid, currentSceneData);
                    //Debug.Log("EditorUtility.SetDirty(m_Settings), for sceneData: " + currentSceneData.Name);
                }
                else
                {
                    currentSceneData = m_GuidToSceneData[guid];
                    m_GuidToSceneData[guid].Name = aScene.name;
                    m_GuidToSceneData[guid].FilePath = filePath;
                }

                    string editorDescription = "";

                if (EvaluateSceneForGraphUse(aScene, out editorDescription))
                {
                    
                    m_CompatibleScenes.Add(currentSceneData);
                }
                else
                {
                    m_UncompatibleScenes.Add(currentSceneData);
                }

                if (m_GuidToDescriptionInSettings.ContainsKey(guid))
                    m_GuidToDescriptionInSettings[guid] = editorDescription;
                else
                    m_GuidToDescriptionInSettings.Add(guid, editorDescription);
                EditorSceneManager.ClosePreviewScene(aScene);
            }

            EditorUtility.SetDirty(m_Settings);
        }

        private bool EvaluateSceneForGraphUse(Scene aScene, out string editorDescription)
        {
            if (!aScene.IsValid())
            {
                //Debug.Log("Invalid scene found");
                EditorSceneManager.ClosePreviewScene(aScene);
                editorDescription = "Invalid scene found";
                return false;
            }
            StringBuilder description = new StringBuilder();
            bool bComponentsFound = false;

            GameObject[] rootObjects = aScene.GetRootGameObjects();

            foreach (GameObject obj in rootObjects)
            {
                IEncounterRegionConnector[] components = obj.GetComponentsInChildren<IEncounterRegionConnector>();

                if (components.Length > 0)
                {
                    bComponentsFound = true;
                    foreach (IEncounterRegionConnector component in components)
                    {
                        RegionConnectionData data = component.GetRegionConnectionData();
                        description.Append($"Found: {data.Name}\n");
                        description.Append($"IEncounterRegionConnector type: {component.GetType().Name}\n");
                        description.Append($"Connection Type: {data.ConnectionType.ToString() }\n");
                        //description.Append("Is Lockable: " + data.IsLockable.ToString() + "\n");

                        description.Append($"Position: { data.Position.ToString() }\n");
                        description.Append("-------------\n");
                    }
                }
            }

            if (bComponentsFound)
            {
                //Debug.Log("Scene: " + aScene.name + " contains target data types \n" + description.ToString());
                //EditorSceneManager.ClosePreviewScene(aScene);
                editorDescription = description.ToString();
                return true;
            }
            else
            {
                //EditorSceneManager.ClosePreviewScene(aScene);
                editorDescription = "No IEncounterRegionConnector components found in scene.";
                return false;
            }
        }
    }

}