using System.IO;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Extensions
{
	public static class AssetsEditorExt
	{
		public static string GetAssetFolder<T>(this T asset) where T: Object
		{
			var assetPath = AssetDatabase.GetAssetPath(asset);
			var folderPath = Path.GetDirectoryName(assetPath);
			return folderPath;
		}
	}
}