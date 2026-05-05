using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace NGAME.Editor
{
    [CustomPropertyDrawer(typeof(SOWaveData))]
    public class SOWaveDataPropertyDrawer : PropertyDrawer
    {

        //public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        //{
        //    //base.OnGUI(position, property, label);
        //    if(property.objectReferenceValue == null)
        //    {
        //        //Debug.Log("skipping property drawer fro null SOWaveData");
        //        return;
        //    }

        //    SOWaveData wave = property.objectReferenceValue as SOWaveData;
        //    if (wave == null)
        //    {
        //        //Debug.Log("object refrence was not null, but was not convertable to SOWaveData, skipping property drawer");
        //        return;
        //    }

        //    var editor = UnityEditor.Editor.CreateEditor(wave);
        //    IMGUIContainer container = new IMGUIContainer(() => { editor.OnInspectorGUI(); });


        //}


        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {

            var popup = new UnityEngine.UIElements.PopupWindow();
            popup.text = "Wave Data";
            if(property.objectReferenceValue == null)
            {
                VisualElement row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;

                PropertyField objectInput = new PropertyField(property, "Load Scriptable Object");
                objectInput.Bind(property.serializedObject);
                objectInput.name = "WaveDataObjectField";
                objectInput.RegisterValueChangeCallback(
                evt => OnPropertyChanged(evt, popup));


                Button newObjectButton = new Button();
                newObjectButton.text = "New";
                newObjectButton.clicked += () => { OnNewButtonClicked(property); };

                row.Add(objectInput);
                row.Add(newObjectButton);

                popup.Add(row);
                return popup;
            }
            


            //SOWaveData data = ScriptableObject.CreateInstance<SOWaveData>(property.objectReferenceValue);
            var editor = UnityEditor.Editor.CreateEditor(property.objectReferenceValue);
            //editor.OnInspectorGUI();
            IMGUIContainer container = new IMGUIContainer(() => { editor.OnInspectorGUI(); });

            popup.Add(container);
            return popup;
        }

        private void OnNewButtonClicked(SerializedProperty waveDataProperty)
        {
            SOWaveData newData = ScriptableObject.CreateInstance<SOWaveData>();
            string path = EditorUtility.SaveFilePanelInProject("New WaveData Asset", "SOWD_NewData", "asset",
            "Please enter a file name");
            if (path.Length != 0)
            {
                AssetDatabase.CreateAsset(newData, path);
                var newAsset = AssetDatabase.LoadAssetAtPath(path, typeof(SOWaveData));
                waveDataProperty.objectReferenceValue = newAsset;
               
            }

            waveDataProperty.serializedObject.ApplyModifiedProperties();
            waveDataProperty.serializedObject.UpdateIfRequiredOrScript();
        }

        private void OnPropertyChanged(SerializedPropertyChangeEvent evt, VisualElement container)
        {
            //SerializedProperty wavesList = evt.changedProperty;
            evt.changedProperty.serializedObject.ApplyModifiedProperties();
            evt.changedProperty.serializedObject.UpdateIfRequiredOrScript();
        }

    }
}
