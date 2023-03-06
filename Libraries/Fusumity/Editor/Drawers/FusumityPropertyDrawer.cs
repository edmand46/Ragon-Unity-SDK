using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fusumity.Attributes;
using Fusumity.Editor.Extensions;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Drawers
{
	[CustomPropertyDrawer(typeof(FusumityDrawerAttribute))]
	[CustomPropertyDrawer(typeof(IFusumitySerializable), true)]
	public class FusumityPropertyDrawer : PropertyDrawer
	{
		private const float INDENT_WIDTH = 15f;

		private static readonly Type BASE_DRAWER_TYPE = typeof(FusumityPropertyDrawer);
		private static readonly Type ATTRIBUTE_TYPE = typeof(FusumityDrawerAttribute);

		private static Dictionary<Type, Type> _attributeTypeToDrawerType;
		private static HashSet<string> _currentPropertyPath = new HashSet<string>();

		private FusumityDrawerAttribute[] _fusumityAttributes;
		private FusumityPropertyDrawer[] _fusumityDrawers;
		// If not use this arrays will suck (very long story).
		private Dictionary<string, PropertyData> _pathToPropertyData;

		protected PropertyData currentPropertyData;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (_currentPropertyPath.Contains(property.propertyPath))
			{
				// When you open the unity Object selection field the _currentPropertyPath will not cleaned in that frame :(
				if (_currentPropertyPath.Contains(property.GetParentPropertyPath()))
				{
					return property.GetPropertyHeight_Cached();
				}
				_currentPropertyPath.Clear();
			}

			_currentPropertyPath.Add(property.propertyPath);

			LazyInitializeAttributes();
			LazyInitializeDrawers();
			LazyInitializePropertyData(property.propertyPath);
			SetupPropertyData(property.propertyPath);

			currentPropertyData.ResetData(property, label);
			ExecuteModifyPropertyData();

			_currentPropertyPath.Remove(property.propertyPath);
			return currentPropertyData.GetTotalHeight();
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (_currentPropertyPath.Contains(property.propertyPath))
			{
				property.PropertyField_Cached();
				return;
			}

			SetupPropertyData(property.propertyPath);

			if (!(currentPropertyData is { drawProperty: true }))
				return;

			_currentPropertyPath.Add(property.propertyPath);

			var oldBackgroundColor = GUI.backgroundColor;
			var oldGuiEnabled = GUI.enabled;
			var lastIndentLevel = EditorGUI.indentLevel;
			var lastLabelWidth = EditorGUIUtility.labelWidth;

			GUI.backgroundColor = currentPropertyData.backgroundColor;
			GUI.enabled &= currentPropertyData.isEnabled;

			EditorGUI.BeginChangeCheck();

			var propertyPosition = position;
			if (currentPropertyData.hasBeforeExtension)
				propertyPosition.yMin += currentPropertyData.beforeExtensionHeight;
			if (currentPropertyData.hasAfterExtension)
				propertyPosition.yMax -= currentPropertyData.afterExtensionHeight;

			var beforeExtensionPosition = currentPropertyData.hasBeforeExtension
				? new Rect(position.x, position.y, position.width, currentPropertyData.beforeExtensionHeight)
				: Rect.zero;

			var labelPosition = currentPropertyData.hasLabel
				? new Rect(propertyPosition.x, propertyPosition.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight)
				: Rect.zero;
			var foldoutPosition = currentPropertyData.hasFoldout
				? new Rect(propertyPosition.x, propertyPosition.y, propertyPosition.width, EditorGUIUtility.singleLineHeight)
				: Rect.zero;
			var subBodyPosition = currentPropertyData.hasLabel & !currentPropertyData.labelIntersectSubBody
				? new Rect(propertyPosition.x + EditorGUIUtility.labelWidth, propertyPosition.y,
					propertyPosition.width - EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight)
				: new Rect(propertyPosition.x, propertyPosition.y, propertyPosition.width, EditorGUIUtility.singleLineHeight);
			var bodyPosition = (currentPropertyData.hasLabel | currentPropertyData.hasSubBody)
				? new Rect(propertyPosition.x, propertyPosition.y + EditorGUIUtility.singleLineHeight, propertyPosition.width,
					propertyPosition.height - EditorGUIUtility.singleLineHeight)
				: propertyPosition;

			var afterExtensionPosition = currentPropertyData.hasAfterExtension
				? new Rect(position.x, propertyPosition.yMax, position.width, currentPropertyData.afterExtensionHeight)
				: Rect.zero;

			ExecuteValidateBeforeDrawing();

			if (currentPropertyData.hasBeforeExtension)
			{
				ExecuteDrawBeforeExtension(beforeExtensionPosition);
			}

			if (currentPropertyData.hasLabel)
			{
				ExecuteDrawLabel(labelPosition);
			}

			if (currentPropertyData.hasFoldout)
			{
				currentPropertyData.property.isExpanded = EditorGUI.Foldout(foldoutPosition, property.isExpanded, "");
			}

			if (currentPropertyData.ShouldDrawSubBody())
			{
				if (!currentPropertyData.hasLabel | !currentPropertyData.labelIntersectSubBody)
				{
					EditorGUIUtility.labelWidth = INDENT_WIDTH;
				}

				ExecuteDrawSubBody(subBodyPosition);

				EditorGUIUtility.labelWidth = lastLabelWidth;
			}

			if (currentPropertyData.ShouldDrawBody())
			{
				if (currentPropertyData.hasLabel)
				{
					EditorGUI.indentLevel++;
				}

				ExecuteDrawBody(bodyPosition);
			}

			if (currentPropertyData.hasAfterExtension)
			{
				ExecuteDrawAfterExtension(afterExtensionPosition);
			}

			if (EditorGUI.EndChangeCheck())
			{
				ExecuteOnPropertyChanged();
			}

			EditorGUIUtility.labelWidth = lastLabelWidth;
			EditorGUI.indentLevel = lastIndentLevel;
			GUI.enabled = oldGuiEnabled;
			GUI.backgroundColor = oldBackgroundColor;

			_currentPropertyPath.Remove(property.propertyPath);
		}

		#region Initialization

		private void LazyInitializeAttributes()
		{
			if (_fusumityAttributes is { Length: > 0 })
				return;

			var attributes = new List<FusumityDrawerAttribute>();
			var customAttributes = fieldInfo.GetCustomAttributes();

			foreach (var customAttribute in customAttributes)
			{
				if (!(customAttribute is FusumityDrawerAttribute fusumityDrawerAttribute))
					continue;
				if (fusumityDrawerAttribute.Equals(attribute))
					continue;
				attributes.Add(fusumityDrawerAttribute);
			}

			_fusumityAttributes = attributes.ToArray();
		}

		private void LazyInitializeDrawers()
		{
			if (_fusumityDrawers is { Length: > 0 })
				return;

			if (_attributeTypeToDrawerType == null)
			{
				var drawersTypes = BASE_DRAWER_TYPE.GetInheritorTypes();
				_attributeTypeToDrawerType = new Dictionary<Type, Type>(drawersTypes.Length * 3);

				foreach (var drawerType in drawersTypes)
				{
					var customAttributes = drawerType.GetCustomAttributes<CustomPropertyDrawer>();

					foreach (var customAttribute in customAttributes)
					{
						var customAttributeTypes = customAttribute.GetCustomPropertyDrawerTypes();
						foreach (var customAttributeType in customAttributeTypes)
						{
							if (!_attributeTypeToDrawerType.ContainsKey(customAttributeType))
							{
								_attributeTypeToDrawerType.Add(customAttributeType, drawerType);
							}
							else
							{
								Debug.Log(customAttributeType.Name);
								Debug.Log(drawerType.Name);
							}
						}
					}
				}
			}

			_fusumityDrawers = new FusumityPropertyDrawer[_fusumityAttributes.Length];

			for (var i = 0; i < _fusumityAttributes.Length; i++)
			{
				var genericAttribute = _fusumityAttributes[i];

				if (!_attributeTypeToDrawerType.TryGetValue(genericAttribute.GetType(), out var drawerType))
				{
					drawerType = BASE_DRAWER_TYPE;
				}

				var drawer = (FusumityPropertyDrawer)Activator.CreateInstance(drawerType);
				drawer.SetAttribute(genericAttribute);
				drawer.SetFieldInfo(fieldInfo);

				_fusumityDrawers[i] = drawer;
			}
		}


		private void LazyInitializePropertyData(string propertyPath)
		{
			if (_pathToPropertyData == null)
				_pathToPropertyData = new Dictionary<string, PropertyData>();
			if (!_pathToPropertyData.TryGetValue(propertyPath, out var drawerData))
			{
				drawerData = new PropertyData();
				currentPropertyData = new PropertyData();
				_pathToPropertyData.Add(propertyPath, drawerData);
			}
		}

		private void SetupPropertyData(string propertyPath)
		{
			if (_pathToPropertyData == null || !_pathToPropertyData.TryGetValue(propertyPath, out currentPropertyData))
				return;

			foreach (var drawer in _fusumityDrawers)
			{
				drawer.currentPropertyData = currentPropertyData;
			}
		}

		#endregion

		#region Custom Executers

		private void ExecuteModifyPropertyData()
		{
			ModifyPropertyData();

			foreach (var drawer in _fusumityDrawers)
			{
				drawer.ModifyPropertyData();
			}
		}

		private void ExecuteValidateBeforeDrawing()
		{
			ValidateBeforeDrawing();

			foreach (var drawer in _fusumityDrawers)
			{
				drawer.ValidateBeforeDrawing();
			}
		}

		private void ExecuteDrawBeforeExtension(Rect position)
		{
			DrawBeforeExtension(ref position);
			foreach (var drawer in _fusumityDrawers)
			{
				drawer.DrawBeforeExtension(ref position);
			}
		}

		private void ExecuteDrawLabel(Rect position)
		{
			if (!this.IsDrawLabelOverriden())
			{
				foreach (var drawer in _fusumityDrawers)
				{
					if (drawer.IsDrawLabelOverriden())
					{
						drawer.DrawLabel(position);
						return;
					}
				}
			}

			DrawLabel(position);
		}

		private void ExecuteDrawSubBody(Rect position)
		{
			if (!this.IsDrawSubBodyOverriden())
			{
				foreach (var drawer in _fusumityDrawers)
				{
					if (drawer.IsDrawSubBodyOverriden())
					{
						drawer.DrawSubBody(position);
						return;
					}
				}
			}

			DrawSubBody(position);
		}

		private void ExecuteDrawBody(Rect position)
		{
			if (!this.IsDrawBodyOverriden())
			{
				foreach (var drawer in _fusumityDrawers)
				{
					if (drawer.IsDrawBodyOverriden())
					{
						drawer.DrawBody(position);
						return;
					}
				}
			}

			DrawBody(position);
		}

		private void ExecuteDrawAfterExtension(Rect position)
		{
			DrawAfterExtension(ref position);
			foreach (var drawer in _fusumityDrawers)
			{
				drawer.DrawAfterExtension(ref position);
			}
		}

		private void ExecuteOnPropertyChanged()
		{
			OnPropertyChanged();

			foreach (var drawer in _fusumityDrawers)
			{
				drawer.OnPropertyChanged();
			}
		}

		#endregion

		#region Custom

		public virtual void ModifyPropertyData() {}

		public virtual void ValidateBeforeDrawing() {}

		public virtual void DrawBeforeExtension(ref Rect position) {}

		public virtual void DrawLabel(Rect position)
		{
			EditorGUI.LabelField(position, currentPropertyData.label, currentPropertyData.labelStyle);
		}

		public virtual void DrawSubBody(Rect position)
		{
			currentPropertyData.property.DrawBody(position);
		}

		public virtual void DrawBody(Rect position)
		{
			currentPropertyData.property.DrawBody(position);
		}

		public virtual void DrawAfterExtension(ref Rect position) {}

		public virtual void OnPropertyChanged() {}

		#endregion
	}

	public class PropertyData
	{
		public SerializedProperty property;
		public GUIContent label;

		public bool drawPropertyChanged;
		public bool drawProperty;
		public bool isEnabledChanged;
		public bool isEnabled;

		public bool hasBeforeExtension;
		public bool hasFoldout;
		public bool hasLabel;
		public bool hasSubBody;
		public bool hasBody;
		public bool hasAfterExtension;

		public bool drawSubBodyWhenRollUp;
		public bool labelIntersectSubBody;

		public float beforeExtensionHeight;
		public float labelHeight;
		public float bodyHeight;
		public float afterExtensionHeight;
		public GUIStyle labelStyle;

		public Color backgroundColor;

		public void ResetData(SerializedProperty property, GUIContent label)
		{
			this.property = property;
			this.label = new GUIContent(label);

			drawPropertyChanged = false;
			drawProperty = true;
			isEnabledChanged = false;
			isEnabled = true;

			var hasChildren = property.HasChildren();

			hasBeforeExtension = false;
			hasFoldout = hasChildren;
			hasLabel = true;
			hasSubBody = !hasChildren;
			hasBody = hasChildren & property.isExpanded;
			hasAfterExtension = false;

			drawSubBodyWhenRollUp = true;
			labelIntersectSubBody = true;

			beforeExtensionHeight = 0f;
			labelHeight = EditorGUIUtility.singleLineHeight;
			bodyHeight = property.GetPropertyHeight(true);
			afterExtensionHeight = 0f;
			if (hasChildren)
				bodyHeight -= labelHeight;

			labelStyle = EditorStyles.label;

			backgroundColor = GUI.backgroundColor;
		}

		public float GetTotalHeight()
		{
			var height = 0f;
			if (!drawProperty)
				return height;

			if (hasBeforeExtension)
			{
				height += beforeExtensionHeight;
			}
			if (hasLabel || ShouldDrawSubBody())
			{
				height += labelHeight;
			}
			if (ShouldDrawBody())
			{
				height += bodyHeight;
			}
			if (hasAfterExtension)
			{
				height += afterExtensionHeight;
			}

			return height;
		}

		public bool ShouldDrawSubBody()
		{
			return hasSubBody & (property.isExpanded | !hasFoldout | drawSubBodyWhenRollUp);
		}

		public bool ShouldDrawBody()
		{
			return hasBody & (property.isExpanded | !hasFoldout);
		}
	}
}