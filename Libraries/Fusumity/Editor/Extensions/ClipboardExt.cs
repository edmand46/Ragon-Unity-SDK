using System;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Extensions
{
	public static class ClipboardExt
	{
		private static object _source;

		public static void CopyManagedReferenceValue(this SerializedProperty target)
		{
#if UNITY_2022_3_OR_NEWER
			if (target.managedReferenceValue == null)
			{
				_source = null;
				return;
			}
			_source = Activator.CreateInstance(target.managedReferenceValue.GetType());
			EditorUtility.CopySerializedManagedFieldsOnly(target.managedReferenceValue, _source);
#else
			var type = target.GetManagedReferenceType();
			_source = Activator.CreateInstance(type);
			var sourceObject = target.GetPropertyObjectByLocalPath(target.name);
			EditorUtility.CopySerializedManagedFieldsOnly(sourceObject, _source);
#endif
		}

		public static void PasteManagedReferenceValue(this SerializedProperty target)
		{
			if (_source == null)
			{
				target.managedReferenceValue = null;
				return;
			}
			var value = Activator.CreateInstance(_source.GetType());
			EditorUtility.CopySerializedManagedFieldsOnly(_source, value);
			try
			{
				target.managedReferenceValue = value;
			}
			catch
			{
				Debug.LogWarning("Type is mismatched");
			}
		}
	}
}