using Fusumity.Attributes.Specific;
using Fusumity.Editor.Extensions;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(UrlHeaderAttribute))]
	public class UrlHeaderDrawer : FusumityPropertyDrawer
	{
		public override void ModifyPropertyData()
		{
			base.ModifyPropertyData();

			currentPropertyData.hasBeforeExtension = true;
			currentPropertyData.beforeExtensionHeight += EditorGUIUtility.singleLineHeight;
		}

		public override void DrawBeforeExtension(ref Rect position)
		{
			base.DrawBeforeExtension(ref position);

			var enabled = GUI.enabled;
			GUI.enabled = true;

			var urlHeaderAttribute = (UrlHeaderAttribute)attribute;

			if (Event.current.type == EventType.MouseUp && position.Contains(Event.current.mousePosition))
				Application.OpenURL(urlHeaderAttribute.url);

			var color = GUI.color;
			GUI.color = urlHeaderAttribute.color;

			GUI.Label(position, $"URL: {urlHeaderAttribute.label}");

			GUI.color = color;

			GUI.enabled = enabled;
		}

		private string CreateUnderline(string label)
		{
			return new string('_', (int)(label.Length * 1.75f));
		}
	}
}