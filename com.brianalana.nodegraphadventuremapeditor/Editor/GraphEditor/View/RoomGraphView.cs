using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace NGAME.Editor
{
    public class UndoableGraphChanges : ScriptableObject
    {
        public List<RoomNode> UnsavedNodes {get => m_UnsavedNodes;}
        public List<RoomNode> NodesToDelete { get => m_NodesToDelete; }

        public Dictionary<SceneData, int> SceneDataRefs
        {
            get
            {
                Dictionary<SceneData, int> result = new();
                foreach(SceneData data in m_SceneData)
                {
                    result.Add(data, m_SceneDataRefCount[m_SceneData.IndexOf(data)]);
                }
                return result;
            }
        }

        private List<RoomNode> m_UnsavedNodes = new();
        private List<RoomNode> m_NodesToDelete = new();

        private List<SceneData> m_SceneData = new();
        private List<int> m_SceneDataRefCount = new();
        public static  UndoableGraphChanges CreateNew() 
        {
            return ScriptableObject.CreateInstance<UndoableGraphChanges>();
        }

        public void AddSceneDataRef(SceneData data)
        {
            Undo.RegisterCompleteObjectUndo(this, $"A Node is adding a refrence to {data.name}");
            if (m_SceneData.Contains(data))
            {
                int index = m_SceneData.IndexOf(data);
                m_SceneDataRefCount[index]++;
            }
            else
            {
                m_SceneData.Add(data);
                m_SceneDataRefCount.Add(1);
            }
        }

        public void RemoveSceneDataRef(SceneData data)
        {
            Undo.RegisterCompleteObjectUndo(this, $"A Node is removing its refrence to {data.name}");
            if (m_SceneData.Contains(data))
            {
                int index = m_SceneData.IndexOf(data);
                m_SceneDataRefCount[index]--;
            }
            else
            {
                m_SceneData.Add(data);
                m_SceneDataRefCount.Add(-1);
            }
        }

        public void AddNode(RoomNode node)
        {
            if (!m_UnsavedNodes.Contains(node))
            {
                Undo.RegisterCompleteObjectUndo(this, "Add node to unsaved nodes");
                m_UnsavedNodes.Remove(node);
            }

            if (m_NodesToDelete.Contains(node))
            {
                Undo.RegisterCompleteObjectUndo(this, "remove node from nodes to delete");
                m_NodesToDelete.Add(node);
            }
        }
        public void RemoveNode(RoomNode node)
        {
            if (m_UnsavedNodes.Contains(node))
            {
                Undo.RegisterCompleteObjectUndo(this, "Remove node from unsaved nodes");
                m_UnsavedNodes.Remove(node);
            }

            if(!m_NodesToDelete.Contains(node))
            {
                Undo.RegisterCompleteObjectUndo(this, "add node to nodes to delete");
                m_NodesToDelete.Add(node);
            }
        }

        public void Reset()
        {
            m_UnsavedNodes.Clear();
            m_NodesToDelete.Clear();
            m_SceneData.Clear();
            m_SceneDataRefCount.Clear();
        }
    }

    [UxmlElement]
    public partial class RoomGraphView : GraphView
    {
        public delegate List<SceneData> SceneDataRequestCallback();
        public SceneDataRequestCallback SceneDataRequested;

        public delegate Dictionary<string, Texture2D> ScenePreviewsRequestCallback();
        public ScenePreviewsRequestCallback ScenePreviewsRequested;

        public Action<NodeView> OnNodeSelected;
        public Action<NodeView> OnNodeValuesChanged;
        public Action<SceneAsset, EdgeData> RegisterPlayModeRequest;
        public Action OnGraphChanged;

        public Action ContextMenuNewGraphRequest;
        public Action ContextMenuLoadGraphRequest;

        public List<SceneInclusionData> IncludedScenes = new List<SceneInclusionData>();
        public List<SceneConnectionsData> ValidScenes = new List<SceneConnectionsData>();
        public List<SceneSpawnData> SpawnersByScene = new List<SceneSpawnData>();
        public Dictionary<string, SceneData> SceneLookup = new();
        public Dictionary<string, Texture2D> ScenePreviewLookup = new();
        //public StyleSheet Style;

        private UndoableGraphChanges m_UndoableGraphChanges;
        
        
        private RoomGraph _graph;
        public bool HasRoomGraph { get => _graph != null; }

        private MiniMap m_MiniMap;

        private NodeInspectorView m_Inspector;

        //public override bool supportsWindowedBlackboard => true;
        public RoomGraphView() : base()
        {
            Insert(0, new GridBackground());

            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            m_UndoableGraphChanges = UndoableGraphChanges.CreateNew();
            m_UndoableGraphChanges.hideFlags = HideFlags.DontUnloadUnusedAsset;

            //var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/UI Toolkit/Styles/Editor/RoomGraphEditor.uss");
            //styleSheets.Add(styleSheet);
            Undo.undoRedoPerformed += OnUndoRedo;
        }
        
        //internal public void OnWindowDestroy()
        //{

        //}
        

        protected void OnUndoRedo()
        {
            PopulateView(_graph);
            if (OnGraphChanged != null)
                OnGraphChanged.Invoke();
        }

        public override bool supportsWindowedBlackboard => true;

        public MiniMap GetOrCreateMiniMap()
        {
            if(m_MiniMap == null)
            {
                m_MiniMap = new();
                m_MiniMap.graphView = this;
                m_MiniMap.AddToClassList("Minimap");
                //miniMap.anchored = true;

                Add(m_MiniMap);
            }

            return m_MiniMap;
        }

        public NodeInspectorView GetOrCreateNodeInspector()
        {
            if (m_Inspector == null)
            {
                m_Inspector = new NodeInspectorView(this);
                Add(m_Inspector);
            }
            return m_Inspector;
        }

        public void ReleaseNodeInspector(NodeInspectorView inspector)
        {
            if (m_Inspector == inspector)
            {
                m_Inspector = null;
            }
        }

        public void UpdateSceneDataRefrenceCounts(string oldSceneGuid, SceneData newData)
        {
            if (!string.IsNullOrEmpty(oldSceneGuid))
            {
                SceneData oldData;
                SceneLookup.TryGetValue(oldSceneGuid, out oldData);
                if (oldData != null)
                    m_UndoableGraphChanges.RemoveSceneDataRef(oldData);
            }

            if (newData == null)
                return;

            if(m_UndoableGraphChanges == null)
            {
                m_UndoableGraphChanges = UndoableGraphChanges.CreateNew();
                m_UndoableGraphChanges.hideFlags = HideFlags.DontUnloadUnusedAsset;
            }
            m_UndoableGraphChanges.AddSceneDataRef(newData);
        }

        internal void PopulateView(RoomGraph roomGraph)
        {
            this._graph = roomGraph;

            graphViewChanged -= OnGraphViewChanged;
            DeleteElements(graphElements);
            graphViewChanged += OnGraphViewChanged;

            GetOrCreateMiniMap();
            if (_graph == null)
                return;

            foreach (RoomNode node in _graph.nodes)
            {
                if(node is RoomNode)
                {
                    CreateNodeView(node);
                }
            }

            EditorApplication.delayCall += OnDelayValidateGraph;
        }

        private void OnDelayValidateGraph()
        {
            EditorApplication.delayCall -= OnDelayValidateGraph;
            foreach (RoomNode node in _graph.nodes)
            {
                NodeView currentView = FindNodeView(node);

                currentView.ValidateNode(ValidScenes);
            }
        }

        public void SaveGraph()
        {
            if (_graph == null)
                return;
            
            Dictionary<SceneData, int> sceneDataRefs = m_UndoableGraphChanges.SceneDataRefs;
            foreach(RoomNode node in _graph.nodes)
            {
                string path = AssetDatabase.GetAssetPath(node);
                if (!path.Contains(_graph.name + ".asset"))
                    AssetDatabase.AddObjectToAsset(node, _graph);

                if(node.SceneData != null)
                {
                    if (sceneDataRefs.ContainsKey(node.SceneData))
                        sceneDataRefs[node.SceneData]++;
                    else
                        sceneDataRefs.Add(node.SceneData, 1);
                }

                EditorUtility.SetDirty(_graph);
                EditorUtility.SetDirty(node);
            }

            foreach(RoomNode nodeToDelete in m_UndoableGraphChanges.NodesToDelete)
            {
                AssetDatabase.RemoveObjectFromAsset(nodeToDelete);
                EditorUtility.SetDirty(_graph);
                EditorUtility.SetDirty(nodeToDelete);
            }

            UnityEngine.Object[] graphAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(_graph));

            foreach(UnityEngine.Object asset in graphAssets)
            {
                if(asset is SceneData scene)
                {
                    bool bInRefCount = sceneDataRefs.ContainsKey(scene);
                    int refCount = bInRefCount ? sceneDataRefs[scene] : 0;
                    if (refCount  <= 0)
                    {
                        AssetDatabase.RemoveObjectFromAsset(scene);
                    }
                    else
                    {
                        sceneDataRefs.Remove(scene);
                    }
                }
            }
            foreach (SceneData key in sceneDataRefs.Keys)
            {
                if (sceneDataRefs[key] <= 0)
                {
                    // scene was removed from a node but never saved to the graph so we don't need to remove any assets
                    continue;
                }
                else
                {
                    // this is a workaround for needing to make the scenedata hideanddontsave so that the data persists across scene loads.
                    HideFlags oldFlags = HideFlags.None;
                    if (key.hideFlags != HideFlags.None)
                    {
                         oldFlags = key.hideFlags;
                    }
                    key.hideFlags = HideFlags.None;
                    AssetDatabase.AddObjectToAsset(key, _graph);
                    key.hideFlags = oldFlags;
                }
            }
            EditorUtility.SetDirty(_graph);

            m_UndoableGraphChanges.Reset();

            AssetDatabase.SaveAssetIfDirty(_graph);
        }

        public void DiscardChanges()
        {
            if(_graph == null) return;

            List<string> paths = new();

            paths.Add(AssetDatabase.GetAssetPath(_graph));
            AssetDatabase.ForceReserializeAssets(paths);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(_graph), ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();

            if (m_UndoableGraphChanges == null)
                m_UndoableGraphChanges = UndoableGraphChanges.CreateNew();
            else
                m_UndoableGraphChanges.Reset();

            m_UndoableGraphChanges.hideFlags = HideFlags.DontUnloadUnusedAsset;

            PopulateView(_graph);
        }

        private NodeView FindNodeView(IMapNode roomNode)
        {
            return GetNodeByGuid(roomNode.Guid) as NodeView;
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange viewChange) 
        {
            bool bSendChanges = false;
            if (viewChange.elementsToRemove != null)
            {
                foreach (GraphElement element in viewChange.elementsToRemove)
                {
                    NodeView nodeView =  element as NodeView;
                    if(nodeView != null)
                    {
                        Undo.IncrementCurrentGroup();

                        Undo.RegisterCompleteObjectUndo(new UnityEngine.Object[] { _graph, nodeView.Node }, "remove node from asset");
                        _graph.DeleteNode(nodeView.Node);
                        m_UndoableGraphChanges.RemoveNode(nodeView.Node);

                        //AssetDatabase.RemoveObjectFromAsset(nodeView.Node);
                        EditorUtility.SetDirty(nodeView.Node);
                        //AssetDatabase.SaveAssetIfDirty(nodeView.Node);
                        EditorUtility.SetDirty(_graph);
                        //AssetDatabase.SaveAssetIfDirty(_graph);

                        //Undo.DestroyObjectImmediate(nodeView.Node);
                        nodeView.Node = null;

                        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
                        Undo.SetCurrentGroupName("NGAME - Delete Node");
                        
                        bSendChanges = true;
                    }

                    Edge edge = element as Edge;
                    if(edge != null)
                    {
                        NodeView sourceNode = edge.output.node as NodeView;
                        NodeView destinationNode = edge.input.node as NodeView;
                        NodeView.RemoveEdge(edge);
                        bSendChanges = true;
                    }
                }
            }

            if(viewChange.edgesToCreate != null)
            {
                List<Edge> invalidEdges = new();
                foreach(Edge edge in viewChange.edgesToCreate)
                {
                    if(!edge.output.enabledSelf || !edge.input.enabledSelf)
                    {
                        invalidEdges.Add(edge);
                    }
                    else
                    {
                        NodeView sourceNode = edge.output.node as NodeView;
                        NodeView destinationNode = edge.input.node as NodeView;
                        NodeView.AddEdge(edge);
                        bSendChanges = true;
                        //Debug.Log("GRAPHVIEW: Edge created between " + edge.input.portName + ", and " + edge.output.portName);
                    }
                }

                foreach(Edge edge in invalidEdges)
                {
                    viewChange.edgesToCreate.Remove(edge);
                }
            }

            //if(viewChange.movedElements != null)
            //{
            //    foreach (GraphElement element in viewChange.movedElements)
            //    {
            //        Edge edge = element as Edge;
            //        if( edge != null )
            //        {
            //            Debug.Log("Edge between " + edge.input.portName + ", and " + edge.output.portName + ". MOVED");

            //        }
            //    }
            //}

            if (bSendChanges && OnGraphChanged != null)
                OnGraphChanged.Invoke();

            return viewChange;
        }
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if(_graph == null)
            {
                if (evt.target is GraphView)
                {
                    evt.menu.AppendAction("No Graph Loaded. Load a graph or Create a new one",
                        (a) => { }, DropdownMenuAction.Status.Disabled);

                    evt.menu.AppendSeparator();

                    evt.menu.AppendAction("Create Graph", (a) =>
                    {
                        if (ContextMenuNewGraphRequest != null)
                            ContextMenuNewGraphRequest.Invoke();
                    });

                    evt.menu.AppendAction("Load Graph", (a) =>
                    {
                        if (ContextMenuLoadGraphRequest != null)
                            ContextMenuLoadGraphRequest.Invoke();
                    });
                }
                return;
            }
            
                base.BuildContextualMenu(evt);

            
            if(evt.target is GraphView)
            {
                
                var types = TypeCache.GetTypesDerivedFrom<IMapNode>();
                Vector2 position = evt.mousePosition;
                foreach(var type in types)
                {
                    evt.menu.AppendAction($"Add [{type.BaseType.Name}] {type.Name}", 
                        (a) => OnContextMenuCreateNode(a, type),
                        (DropdownMenuAction a) => _graph != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                }
            }

        }
        protected void OnContextMenuCreateNode(DropdownMenuAction a, System.Type type)
        {
            
            Vector2 screenMousePosition = a.eventInfo.localMousePosition;
            CreateNewNode(type, screenMousePosition);
        }
        void CreateNewNode(System.Type type, Vector2 position)
        {
            Undo.IncrementCurrentGroup();

            string newGuid = GUID.Generate().ToString();
            RoomNode node = RoomNode.CreateNode(type, newGuid);

            node.Position = position;
            Undo.RegisterCreatedObjectUndo(node, "node created");

            Undo.RegisterCompleteObjectUndo(_graph, "NGAME - Add new Node to graph");
            _graph.AddNode(node);

            Undo.RecordObject(node, "rename node");
            node.name = _graph.name + ": Room " + _graph.nodes.Count.ToString();

            Undo.RegisterCompleteObjectUndo(new UnityEngine.Object[] { _graph, node }, "add node to asset");
            //AssetDatabase.AddObjectToAsset(node, _graph);

            EditorUtility.SetDirty(_graph);
            EditorUtility.SetDirty(node);
            //AssetDatabase.SaveAssetIfDirty(_graph);
            //AssetDatabase.SaveAssetIfDirty(node);

            
            CreateNodeView(node);
            Undo.SetCurrentGroupName("NGAME - Create New Node in graph");
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

            
            m_UndoableGraphChanges.AddNode(node);
            if (OnGraphChanged != null)
                OnGraphChanged.Invoke();
        }
        void CreateNodeView(RoomNode roomNode)
        {
            NodeView nodeView = new NodeView(this, roomNode, ValidScenes);
            nodeView.OnNodeSelected = OnNodeSelected;
            nodeView.OnNodeValuesChanged = OnNodeValuesChanged;
            nodeView.RequestPlayMode = OnPlayModeRequest;
            AddElement(nodeView);
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            //return base.GetCompatiblePorts(startPort, nodeAdapter);
            return ports.ToList().Where(endPort =>
            endPort.direction != startPort.direction &&
            endPort.node != startPort.node
            ).ToList();
        }

        internal void RefreshSceneData(List<SceneData> scenes, Dictionary<string, Texture2D> previews)
        {
            UpdateDataObjects(scenes, previews);
          
            PopulateView(_graph);
        }

        private void OnPlayModeRequest(EdgeData edge)
        {
            SceneData data = null;
            SceneLookup.TryGetValue(edge.DestinationSceneGuid, out data);

            if (data == null)
                return;

            string scenePath = data.FilePath;

            SceneAsset myWantedStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);

            if (RegisterPlayModeRequest != null)
                RegisterPlayModeRequest.Invoke(myWantedStartScene, edge);
        }

        internal void UpdateDataObjects(List<SceneData> scenes, Dictionary<string, Texture2D> previews)
        {
            SceneLookup.Clear();
            ValidScenes.Clear();
            SpawnersByScene.Clear();

            ScenePreviewLookup = previews == null ? new() : previews;

            if(scenes == null)
            {
                scenes = new List<SceneData>();
                return;
            }

            for (int i = 0; i < scenes.Count; i++)
            {
                SceneData sceneData = scenes[i];


                string sceneGuid = sceneData.Guid;

                SceneLookup.Add(sceneGuid, sceneData);
                SceneConnectionsData connections = new(sceneData);
                ValidScenes.Add(connections);
                SceneSpawnData spawnData = new(sceneData);
                SpawnersByScene.Add(spawnData);

            }
        }

        private void GetRoomDataObjects()
        {
            string[] settingsGuid = AssetDatabase.FindAssets("t:SO_Settings");

            if(settingsGuid.Length <= 0)
            {
                return;
            }

            SO_Settings settings = AssetDatabase.LoadAssetAtPath<SO_Settings>(AssetDatabase.GUIDToAssetPath(settingsGuid[0]));

            if (settings == null || settings.Scenes.Count <= 0)
            {
                return;
            }

            SceneLookup.Clear();
            ValidScenes.Clear();
            SpawnersByScene.Clear();
            ScenePreviewLookup.Clear();

            for (int i = 0; i < settings.Scenes.Count; i++)
            {
                SceneInclusionData data = settings.Scenes[i];

                if(data.FilePath == "" || !data.IncludeInGraphTool)
                {
                    continue;
                }

                // open the scene to collect data from it
                Scene aScene = EditorSceneManager.OpenPreviewScene(data.FilePath);
                if (!aScene.IsValid())
                {
                    Debug.Log("Graph tried to include an invalid scene from filepath: " + data.FilePath);
                    EditorSceneManager.ClosePreviewScene(aScene);
                    continue;
                }
                string sceneGuid = settings.Guids[i];

                SceneData sceneData = CreateSceneData(aScene, sceneGuid, data.FilePath);
                SceneLookup.Add(sceneGuid, sceneData);
                SceneConnectionsData connections = new(sceneData);
                ValidScenes.Add(connections);
                SceneSpawnData spawnData = new(sceneData);
                SpawnersByScene.Add(spawnData);

                Texture2D previewImage = ScenePreviewRenderer.WriteTexture(aScene, sceneData, 100);
                previewImage.filterMode = FilterMode.Point;
                if (ScenePreviewLookup.ContainsKey(sceneGuid))
                {
                    ScenePreviewLookup[sceneGuid] = previewImage;
                }
                else
                {
                    ScenePreviewLookup.Add(sceneGuid, previewImage);
                }

                EditorSceneManager.ClosePreviewScene(aScene);
            }

        }

        
        private SceneData CreateSceneData(Scene aScene, string sceneGuid, string filePath)
        {
            SceneData result = ScriptableObject.CreateInstance<SceneData>();
            result.Name = aScene.name;
            result.Guid = sceneGuid;
            result.FilePath = filePath;

            List<RegionConnectionData> conectionObjects = new();
            List<SpawnerData> spawners = new();

            //bool bConnectionsFound = false;
            //bool bSpawnersFound = false;

            GameObject[] rootObjects = aScene.GetRootGameObjects();

            foreach (GameObject obj in rootObjects)
            {
                // connection data
                IEncounterRegionConnector[] connectorComponent = obj.GetComponentsInChildren<IEncounterRegionConnector>();
                //if (connectorComponent.Length > 0) bConnectionsFound = true;

                foreach (IEncounterRegionConnector connection in connectorComponent)
                {
                    //connections.Add(component.GetRegionConnectionData());
                    RegionConnectionData data = connection.GetRegionConnectionData();
                    conectionObjects.Add(data);
                }

                // spawner data

                ISpawnPoint[] spawnerComponents = obj.GetComponentsInChildren<ISpawnPoint>();
                //if (spawnerComponents.Length > 0) bSpawnersFound = true;

                foreach (ISpawnPoint spawner in spawnerComponents)
                {
                    spawners.Add(spawner.GetSpawnerData());
                }

            }

            //if (!bConnectionsFound && !bSpawnersFound)
            //{
            //    Debug.Log("No IEncounterRegionConnector or ISpawnPoint components found in scene: " + aScene.name);
            //}
            //else
            //{
            //    Debug.Log("Scene: " + aScene.name + " contains target data types");
            //}

            result.UniqueConnectionObjects = conectionObjects;
            result.SpawnPoints = spawners;
            result.Bounds = new(conectionObjects, spawners);

            return result;
        }
    }
}
