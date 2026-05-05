using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
namespace NGAME.Editor
{
    
    public class NodeInspectorView : GraphElement
    {
        private UnityEditor.Editor _editor;
        private NodeView m_CachedNode = null;

        private Label m_TitleLabel;

        private Label m_SubTitleLabel;

        private ScrollView m_ScrollView;

        private VisualElement m_HeaderItem;

        private Dragger m_Dragger;

        private GraphView m_GraphView;

        private bool m_Anchored;

        public NodeInspectorView(GraphView associatedGraphView = null) : base()
        {
            m_HeaderItem = new VisualElement();
            m_HeaderItem.name = "header";
            m_HeaderItem.AddToClassList("blackboardHeader");

            m_TitleLabel = new Label();
            m_TitleLabel.name = "titleLabel";
            m_TitleLabel.text = "Inspector";
            m_SubTitleLabel = new Label();
            m_SubTitleLabel.name = "subTitleLabel";
            m_SubTitleLabel.text = "Node Details";
            m_HeaderItem.Add(m_TitleLabel);
            m_HeaderItem.Add(m_SubTitleLabel);

            m_ScrollView = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            m_ScrollView.name = "MainContentScroller";

            Add(m_HeaderItem);
            Add(m_ScrollView);

            base.capabilities |= Capabilities.Resizable | Capabilities.Movable;
            base.style.overflow = Overflow.Hidden;
            ClearClassList();
            AddToClassList("blackboard");

            m_Dragger = new Dragger()
            {
                clampToParentEdges = true
            };

            this.AddManipulator(m_Dragger);

            Add(new Resizer());

            //RegisterCallback(delegate (DragUpdatedEvent e)
            //{
            //    e.StopPropagation();
            //});

            RegisterCallback(delegate (WheelEvent e)
            {
                e.StopPropagation();
            });
            //RegisterCallback<MouseDownEvent>(EatMouseDown);

            //this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));

            m_GraphView = associatedGraphView;
            focusable = true;
            
        }

        //
        // Summary:
        //     The GraphView that the Inspector is attached to. Based on GraphView.Blackboard
        public GraphView Graph
        {
            get
            {
                if (m_GraphView == null)
                {
                    m_GraphView = GetFirstAncestorOfType<GraphView>();
                }

                return m_GraphView;
            }
            set
            {
                m_GraphView = value;
            }
        }
       
        //
        // Summary:
        //     The title of this window.
        public override string title
        {
            get
            {
                return m_TitleLabel.text;
            }
            set
            {
                m_TitleLabel.text = value;
            }
        }

        //
        // Summary:
        //     The subtitle of this window.
        public string subTitle
        {
            get
            {
                return m_SubTitleLabel.text;
            }
            set
            {
                m_SubTitleLabel.text = value;
            }
        }


        // CUSTOM BEHAVIOR
        public void UpdateSelection(NodeView nodeView)
        {
            if(m_CachedNode != null)
            {
                m_ScrollView.Clear();
                Object.DestroyImmediate(_editor);
            }
            
            m_CachedNode = nodeView;

            if (nodeView == null)
            {
                return;
            }

             _editor = UnityEditor.Editor.CreateEditor(m_CachedNode.Node);
            
            var container = _editor.CreateInspectorGUI();
            CreateSceneDataEditor(container);
            m_ScrollView.Add(container);

        }

        public void Repaint(NodeView nodeView)
        {
            m_CachedNode =  m_CachedNode == null ? nodeView : m_CachedNode;
            EditorApplication.delayCall += DelayedRepaint;
        }

        private void DelayedRepaint()
        {
            EditorApplication.delayCall -= DelayedRepaint;
            if (m_CachedNode == null)
            {
                return;
            }
            UpdateSelection(m_CachedNode);

        }

        private VisualElement CreateSceneDataEditor(VisualElement roomEditorGui)
        {
            
            if(m_CachedNode == null || roomEditorGui == null)    
                return roomEditorGui;

            SceneData data = m_CachedNode.CurrentSceneData;
            if (data == null)
                return roomEditorGui;

            var editor = UnityEditor.Editor.CreateEditor(m_CachedNode.CurrentSceneData);

            //SerializedObject sceneData = new SerializedObject(m_CachedNode.CurrentSceneData);

            VisualElement sceneDataGui = editor.CreateInspectorGUI();

            Label displayAfterThis = roomEditorGui.Q<Label>("Title");
            if (displayAfterThis != null)
                roomEditorGui.Insert(roomEditorGui.IndexOf(displayAfterThis) + 1, sceneDataGui);
            else
                roomEditorGui.Add(sceneDataGui);

            return roomEditorGui;
        }

    }
}