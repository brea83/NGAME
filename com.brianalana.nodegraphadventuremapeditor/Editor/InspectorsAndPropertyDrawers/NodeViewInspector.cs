using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace NGAME.Editor
{
    [CustomEditor(typeof(NodeView))]
    public class NodeViewInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI() 
        { 
            serializedObject.Update();

            VisualElement inspector = new();

            Label title = new();
            title.Bind(serializedObject);
            title.bindingPath = serializedObject.FindProperty("title").propertyPath;
            
            inspector.Add(title);
            return inspector;
        }
    }


}