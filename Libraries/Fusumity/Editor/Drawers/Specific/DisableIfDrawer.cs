using Fusumity.Attributes.Specific;
using Fusumity.Editor.Extensions;
using UnityEditor;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(DisableIfAttribute))]
	public class DisableIfDrawer : FusumityPropertyDrawer
	{
		public override void ModifyPropertyData()
		{
			base.ModifyPropertyData();

			var property = currentPropertyData.property;
			var disableIfAttribute = (DisableIfAttribute)attribute;

			if (disableIfAttribute.equalsAny == null || disableIfAttribute.equalsAny.Length == 0)
			{
				var boolProperty = property.GetPropertyByLocalPath(disableIfAttribute.checkPath);

				bool isDisabled;
				if (boolProperty == null)
				{
					isDisabled = (bool)property.InvokeFuncByLocalPath(disableIfAttribute.checkPath);
				}
				else
				{
					isDisabled = boolProperty.boolValue;
				}

				if (currentPropertyData.isEnabledChanged)
				{
					currentPropertyData.isEnabled &= !isDisabled;
				}
				else
				{
					currentPropertyData.isEnabled = !isDisabled;
					currentPropertyData.isEnabledChanged = true;
				}
				return;
			}

			var checkObject = property.GetPropertyObjectByLocalPath(disableIfAttribute.checkPath);
			foreach (var equalsObject in disableIfAttribute.equalsAny)
			{
				if ((checkObject == null && equalsObject == null) || (checkObject != null && checkObject.Equals(equalsObject)))
				{
					currentPropertyData.isEnabled = false;
					currentPropertyData.isEnabledChanged = true;
					return;
				}
			}

			if (currentPropertyData.isEnabledChanged)
			{
				currentPropertyData.isEnabled = true;
				currentPropertyData.isEnabledChanged = true;
			}
		}
	}
}