using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Linq;

namespace NGAME.Editor
{
    public class EditorExtensions : EditorWindow
    {

        public static void DrawProperties(SerializedProperty property, bool drawChildren)
        {
            string lastPropPath = string.Empty;

            foreach (SerializedProperty p in property)
            {
                if(!p.isArray || p.propertyType != SerializedPropertyType.Generic)
                {
                    if (!string.IsNullOrEmpty(lastPropPath) && p.propertyPath.Contains(lastPropPath))
                    {
                        continue;
                    }
                    lastPropPath = p.propertyPath;
                    EditorGUILayout.PropertyField(p, drawChildren);
                    continue; //early skip to next item in foreach
                }

                // draw fold out for array property
                EditorGUILayout.BeginHorizontal();
                p.isExpanded = EditorGUILayout.Foldout(p.isExpanded, p.displayName);
                EditorGUILayout.EndHorizontal();

                if (p.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    // recursively draw nested children's properties in list
                    DrawProperties(p, drawChildren);
                    EditorGUI.indentLevel--;
                }
                
            }
        }

        public static VisualElement CreateNestedProperties(SerializedProperty property, bool drawChildren)
        {
            VisualElement result = new();
            string lastPropPath = string.Empty;
            List<Foldout> arrayElements = new();
            int currentArray = -1;
            do
            {
                if (!property.isArray || property.propertyType != SerializedPropertyType.Generic)
                {
                    if (!string.IsNullOrEmpty(lastPropPath) && property.propertyPath.Contains(lastPropPath))
                    {
                        continue;
                    }
                    lastPropPath = property.propertyPath;
                    PropertyField propertyElement = new PropertyField(property);
                    propertyElement.Bind(property.serializedObject);

                    result.Add(propertyElement);
                    continue; //early skip to next item in foreach
                }
                if (property.isArray)
                {
                    if (!string.IsNullOrEmpty(lastPropPath) && property.propertyPath.Contains(lastPropPath))
                    {
                        continue;
                    }
                    lastPropPath = property.propertyPath;

                    // draw fold out for array property
                    Foldout arrayFold = new();
                    currentArray++;
                    arrayFold.name = property.name;

                    result.Add(arrayFold);
                    continue;
                }

                PropertyField child = new PropertyField(property);
                child.Bind(property.serializedObject);
                //VisualElement child = CreateNestedProperties(property, drawChildren);
                arrayElements.ElementAt(currentArray).Add(child);
            }
            while (property.Next(drawChildren));

            return result;
        }
    }
}