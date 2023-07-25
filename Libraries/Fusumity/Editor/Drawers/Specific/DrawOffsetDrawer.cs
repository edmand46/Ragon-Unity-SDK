using Fusumity.Attributes.Specific;
using UnityEditor;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(DrawOffsetAttribute))]
	public class DrawOffsetDrawer : FusumityPropertyDrawer
	{
		public override void ModifyPropertyData()
		{
			base.ModifyPropertyData();

			var drawOffsetAttribute = (DrawOffsetAttribute)attribute;

			if (drawOffsetAttribute.disableIfHasSubBody & currentPropertyData.hasSubBody)
				return;

			currentPropertyData.drawOffsetY += drawOffsetAttribute.yOffset;
			currentPropertyData.drawOffsetY += drawOffsetAttribute.offsetLines * EditorGUIUtility.singleLineHeight;

			currentPropertyData.drawOffsetX += drawOffsetAttribute.xOffset;
			currentPropertyData.indent += drawOffsetAttribute.indent;
			currentPropertyData.foldoutIndent += drawOffsetAttribute.foldoutIndent;
		}
	}
}