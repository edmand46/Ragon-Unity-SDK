using UnityEditor;
using UnityEngine;

namespace Ragon.Client.Unity.Property
{
  [CustomPropertyDrawer(typeof(RagonVector3))]
  public class RagonVector3Drawer : PropertyDrawer
  {
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
      EditorGUI.BeginProperty(position, label, property);

      var originalName = label.text;
      label.text = $"[R] {originalName}";
      position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
      var valueRect = new Rect(position.x, position.y + 2, position.width, position.height);
      var value = property.FindPropertyRelative("_value");
      
      EditorGUI.PropertyField(valueRect, value, GUIContent.none);
      EditorGUI.EndProperty();
    }
  }
}