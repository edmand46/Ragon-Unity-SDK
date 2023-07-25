using Fusumity.Attributes.Specific;
using Fusumity.Editor.Extensions;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(CopyPasteAttribute))]
	public class CopyPasteAttributeDrawer : FusumityPropertyDrawer
	{
		private const float COPY_PASTE_BUTTONS_WIDTH = 50f;

		public override void DrawLabel(Rect position)
		{
			var property = currentPropertyData.property;

			var propertyPosition = position;
			propertyPosition.xMax -= COPY_PASTE_BUTTONS_WIDTH;

			base.DrawLabel(propertyPosition);

			var copyPosition = position;
			copyPosition.xMin = copyPosition.xMax - COPY_PASTE_BUTTONS_WIDTH;
			copyPosition.xMax = copyPosition.xMax - COPY_PASTE_BUTTONS_WIDTH / 2;
			if (GUI.Button(copyPosition, "C"))
			{
				property.CopyManagedReferenceValue();
			}

			var pastPosition = position;
			pastPosition.xMin = copyPosition.xMax;
			if (GUI.Button(pastPosition, "P"))
			{
				property.PasteManagedReferenceValue();
			}
		}
	}
}