#if UNITY_EDITOR
using Fusumity.Collections;
using Fusumity.Editor.Extensions;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Drawers.Collections
{
	[CustomPropertyDrawer(typeof(IEnumArray), true)]
	public class EnumArrayDrawer : FusumityPropertyDrawer
	{
		public override void ModifyPropertyData()
		{
			base.ModifyPropertyData();

			currentPropertyData.hasLabel = false;
			currentPropertyData.hasFoldout = false;
		}

		public override void DrawBody(Rect position)
		{
			var valuesProperty = currentPropertyData.property.FindPropertyRelative("values");
			valuesProperty.PropertyField(position, currentPropertyData.label, true);
		}
	}
}
#endif