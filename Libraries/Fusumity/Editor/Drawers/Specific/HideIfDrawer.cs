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
			var disableIfAttribute = (HideIfAttribute)attribute;

			if (disableIfAttribute.equalsAny == null || disableIfAttribute.equalsAny.Length == 0)
			{
				var boolProperty = property.GetPropertyByLocalPath(disableIfAttribute.checkPath);

				bool isHide;
				if (boolProperty == null)
				{
					isHide = (bool)property.InvokeFuncByLocalPath(disableIfAttribute.checkPath);
				}
				else
				{
					isHide = boolProperty.boolValue;
				}

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

			var checkObject = property.GetPropertyObjectByLocalPath(disableIfAttribute.checkPath);
			foreach (var equalsObject in disableIfAttribute.equalsAny)
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