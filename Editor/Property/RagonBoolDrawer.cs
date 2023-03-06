﻿using UnityEditor;
using UnityEngine;

namespace Ragon.Client.Unity.Property
{
  [CustomPropertyDrawer(typeof(RagonBool))]
  public class RagonBoolDrawer : PropertyDrawer
  {
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
      EditorGUI.BeginProperty(position, label, property);
      
      var originalName = label.text;
      label.text = $"[R] {originalName}";
      position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
     
      var valueRect = new Rect(position.x, position.y, position.width, position.height);
      var value = property.FindPropertyRelative("_value");
      
      GUI.enabled = false;
      EditorGUI.Toggle(valueRect, value.boolValue);
      GUI.enabled = true;
      
      EditorGUI.EndProperty();
    }
  }
}
