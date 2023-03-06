using Fusumity.Attributes.Specific;
using UnityEditor;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(RemoveFoldoutAttribute))]
	public class RemoveFoldoutDrawer : FusumityPropertyDrawer
	{
		public override void ModifyPropertyData()
		{
			base.ModifyPropertyData();

			currentPropertyData.property.isExpanded = true;
			currentPropertyData.hasFoldout = false;
		}
	}
}
