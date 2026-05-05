using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NGAME.Editor
{

    [CustomPropertyDrawer(typeof(EntranceCondition))]
    public class EntranceCondition_Drawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            Label result = new(property.FindPropertyRelative("Name").stringValue);
            result.tooltip = property.FindPropertyRelative("Description").stringValue;
            return result;
        }
    }
    
}
