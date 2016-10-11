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

/**
 * Custom Property Drawer for Ranged inspector properties that also
 * have a tooltip
 *
 **/
@CustomPropertyDrawer(RangedTooltipAttribute)
class RangedTooltipDrawer extends PropertyDrawer {

  // Draw the property inside the given rect
  function OnGUI(position : Rect, property : SerializedProperty,
      label : GUIContent) {
    // First get the attribute since it contains the range for the slider
    var range : RangedTooltipAttribute = attribute as RangedTooltipAttribute;
    var content = new GUIContent(label.text, range.text);

    if (property.propertyType == SerializedPropertyType.Float) {
      EditorGUI.Slider(position, property, range.min, range.max, content);
    } else if (property.propertyType == SerializedPropertyType.Integer) {
      EditorGUI.IntSlider(position, property, range.min, range.max, content);
    } else {
      EditorGUI.LabelField(position, label.text,
          'Use Range with float or int.');
    }
  }
}
