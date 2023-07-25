using System;
using Fusumity.Attributes.Specific;
using Fusumity.Editor.Assistance;
using Fusumity.Editor.Extensions;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(ReferenceSelectionAttribute))]
	public class ReferenceSelectionAttributeDrawer : FusumityPropertyDrawer
	{
		private const float COPY_PASTE_BUTTONS_WIDTH = 50f;

		private Type[] _currentTypes;
		private Type _selectedType;

		public override bool OverrideMethods => (currentPropertyData.property.propertyType == SerializedPropertyType.ManagedReference);

		public override void ModifyPropertyData()
		{
			base.ModifyPropertyData();

			var property = currentPropertyData.property;
			if (!OverrideMethods || property.propertyType != SerializedPropertyType.ManagedReference)
				return;

			currentPropertyData.labelIntersectSubBody = false;
			currentPropertyData.hasFoldout = _selectedType != null;
			currentPropertyData.hasSubBody = true;
			currentPropertyData.hasBody = property.GetManagedReferenceType() != null;
		}

		public override void DrawSubBody(Rect position)
		{
			var property = currentPropertyData.property;
			if (property.propertyType != SerializedPropertyType.ManagedReference)
			{
				base.DrawSubBody(position);
				return;
			}

			var fieldType = fieldInfo.FieldType.IsArray ? fieldInfo.FieldType.GetElementType() :
				(fieldInfo.FieldType.IsList() ? fieldInfo.FieldType.GetGenericArguments()[0] : fieldInfo.FieldType);

			var attr = (ReferenceSelectionAttribute)attribute;
			var targetType = attr.type ?? fieldType;
			var currentType = property.GetManagedReferenceType();

			SelectType(position, currentType, targetType, attr.insertNull);
		}

		protected void SelectType(Rect position, Type currentType, Type targetType, bool insertNull)
		{
			var property = currentPropertyData.property;
			var propertyPath = property.propertyPath;

			_selectedType = currentType;
			_currentTypes ??= targetType.GetInheritorTypes(insertNull);

			var typeName = currentType == null ? "None" : currentType.Name;

			var dropdownPosition = position;
			dropdownPosition.xMin += EditorGUI.indentLevel * EditorExt.INDENT_WIDTH;
			dropdownPosition.xMax -= COPY_PASTE_BUTTONS_WIDTH;

			if (EditorGUI.DropdownButton(dropdownPosition, new GUIContent(typeName), default))
			{
				position = new Rect(position.x, position.y + position.height, position.width, 200f);
				var v = GUIUtility.GUIToScreenPoint(new Vector2(position.x, position.y));
				position.x = v.x;
				position.y = v.y;

				var popup = new Popup()
				{
					Title = targetType.Name,
					AutoClose = true,
					ScreenRect = position,
					Separator = '.',
					AutoHeight = false,
				};
				var i = 0;
				if (insertNull)
				{
					popup.Item("None", item => { Select(propertyPath ,item.order); }, false, i++);
				}
				for (; i < _currentTypes.Length; i++)
				{
					popup.Item(ToCamelCaseSpace(_currentTypes[i].Name), item => { Select(propertyPath, item.order); }, true, i);
				}
				popup.Show();
			}

			var copyPosition = position;
			copyPosition.xMin = copyPosition.xMax - COPY_PASTE_BUTTONS_WIDTH;
			copyPosition.xMax = copyPosition.xMax - COPY_PASTE_BUTTONS_WIDTH / 2;
			if (GUI.Button(copyPosition, "C"))
			{
				property.CopyManagedReferenceValue();
			}

			var pastPosition = position;
			pastPosition.xMin = copyPosition.xMax;
			if (GUI.Button(pastPosition, "P"))
			{
				property.PasteManagedReferenceValue();
			}
		}

		private void Select(string propertyPath, int newSelected)
		{
			var newType = GetType(newSelected);

			if (_selectedType == newType)
				return;

			var newValue = newType == null ? null : Activator.CreateInstance(newType, true);
			var property = currentPropertyData.property.serializedObject.FindProperty(propertyPath);

			SetValue(property, newValue);
		}

		protected virtual void SetValue(SerializedProperty property, object value)
		{
			property.managedReferenceValue = value;

			property.serializedObject.ApplyModifiedProperties();
			if (property.serializedObject.context != null)
				EditorUtility.SetDirty(property.serializedObject.context);
		}

		private Type GetType(int typeIndex)
		{
			if (typeIndex < 0 | typeIndex >= _currentTypes.Length)
				return null;
			return _currentTypes[typeIndex];
		}

		private static string ToCamelCaseSpace(string caption)
		{
			if (string.IsNullOrEmpty(caption))
				return string.Empty;
			var str = System.Text.RegularExpressions.Regex.Replace(caption, "[A-Z]", " $0").Trim();
			return char.ToUpper(str[0]) + str.Substring(1);
		}
	}
}