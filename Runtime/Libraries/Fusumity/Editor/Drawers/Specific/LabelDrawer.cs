using Fusumity.Attributes.Specific;
using Fusumity.Editor.Extensions;
using UnityEditor;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(LabelAttribute))]
	public class LabelDrawer : FusumityPropertyDrawer
	{
		public override void ModifyPropertyData()
		{
			base.ModifyPropertyData();

			var labelAttribute = (LabelAttribute)attribute;
			if (labelAttribute.useParentLabel)
			{
				currentPropertyData.label.text = currentPropertyData.property.GetParentProperty().displayName;
				return;
			}

			currentPropertyData.label.text = labelAttribute.label;
		}
	}
}