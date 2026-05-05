using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace NGAME.Editor
{
    [CustomEditor(typeof(SceneData))]
    public class SceneDataInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement inspector = new();
            serializedObject.Update();

            Foldout fold = new();
            fold.name = "FullSceneData";
            fold.text = "Scene: " + serializedObject.FindProperty("m_Name").stringValue;

            //TextElement header = new TextElement();
            //header.Bind(sceneData);
            //header.bindingPath = sceneData.FindProperty("Name").propertyPath;
            //fold.Add(header);


            TextField description = new TextField("Description", 200, true, false, char.MinValue);
            description.Bind(serializedObject);
            description.bindingPath = serializedObject.FindProperty("Description").propertyPath;
            description.style.whiteSpace = WhiteSpace.Normal;
            description.AddToClassList(".wrap");
            description.style.height = 50;
            fold.Add(description);


            // alternate array display
            SerializedProperty connections = serializedObject.FindProperty("UniqueConnectionObjects");
            Foldout header = new();
            header.text = connections.displayName;
            fold.Add(header);

            for (int i = 0; i < connections.arraySize; i++)
            {
                PropertyField connection = new PropertyField(connections.GetArrayElementAtIndex(i));
                connection.Bind(serializedObject);
                connection.SetEnabled(false);
                header.Add(connection);
            }

            serializedObject.ApplyModifiedProperties();

            inspector.Add(fold);
            return inspector;
        }
    }
}
