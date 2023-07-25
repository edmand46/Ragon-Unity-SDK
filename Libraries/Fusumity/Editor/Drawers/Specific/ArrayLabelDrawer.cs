using Fusumity.Attributes.Specific;
using UnityEditor;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(ArrayLabelAttribute))]
	public class ArrayLabelDrawer : FusumityPropertyDrawer
	{
		public override void ModifyPropertyData()
		{
			base.ModifyPropertyData();

			var labelAttribute = (ArrayLabelAttribute)attribute;

			currentPropertyData.drawOffsetX = -10;

			var elementIndex = currentPropertyData.label.text.Replace("Element ", string.Empty);
			if (labelAttribute.indexOffset != 0)
				elementIndex = (int.Parse(elementIndex) + labelAttribute.indexOffset).ToString();
			currentPropertyData.labelPrefix = elementIndex;

			currentPropertyData.labelPrefixWidth = currentPropertyData.labelPrefix.Length * 7f + 15f;

			currentPropertyData.hasLabel = false;
			currentPropertyData.hasFoldout = labelAttribute.hasFoldout;
		}
	}
}