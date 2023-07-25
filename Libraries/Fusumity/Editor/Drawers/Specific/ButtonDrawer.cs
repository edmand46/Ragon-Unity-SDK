using Fusumity.Attributes.Specific;
using Fusumity.Editor.Extensions;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(ButtonAttribute))]
	public class ButtonDrawer : FusumityPropertyDrawer
	{
		public override void ModifyPropertyData()
		{
			base.ModifyPropertyData();

			var boolButtonAttribute = (ButtonAttribute)attribute;

			if (boolButtonAttribute.drawBefore)
			{
				currentPropertyData.hasBeforeExtension = true;
				currentPropertyData.beforeExtensionHeight += EditorGUIUtility.singleLineHeight;
			}
			else
			{
				currentPropertyData.hasAfterExtension = true;
				currentPropertyData.afterExtensionHeight += EditorGUIUtility.singleLineHeight;
			}

			if (boolButtonAttribute.hidePropertyField)
			{
				currentPropertyData.hasLabel = false;
				currentPropertyData.hasBody = false;
				currentPropertyData.hasSubBody = false;
				currentPropertyData.hasFoldout = false;
			}
		}

		public override void DrawBeforeExtension(ref Rect position)
		{
			base.DrawBeforeExtension(ref position);

			var boolButtonAttribute = (ButtonAttribute)attribute;
			if (!boolButtonAttribute.drawBefore)
				return;

			DrawButton(ref position, boolButtonAttribute);
		}

		public override void DrawAfterExtension(ref Rect position)
		{
			base.DrawAfterExtension(ref position);

			var boolButtonAttribute = (ButtonAttribute)attribute;
			if (boolButtonAttribute.drawBefore)
				return;

			DrawButton(ref position, boolButtonAttribute);
		}

		private void DrawButton(ref Rect position, ButtonAttribute boolButtonAttribute)
		{
			var drawPosition = EditorGUI.IndentedRect(position);
			drawPosition.height = EditorGUIUtility.singleLineHeight;
			position.yMin += EditorGUIUtility.singleLineHeight;

			var label = string.IsNullOrEmpty(boolButtonAttribute.buttonName)
				? (string.IsNullOrEmpty(boolButtonAttribute.methodPath)
					? currentPropertyData.label
					: new GUIContent(boolButtonAttribute.methodPath))
				: new GUIContent(boolButtonAttribute.buttonName);

			var isPressed = GUI.Button(drawPosition, label);
			if (!isPressed)
				return;

			if (string.IsNullOrEmpty(boolButtonAttribute.methodPath))
				return;

			Undo.RecordObject(currentPropertyData.property.serializedObject.targetObject, boolButtonAttribute.buttonName);
			currentPropertyData.property.InvokeMethodByLocalPath(boolButtonAttribute.methodPath);
			currentPropertyData.property.serializedObject.targetObject.SaveChanges();
		}
	}
}