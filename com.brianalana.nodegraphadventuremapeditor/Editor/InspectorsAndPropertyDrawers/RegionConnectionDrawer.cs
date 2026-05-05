using System.Collections.Generic;
using System;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace NGAME.Editor
{
    [CustomPropertyDrawer(typeof(RegionConnectionData))]
    public class RegionConnectionDrawer : PropertyDrawer
    {
        //private ListView m_ListView;
        //private DropdownField m_EntranceConditionDropdown;

        //private SerializedObject serializedObject;
        //private SerializedProperty m_ArraySizeProperty;
        //private SerializedProperty m_ArrayProperty;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            //serializedObject = property.serializedObject;

            UnityEngine.UIElements.PopupWindow popup = new();
            popup.text = property.FindPropertyRelative("Name").stringValue;

            PropertyField type = new(property.FindPropertyRelative("TypeName"));
            type.Bind(property.serializedObject);
            type.SetEnabled(false);
            popup.Add(type);

            //PropertyField position = new(property.FindPropertyRelative("Position"));
            //position.Bind(property.serializedObject);
            //position.SetEnabled(false);
            //popup.Add(position);

            PropertyField connectionType = new(property.FindPropertyRelative("ConnectionType"));
            connectionType.Bind(property.serializedObject);
            popup.Add(connectionType);

            SerializedProperty entryconditionsproperty = property.FindPropertyRelative("EntranceConditions");
           
            if (entryconditionsproperty != null)
            {
                VisualElement entryConditionsEditor = CreateEntryConditionEditor(entryconditionsproperty);
                popup.Add(entryConditionsEditor);
            }
            //else
            //{
            //    m_ArrayProperty = null;
            //    m_ArraySizeProperty = null;
            //}

                return popup;
        }

        private VisualElement CreateEntryConditionEditor(SerializedProperty conditionsList)
        {
            //m_ArrayProperty = conditionsList;
            //m_ArraySizeProperty = serializedObject.FindProperty(conditionsList.propertyPath + ".Array.size");
            VisualElement result = new();
            result.style.flexDirection = FlexDirection.Column;
            Label label = new("Entrance Conditions");
            result.Add(label);

            ListView listElement = CreateListView(conditionsList);
            result.Add(listElement);

            result.Add(CreateConditionSelector(conditionsList));

            return result;
        }
        private ListView CreateListView(SerializedProperty conditionsList)
        {
            ListView m_ListView = new ListView();
            m_ListView.showBoundCollectionSize = false;
            m_ListView.name = "List-EntranceConditions";
            m_ListView.bindingPath = conditionsList.propertyPath;
            m_ListView.style.flexGrow = 1;

            m_ListView.makeItem = () => CreateConditionItem(conditionsList);
            m_ListView.bindItem = (item, index) => { BindConditionItem(conditionsList, item, index); };

            m_ListView.Bind(conditionsList.serializedObject);
            return m_ListView;
        }

        private void AddCondition(SerializedProperty conditionsListProperty, List<Type> types, DropdownField typeSelector)
        {
            SerializedProperty sizeProperty = conditionsListProperty.serializedObject.FindProperty(conditionsListProperty.propertyPath + ".Array.size");
            if (sizeProperty == null)
                return;

            int index = typeSelector.index;
            if (types.Count <= index)
                return;

            Type selectedType = types[index];

            EntranceCondition newCondition = (EntranceCondition)Activator.CreateInstance(selectedType, new object[] { });

            if (newCondition == null)
                return;

            // check if type already in conditions list
            for(int i = 0; i < sizeProperty.intValue; i++)
            {
                EntranceCondition condition = conditionsListProperty.GetArrayElementAtIndex(i).managedReferenceValue as EntranceCondition;
                if(condition != null && condition.Name == newCondition.Name)
                {
                    return;
                }
            }

            int insertionIndex = sizeProperty.intValue;
            sizeProperty.intValue++;

            sizeProperty.serializedObject.ApplyModifiedProperties();

            SerializedProperty newElementProperty = conditionsListProperty.GetArrayElementAtIndex(insertionIndex);
            newElementProperty.managedReferenceValue = newCondition;
            sizeProperty.serializedObject.ApplyModifiedProperties();
        }


        private VisualElement CreateConditionItem(SerializedProperty conditionsList)
        {
            var row = new VisualElement(); //BindableElement so the default bind can assign the item's root property
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;

            row.Add(new PropertyField() { name = "ConditionPropertyDrawer"}); // default bind need this to be the first Bindable in the tree

            Button removeItemButton = new() { text = "X" };
            removeItemButton.name = "Button-RemoveItem";
            removeItemButton.clicked += () =>
            {
                if (removeItemButton.userData is int index)
                {
                    conditionsList.DeleteArrayElementAtIndex(index);
                    conditionsList.serializedObject.ApplyModifiedProperties();
                }

            };
            removeItemButton.tooltip = "Remove this item from the list";

            row.Add(removeItemButton);
            return row;
        }

        private void BindConditionItem(SerializedProperty conditionsList, VisualElement element, int index)
        {
            var label = element.Q<Label>("ConditionLabel");

            var button = element.Q<Button>();
            if (button != null)
                button.userData = index;

            //we find the first Bindable
            var field = element as IBindable;
            if (field == null)
            {
                //we dig through children
                field = element.Query().Where(x => x is IBindable).First() as IBindable;
            }

            // Bound ListView.itemsSource is a IList of SerializedProperty
            var itemProp = conditionsList.GetArrayElementAtIndex(index);

            field.bindingPath = itemProp.propertyPath;

            element.Bind(conditionsList.serializedObject);
        }

        private void AddRemoveItemButton(VisualElement row, SerializedProperty conditionsList)
        {
            var button = new Button() { text = "-" };
            //button.RegisterCallback<ClickEvent>((evt) =>
            //{
            //    var clickedElement = evt.target as VisualElement;

            //    if (clickedElement != null && clickedElement.userData is int index)
            //    {
            //        conditionsList.DeleteArrayElementAtIndex(index);
            //        conditionsList.serializedObject.ApplyModifiedProperties();
            //    }
            //});

            
            
            row.Add(button);
        }

        private VisualElement CreateConditionSelector(SerializedProperty conditionsList)
        {
            VisualElement result = new();
            result.style.flexDirection = FlexDirection.Row;
            Type parent = null;

            var assemblies = UserCreatedAssemblies.GetWhereNgameRefrenced(AppDomain.CurrentDomain).ToList();
            foreach (Assembly assembly in assemblies)
            {
                Type[] types = assembly.GetTypes();
                List<Type> targetTypes = types.Where(t => t.Name == conditionsList.type).ToList();
                if (targetTypes.Count <= 0)
                {
                    continue;
                }
                parent = targetTypes.First();
                break;

            }

            if (parent != null)
            {
                List<Type> childTypes = new();
                foreach (Assembly assembly in assemblies)
                {
                    Type[] types = assembly.GetTypes();
                    if (types.Length <= 0)
                    {
                        continue;
                    }
                    List<Type> inheritingTypes = types.Where(t => parent.IsAssignableFrom(t) && t.Name != conditionsList.type).ToList();
                    if (inheritingTypes.Count <= 0)
                        continue;

                    childTypes.AddRange(inheritingTypes);
                }

                if(childTypes.Count > 0)
                {
                    DropdownField connectionTypeSelector = new DropdownField(childTypes.ConvertAll(t => t.Name), 0);
                    result.Add(connectionTypeSelector);

                    Button addItemButton = new();
                    addItemButton.text = "+";
                    addItemButton.clicked += () =>
                    {
                        AddCondition(conditionsList, childTypes, connectionTypeSelector);
                    };
                    result.Add(addItemButton);
                }
            }

            return result;
        }

        private void OnConnectionSelectorChanged(string oldType, string newType)
        {

        }
        private void OnListItemCallback(string oldType, string newType)
        {

        }
    }
}
