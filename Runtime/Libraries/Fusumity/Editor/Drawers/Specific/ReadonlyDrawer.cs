using Fusumity.Attributes.Specific;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(ReadonlyAttribute))]
	public class ReadonlyDrawer : FusumityPropertyDrawer
	{
		public override void ModifyPropertyData()
		{
			base.ModifyPropertyData();

			var readonlyAttribute = (ReadonlyAttribute)attribute;
			if (readonlyAttribute.ifApplicationIsPlaying && !Application.isPlaying)
				return;
			if (readonlyAttribute.ifApplicationIsNotPlaying && Application.isPlaying)
				return;
			currentPropertyData.isEnabled = false;
		}
	}
}