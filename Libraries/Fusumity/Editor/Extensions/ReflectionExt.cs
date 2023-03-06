using System;
using System.Collections.Generic;
using System.Reflection;

namespace Fusumity.Editor.Extensions
{
	public static class ReflectionExt
	{
		public const BindingFlags FIELD_BINDING_FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField;
		public const BindingFlags INTERNAL_FIELD_BINDING_FLAGS = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField;

		public const BindingFlags METHOD_BINDING_FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
		public const BindingFlags OVERRIDEN_METHOD_BINDING_FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
		public const BindingFlags PRIVATE_METHOD_BINDING_FLAGS = BindingFlags.NonPublic | BindingFlags.Instance;

		public const char PATH_PARENT_CHAR = '/';
		public const char PATH_SPLIT_CHAR = '.';
		public const char ARRAY_DATA_TERMINATOR = ']';
		public const string ARRAY_DATA_BEGINNER = "data[";

		private static readonly Dictionary<Type, Type[]> ASSIGNABLE_FROM = new Dictionary<Type, Type[]>();
		private static readonly Dictionary<Type, Type[]> TYPES_WITH_NULL = new Dictionary<Type, Type[]>();
		private static readonly Dictionary<Type, Type[]> TYPES_WITHOUT_NULL = new Dictionary<Type, Type[]>();

		public static Type[] GetInheritorTypes(this Type baseType, bool insertNull = false)
		{
			Type[] inheritorTypes;
			if (insertNull)
			{
				if (TYPES_WITH_NULL.TryGetValue(baseType, out inheritorTypes))
					return inheritorTypes;
			}
			else if (TYPES_WITHOUT_NULL.TryGetValue(baseType, out inheritorTypes))
				return inheritorTypes;

			if (!ASSIGNABLE_FROM.TryGetValue(baseType, out var typeArray))
			{
				var assemblies = AppDomain.CurrentDomain.GetAssemblies();
				var typeList = new List<Type>();
				for (int a = 0; a < assemblies.Length; a++)
				{
					var types = assemblies[a].GetTypes();
					for (int t = 0; t < types.Length; t++)
					{
						if (baseType.IsAssignableFrom(types[t]) && !types[t].IsInterface && !types[t].IsAbstract &&
						    !types[t].IsGenericType)
						{
							typeList.Add(types[t]);
						}
					}
				}

				typeArray = typeList.ToArray();
				ASSIGNABLE_FROM.Add(baseType, typeArray);
			}

			if (insertNull)
			{
				inheritorTypes = new Type[typeArray.Length + 1];
				Array.ConstrainedCopy(typeArray, 0, inheritorTypes, 1, typeArray.Length);
			}
			else
			{
				inheritorTypes = typeArray;
			}

			(insertNull ? TYPES_WITH_NULL : TYPES_WITHOUT_NULL).Add(baseType, inheritorTypes);

			return inheritorTypes;
		}

		public static void SetObjectByLocalPath(object source, string objectPath, object value)
		{
			var target = source;
			if (string.IsNullOrEmpty(objectPath))
				return;

			var pathComponents = objectPath.Split(PATH_SPLIT_CHAR);

			for (var p = 0; p < pathComponents.Length; p++)
			{
				var pathComponent = pathComponents[p];
				if (target is Array array)
				{
					if (p < pathComponents.Length - 1 && pathComponents[p + 1].StartsWith(ARRAY_DATA_BEGINNER))
					{
						var index = int.Parse(pathComponents[++p].Replace(ARRAY_DATA_BEGINNER, "").Replace($"{ARRAY_DATA_TERMINATOR}", ""));

						if (p + 1 == pathComponents.Length)
						{
							array.SetValue(value, index);
							return;
						}
						target = array.GetValue(index);
					}
				}
				else
				{
					var field = GetAnyField(target.GetType(), pathComponent);

					if (p + 1 == pathComponents.Length)
					{
						field.SetValue(target, value);
						return;
					}
					target = field.GetValue(target);
				}
			}
		}

		public static object GetObjectByLocalPath(object source, string objectPath)
		{
			var target = source;
			if (string.IsNullOrEmpty(objectPath))
				return target;

			var pathComponents = objectPath.Split(PATH_SPLIT_CHAR);

			for (var p = 0; p < pathComponents.Length; p++)
			{
				var pathComponent = pathComponents[p];
				if (target is Array array)
				{
					if (p < pathComponents.Length - 1 && pathComponents[p + 1].StartsWith(ARRAY_DATA_BEGINNER))
					{
						var index = int.Parse(pathComponents[++p].Replace(ARRAY_DATA_BEGINNER, "").Replace($"{ARRAY_DATA_TERMINATOR}", ""));
						target = array.GetValue(index);
					}
				}
				else
				{
					var field = GetAnyField(target.GetType(), pathComponent);
					target = field.GetValue(target);
				}
			}

			return target;
		}

		public static Type GetTypeByLocalPath(object source, string propertyPath)
		{
			return GetObjectByLocalPath(source, propertyPath).GetType();
		}

		public static string GetParentPath(string propertyPath, bool includeArray = false)
		{
			return GetParentPath(propertyPath, out _, includeArray);
		}

		public static string GetParentPath(string propertyPath, out string localPath, bool includeArray = false)
		{
			var removeIndex = propertyPath.LastIndexOf(PATH_SPLIT_CHAR);
			if (removeIndex >= 0)
			{
				localPath = propertyPath.Remove(0, removeIndex + 1);
				propertyPath = propertyPath.Remove(removeIndex, propertyPath.Length - removeIndex);

				if (localPath[^1] != ARRAY_DATA_TERMINATOR)
					return propertyPath;

				// Remove "{field name}.Array"
				removeIndex = propertyPath.LastIndexOf(PATH_SPLIT_CHAR);
				localPath = propertyPath.Remove(0, removeIndex + 1) + PATH_SPLIT_CHAR + localPath;
				propertyPath = propertyPath.Remove(removeIndex, propertyPath.Length - removeIndex);

				if (includeArray)
					return propertyPath;

				removeIndex = propertyPath.LastIndexOf(PATH_SPLIT_CHAR);
				if (removeIndex < 0)
					return "";

				localPath = propertyPath.Remove(0, removeIndex + 1) + localPath;
				propertyPath = propertyPath.Remove(removeIndex, propertyPath.Length - removeIndex);

				return propertyPath;
			}
			else
			{
				localPath = propertyPath;
				return "";
			}
		}

		public static FieldInfo GetAnyField(this Type type, string fieldName)
		{
			var field = type.GetField(fieldName, FIELD_BINDING_FLAGS);
			while (field == null)
			{
				type = type.BaseType;
				field = type.GetField(fieldName, INTERNAL_FIELD_BINDING_FLAGS);
			}

			return field;
		}

		public static MethodInfo GetAnyMethod_WithoutArguments(this Type type, string methodName)
		{
			var methodInfo = type.GetMethod(methodName, METHOD_BINDING_FLAGS, null, new Type[]{}, null);
			while (methodInfo == null)
			{
				type = type.BaseType;
				methodInfo = type.GetMethod(methodName, PRIVATE_METHOD_BINDING_FLAGS, null, new Type[]{}, null);
			}

			return methodInfo;
		}

		public static object InvokeFuncByLocalPath(object source, string methodPath)
		{
			var targetPath = "";
			var methodName = methodPath;

			var removeIndex = methodPath.LastIndexOf(PATH_SPLIT_CHAR);
			if (removeIndex >= 0)
			{
				targetPath = methodPath.Remove(removeIndex, methodPath.Length - removeIndex);
				methodName = methodPath.Remove(0, removeIndex + 1);
			}

			var target = GetObjectByLocalPath(source, targetPath);
			var methodInfo = target.GetType().GetAnyMethod_WithoutArguments(methodName);

			return methodInfo.Invoke(target, null);
		}

		public static void InvokeMethodByLocalPath(object source, string methodPath)
		{
			var targetPath = "";
			var methodName = methodPath;

			var removeIndex = methodPath.LastIndexOf(PATH_SPLIT_CHAR);
			if (removeIndex >= 0)
			{
				targetPath = methodPath.Remove(removeIndex, methodPath.Length - removeIndex);
				methodName = methodPath.Remove(0, removeIndex + 1);
			}

			var target = GetObjectByLocalPath(source, targetPath);
			var methodInfo = target.GetType().GetAnyMethod_WithoutArguments(methodName);

			methodInfo.Invoke(target, null);
		}

		public static string AppendPath(this string sourcePath, string additionalPath)
		{
			if (string.IsNullOrEmpty(sourcePath))
				return additionalPath;
			if (string.IsNullOrEmpty(additionalPath))
				return sourcePath;

			while (additionalPath[0] == PATH_PARENT_CHAR)
			{
				additionalPath = additionalPath.Remove(0, 1);
				sourcePath = GetParentPath(sourcePath);
			}

			return sourcePath + PATH_SPLIT_CHAR + additionalPath;
		}
	}
}