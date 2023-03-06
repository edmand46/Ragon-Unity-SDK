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
			
			currentPropertyData.hasBody = false;
			currentPropertyData.hasSubBody = false;
			// currentPropertyData.hasLabel = false;
			currentPropertyData.hasFoldout = false;
		}
	}
}