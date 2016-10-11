/*
  Copyright 2014 Google Inc. All rights reserved.

  Licensed under the Apache License, Version 2.0 (the "License");
  you may not use this file except in compliance with the License.
  You may obtain a copy of the License at

      http://www.apache.org/licenses/LICENSE-2.0

  Unless required by applicable law or agreed to in writing, software
  distributed under the License is distributed on an "AS IS" BASIS,
  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  See the License for the specific language governing permissions and
  limitations under the License.
*/

#if UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2
#define PRE_UNITY_4_3
#endif

using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;


/*
  Custom Property Drawer to enable Tooltips for Inspector properties
*/

[CustomPropertyDrawer(typeof(TooltipAttribute))]
public class TooltipDrawer : PropertyDrawer
{

#if PRE_UNITY_4_3
  private GUIContent _newTooltipContent = null;
  private GUIContent previousTooltipContent = null;
  private Type fieldType = null;

  public override void OnGUI(Rect position, SerializedProperty property, GUIContent tooltipContent) {
    previousTooltipContent = tooltipContent;
    EditorGUI.BeginProperty(position, newTooltipContent, property);
    EditorGUI.BeginChangeCheck();
    switch(property.propertyType)
    {
      case SerializedPropertyType.Boolean:
        bool newBoolValue = EditorGUI.Toggle(position, newTooltipContent, property.boolValue);
        if(EditorGUI.EndChangeCheck()) property.boolValue = newBoolValue;
        break;
      case SerializedPropertyType.Enum:
        int newEnumValueIndex = (int)(object)EditorGUI.EnumPopup(position, newTooltipContent, Enum.Parse(GetFieldType(property), property.enumNames[property.enumValueIndex]) as Enum);
        if(EditorGUI.EndChangeCheck()) property.enumValueIndex = newEnumValueIndex;
        break;
      case SerializedPropertyType.Float:
        float newFloatValue = EditorGUI.FloatField(position, newTooltipContent, property.floatValue);
        if(EditorGUI.EndChangeCheck()) property.floatValue = newFloatValue;
        break;
      case SerializedPropertyType.Integer:
        int newIntValue = EditorGUI.IntField(position, newTooltipContent, property.intValue);
        if(EditorGUI.EndChangeCheck()) property.intValue = newIntValue;
        break;
      case SerializedPropertyType.String:
        string newStringValue = EditorGUI.TextField(position, newTooltipContent, property.stringValue);
        if(EditorGUI.EndChangeCheck()) property.stringValue = newStringValue;
        break;
      default:
        Debug.LogWarning("TooltipDrawer: Unsupported Type: " + property.propertyType);
        break;
    }
  }

  private GUIContent newTooltipContent {
    get {
      if(_newTooltipContent == null) {
        TooltipAttribute tooltipAttribute = attribute as TooltipAttribute;
        _newTooltipContent = new GUIContent(previousTooltipContent.text, tooltipAttribute.text);
      }
      return _newTooltipContent;
    }
  }

  private Type GetFieldType(SerializedProperty property) {
    if (fieldType == null) {
        Type parentClassType = property.serializedObject.targetObject.GetType();
        FieldInfo fieldInfo = parentClassType.GetField(property.name,
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

        if (fieldInfo == null) {
            Debug.LogWarning("TooltipDrawer: No field info found.");
            return null;
        }
        fieldType = fieldInfo.FieldType;
    }
    return fieldType;
  }

#else

  public override void OnGUI(Rect position, SerializedProperty property, GUIContent tooltipContent) {
    var atr = (TooltipAttribute) attribute;
    var content = new GUIContent(tooltipContent.text, atr.text);
    EditorGUI.PropertyField(position, property, content);
  }

#endif
}
