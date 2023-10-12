using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Extensions
{
	public static class EditorExt
	{
		public const float INDENT_WIDTH = 15f;
		public const float LINE_SPACING = 2f;

		private static Rect _positionCache;
		private static GUIContent _labelCache;
		private static bool _includeChildrenCache;

		public static void DrawProperties(this SerializedObject serializedObject, Type inheritRestrictType = null)
		{
			var properties = serializedObject.GetProperties(inheritRestrictType);
			serializedObject.DrawProperties(properties);
		}

		public static void DrawProperties(this SerializedObject serializedObject, SerializedProperty[] properties)
		{
			serializedObject.Update();
			for (var i = 0; i < properties.Length; i++)
			{
				EditorGUILayout.PropertyField(properties[i]);
			}
			serializedObject.ApplyModifiedProperties();
		}

		public static SerializedProperty[] GetProperties(this SerializedObject serializedObject, Type inheritRestrictType = null)
		{
			var fieldsToDraw = serializedObject.GetFieldsToDraw(inheritRestrictType);
			var result = new SerializedProperty[fieldsToDraw.Count];

			for (var i = 0; i < fieldsToDraw.Count; i++)
			{
				result[i] = serializedObject.FindProperty(fieldsToDraw[i].Name);
			}

			return result;
		}

		public static List<FieldInfo> GetFieldsToDraw(this SerializedObject serializedObject, Type inheritRestrictType = null)
		{
			var restrictFields = inheritRestrictType?.GetInstanceFields(inheritRestrictType);
			var restrictSet = restrictFields == null ? null : new HashSet<FieldInfo>(restrictFields);

			var target = serializedObject.targetObject;
			var targetType = target.GetType();
			var fields = targetType.GetInstanceFields(inheritRestrictType);
			var result = new List<FieldInfo>(fields.Count);

			for (var i = 0; i < fields.Count; i++)
			{
				var field = fields[i];
				if ((restrictSet == null || !restrictSet.Contains(field)) &&
				    (field.IsPublic || field.HasAttribute<SerializeField>()) &&
				    !field.HasAttribute<HideInInspector>())
				{
					result.Add(field);
				}
			}

			return result;
		}

		public static bool IsStandardType(this SerializedProperty property)
		{
			return property.propertyType != SerializedPropertyType.Generic & property.propertyType != SerializedPropertyType.ManagedReference;
		}

		public static string GetParentPropertyPath(this SerializedProperty property, bool includeArray = false)
		{
			return ReflectionExt.GetParentPath(property.propertyPath, includeArray);
		}

		public static Type GetManagedReferenceType(this SerializedProperty property)
		{
			var typeName = property.managedReferenceFullTypename;

			var parts = typeName.Split(' ');
			if (parts.Length == 2)
			{
				var assemblyPart = parts[0];
				var nsClassnamePart = parts[1];
				return Type.GetType($"{nsClassnamePart}, {assemblyPart}");
			}

			return null;
		}

		public static object GetObjectByPath(this SerializedObject serializedObject, string objectPath)
		{
			return ReflectionExt.GetObjectByLocalPath(serializedObject.targetObject, objectPath);
		}

		public static Type GetPropertyTypeByPath(this SerializedObject serializedObject, string propertyPath)
		{
			return GetObjectByPath(serializedObject, propertyPath)?.GetType();
		}

		public static Type GetPropertyType(this SerializedProperty property)
		{
			return GetPropertyTypeByPath(property.serializedObject, property.propertyPath);
		}

		public static Type GetPropertyTypeByLocalPath(this SerializedProperty property, string localPath)
		{
			return GetPropertyTypeByPath(property.serializedObject, property.propertyPath.AppendPath(localPath));
		}

		public static void SetPropertyValueByLocalPath(SerializedObject serializedObject, string propertyPath, object value)
		{
			ReflectionExt.SetObjectByLocalPath(serializedObject.targetObject, propertyPath, value);
		}

		public static void SetPropertyValue(this SerializedProperty property, object value)
		{
			SetPropertyValueByLocalPath(property.serializedObject, property.propertyPath, value);
		}

		public static SerializedProperty GetParentProperty(this SerializedProperty property, bool includeArray = false)
		{
			var parentPath = property.GetParentPropertyPath(includeArray);
			var parent = property.serializedObject.FindProperty(parentPath);

			return parent;
		}

		public static int GetElementIndex(this SerializedProperty property)
		{
			var path = property.propertyPath;
			if (path[path.Length - 1] != ']')
				return 0;
			var beginIndex = path.LastIndexOf("[", StringComparison.Ordinal) + 1;
			var indexString = path.Substring(beginIndex, path.Length - beginIndex - 1);

			return int.Parse(indexString);
		}

		public static object GetResultByLocalPath(this SerializedProperty property, string localPath)
		{
			var result = property.GetPropertyObjectByLocalPath(localPath);
			if (result != null)
				return result;
			result = property.InvokeFuncByLocalPath(localPath);
			if (result != null)
				return result;
			result = property.InvokePropertyByLocalPath(localPath);

			return result;
		}

		public static T GetResultByLocalPath<T>(this SerializedProperty property, string localPath)
		{
			if (property.GetPropertyObjectByLocalPath(localPath) is T value)
				return value;
			if (property.InvokeFuncByLocalPath(localPath) is T funcValue)
				return funcValue;
			if (property.InvokePropertyByLocalPath(localPath) is T propertyValue)
				return propertyValue;

			return default;
		}

		public static SerializedProperty GetPropertyByLocalPath(this SerializedProperty property, string localPath)
		{
			var parentPath = property.GetParentPropertyPath();
			var fullPath = parentPath.AppendPath(localPath);

			return property.serializedObject.FindProperty(fullPath);
		}

		public static object GetPropertyObjectByLocalPath(this SerializedProperty property, string objectPath)
		{
			var parentPath = property.GetParentPropertyPath();
			var fullObjectPath = parentPath.AppendPath(objectPath);

			return ReflectionExt.GetObjectByLocalPath(property.serializedObject.targetObject, fullObjectPath);
		}

		public static void InvokeMethodByLocalPath(this SerializedProperty property, string methodPath)
		{
			var parentPath = property.GetParentPropertyPath();
			var fullMethodPath = parentPath.AppendPath(methodPath);

			ReflectionExt.InvokeMethodByLocalPath(property.serializedObject.targetObject, fullMethodPath);
		}

		public static object InvokeFuncByLocalPath(this SerializedProperty property, string methodPath)
		{
			var parentPath = property.GetParentPropertyPath();
			var fullMethodPath = parentPath.AppendPath(methodPath);

			return ReflectionExt.InvokeFuncByLocalPath(property.serializedObject.targetObject, fullMethodPath);
		}

		public static object InvokePropertyByLocalPath(this SerializedProperty property, string propertyPath)
		{
			var parentPath = property.GetParentPropertyPath();
			var fullPropertyPath = parentPath.AppendPath(propertyPath);

			return ReflectionExt.InvokePropertyByLocalPath(property.serializedObject.targetObject, fullPropertyPath);
		}

		public static void DrawBody(this SerializedProperty property, Rect position)
		{
			if (property.IsStandardType())
			{
				property.PropertyField(position, new GUIContent(" "), true);
				return;
			}
			if (!property.hasVisibleChildren)
			{
				property.PropertyField(position, new GUIContent(" "), false);
				return;
			}
			var currentProperty = property.serializedObject.FindProperty(property.propertyPath);

			currentProperty.NextVisible(true);
			do
			{
				if (!currentProperty.propertyPath.StartsWith(property.propertyPath + ReflectionExt.PATH_SPLIT_CHAR))
				{
					break;
				}

				var height = currentProperty.GetPropertyHeight(true);
				position.height = height;
				currentProperty.PropertyField(position, includeChildren: true);
				position.y += height + LINE_SPACING;
			} while (currentProperty.NextVisible(false));

			currentProperty.Dispose();
		}

		public static float GetBodyHeight(this SerializedProperty property)
		{
			if (property.propertyType != SerializedPropertyType.ManagedReference)
				return property.GetPropertyHeight(true);

			var height = property.GetPropertyHeight(false);
			if (!property.hasVisibleChildren)
			{
				return height;
			}
			var currentProperty = property.serializedObject.FindProperty(property.propertyPath);

			currentProperty.NextVisible(true);
			do
			{
				if (!currentProperty.propertyPath.StartsWith(property.propertyPath + ReflectionExt.PATH_SPLIT_CHAR))
				{
					break;
				}

				height += currentProperty.GetBodyHeight();
			}
			while (currentProperty.NextVisible(false));
			currentProperty.Dispose();

			return height;
		}

		public static void PropertyField(this SerializedProperty property, Rect position, GUIContent label = null, bool includeChildren = true)
		{
			_positionCache = position;
			_labelCache = label;
			_includeChildrenCache = includeChildren;

			property.PropertyField_Cached();
		}

		public static float GetPropertyHeight(this SerializedProperty property, bool includeChildren)
		{
			_includeChildrenCache = includeChildren;

			return property.GetPropertyHeight_Cached();
		}

		public static void PropertyField_Cached(this SerializedProperty property)
		{
			EditorGUI.PropertyField(_positionCache, property, _labelCache, _includeChildrenCache);
		}

		public static float GetPropertyHeight_Cached(this SerializedProperty property)
		{
			return EditorGUI.GetPropertyHeight(property, _includeChildrenCache);;
		}

		public static bool HasChildren(this SerializedProperty property)
		{
			// property.hasChildren doesn't work properly
			var copy = property.Copy();
			if (!copy.NextVisible(true))
				return false;

			return copy.GetParentPropertyPath() == property.propertyPath;
		}
	}
}