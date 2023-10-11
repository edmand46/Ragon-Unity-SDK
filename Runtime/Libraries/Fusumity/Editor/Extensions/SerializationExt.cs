using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Extensions
{
	public class SerializationExt : MonoBehaviour
	{
		public static float GetObjectHeight(object source)
		{
			if (source == null)
				return EditorGUIUtility.singleLineHeight;

			var type = source.GetType();

			var fields = type.GetFields(ReflectionExt.FIELD_BINDING_FLAGS);
			var height = GetFieldsHeight(source, fields);

			var baseType = type.BaseType;
			while (baseType != null)
			{
				fields = baseType.GetFields(ReflectionExt.INTERNAL_FIELD_BINDING_FLAGS);
				height += GetFieldsHeight(source, fields);
				baseType = baseType.BaseType;
			}

			return height;
		}

		private static float GetFieldsHeight(object source, FieldInfo[] fields)
		{
			var height = 0f;
			foreach (var field in fields)
			{
				if (field.IsPrivate && field.GetCustomAttribute<SerializeField>() == null ||
				    field.GetCustomAttribute<HideInInspector>() != null)
					continue;

				var value = field.GetValue(source);
				height += GetObjectHeight(value);
			}

			return height;
		}
	}
}