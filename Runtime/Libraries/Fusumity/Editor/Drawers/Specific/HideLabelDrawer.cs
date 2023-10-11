using Fusumity.Attributes.Specific;
using UnityEditor;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(HideLabelAttribute))]
	public class HideLabelDrawer : FusumityPropertyDrawer
	{
		public override void ModifyPropertyData()
		{
			base.ModifyPropertyData();

			var labelAttribute = (HideLabelAttribute)attribute;

			currentPropertyData.hasLabel = false;
			currentPropertyData.hasFoldout = labelAttribute.hasFoldout;
		}
	}
}