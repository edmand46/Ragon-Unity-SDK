using System;
using System.Reflection;
using Fusumity.Editor.Drawers;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Extensions
{
	public static class PropertyDrawerExt
	{
		private static readonly Type CUSTOM_PROPERTY_DRAWER_TYPE = typeof(CustomPropertyDrawer);
		private const string CUSTOM_PROPERTY_DRAWER_FIELD_TYPE = "m_Type";
		private const string CUSTOM_PROPERTY_DRAWER_FIELD_USE_FOR_CHILDREN = "m_UseForChildren";

		private static readonly Type PROPERTY_DRAWER_TYPE = typeof(PropertyDrawer);
		private const string PROPERTY_DRAWER_FIELD_ATTRIBUTE = "m_Attribute";
		private const string PROPERTY_DRAWER_FIELD_FIELD_INFO = "m_FieldInfo";

		private const string CUSTOM_PROPERTY_DRAWER_METHOD_DRAW_LABEL = "DrawLabel";
		private const string CUSTOM_PROPERTY_DRAWER_METHOD_DRAW_SUB_BODY = "DrawSubBody";
		private const string CUSTOM_PROPERTY_DRAWER_METHOD_DRAW_BODY = "DrawBody";

		public static Type[] GetCustomPropertyDrawerTypes(this CustomPropertyDrawer drawer)
		{
			var typeField = CUSTOM_PROPERTY_DRAWER_TYPE.GetField(CUSTOM_PROPERTY_DRAWER_FIELD_TYPE, ReflectionExt.INTERNAL_FIELD_BINDING_FLAGS);
			var useForChildrenField = CUSTOM_PROPERTY_DRAWER_TYPE.GetField(CUSTOM_PROPERTY_DRAWER_FIELD_USE_FOR_CHILDREN, ReflectionExt.INTERNAL_FIELD_BINDING_FLAGS);

			var type = (Type)typeField.GetValue(drawer);
			var useForChildren = (bool)useForChildrenField.GetValue(drawer);

			return useForChildren ? type.GetInheritorTypes() : new[] { type };
		}

		public static void SetAttribute(this PropertyDrawer drawer, PropertyAttribute attribute)
		{
			var attributeField = PROPERTY_DRAWER_TYPE.GetField(PROPERTY_DRAWER_FIELD_ATTRIBUTE, ReflectionExt.INTERNAL_FIELD_BINDING_FLAGS);
			attributeField.SetValue(drawer, attribute);
		}

		public static void SetFieldInfo(this PropertyDrawer drawer, FieldInfo fieldInfo)
		{
			var fieldInfoField = PROPERTY_DRAWER_TYPE.GetField(PROPERTY_DRAWER_FIELD_FIELD_INFO, ReflectionExt.INTERNAL_FIELD_BINDING_FLAGS);
			fieldInfoField.SetValue(drawer, fieldInfo);
		}

		public static bool IsDrawLabelOverriden(this FusumityPropertyDrawer drawer)
		{
			return drawer.IsDrawerMethodOverriden(CUSTOM_PROPERTY_DRAWER_METHOD_DRAW_LABEL);
		}

		public static bool IsDrawSubBodyOverriden(this FusumityPropertyDrawer drawer)
		{
			return drawer.IsDrawerMethodOverriden(CUSTOM_PROPERTY_DRAWER_METHOD_DRAW_SUB_BODY);
		}

		public static bool IsDrawBodyOverriden(this FusumityPropertyDrawer drawer)
		{
			return drawer.IsDrawerMethodOverriden(CUSTOM_PROPERTY_DRAWER_METHOD_DRAW_BODY);
		}

		private static bool IsDrawerMethodOverriden(this FusumityPropertyDrawer drawer, string name)
		{
			var drawerType = drawer.GetType();
			var methodInfo = drawerType.GetMethod(name, ReflectionExt.OVERRIDEN_METHOD_BINDING_FLAGS, null, new [] {typeof(Rect)}, null);

			return methodInfo != null;
		}
	}
}