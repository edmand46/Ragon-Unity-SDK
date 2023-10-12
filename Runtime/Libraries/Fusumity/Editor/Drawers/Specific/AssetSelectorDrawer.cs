using Fusumity.Attributes.Specific;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(AssetSelectorAttribute))]
	public class AssetSelectorDrawer : FusumityPropertyDrawer
	{
		public override bool OverrideMethods => (currentPropertyData.property.propertyType == SerializedPropertyType.ObjectReference);

		public override void DrawSubBody(Rect position)
		{
			var property = currentPropertyData.property;
			if (property.propertyType != SerializedPropertyType.ObjectReference)
			{
				base.DrawSubBody(position);
				return;
			}

			var fieldType = fieldInfo.FieldType.IsArray ? fieldInfo.FieldType.GetElementType() : fieldInfo.FieldType;

			var attr = (AssetSelectorAttribute)attribute;
			var targetType = attr.type ?? fieldType;

			var value = property.objectReferenceValue;
			if (typeof(Component).IsAssignableFrom(targetType))
			{
				var component = value as Component;
				var gameObject = component == null ? null : component.gameObject;

				var result = EditorGUI.ObjectField(position, new GUIContent(" "), gameObject, typeof(GameObject), false) as GameObject;

				if (result == null)
					property.objectReferenceValue = result;
				else if (result != gameObject)
				{
					if (result.TryGetComponent(targetType, out component))
						property.objectReferenceValue = component;
				}
			}
			else
			{
				property.objectReferenceValue = EditorGUI.ObjectField(position, new GUIContent(" "), value, targetType, false);
			}
		}
	}
}