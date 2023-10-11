using Fusumity.Attributes.Specific;
using Fusumity.Editor.Extensions;
using UnityEditor;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(HideIfAttribute))]
	public class HideIfDrawer : FusumityPropertyDrawer
	{
		public override void ModifyPropertyData()
		{
			base.ModifyPropertyData();

			var property = currentPropertyData.property;
			var hideIfAttribute = (HideIfAttribute)attribute;

			if (hideIfAttribute.equalsAny == null || hideIfAttribute.equalsAny.Length == 0)
			{
				var isHide = property.GetResultByLocalPath<bool>(hideIfAttribute.checkPath);

				if (currentPropertyData.drawPropertyChanged)
				{
					currentPropertyData.drawProperty &= !isHide;
				}
				else
				{
					currentPropertyData.drawProperty = !isHide;
					currentPropertyData.drawPropertyChanged = true;
				}
				return;
			}

			var checkObject = property.GetPropertyObjectByLocalPath(hideIfAttribute.checkPath);
			foreach (var equalsObject in hideIfAttribute.equalsAny)
			{
				if ((checkObject == null && equalsObject == null) || (checkObject != null && checkObject.Equals(equalsObject)))
				{
					currentPropertyData.drawProperty = false;
					currentPropertyData.drawPropertyChanged = true;
					return;
				}
			}

			if (currentPropertyData.drawPropertyChanged)
			{
				currentPropertyData.drawProperty = true;
				currentPropertyData.drawPropertyChanged = true;
			}
		}
	}
}