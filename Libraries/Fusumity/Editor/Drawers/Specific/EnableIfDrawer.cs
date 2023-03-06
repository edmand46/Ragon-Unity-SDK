using Fusumity.Attributes.Specific;
using Fusumity.Editor.Extensions;
using UnityEditor;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(EnableIfAttribute))]
	public class EnableIfDrawer : FusumityPropertyDrawer
	{
		public override void ModifyPropertyData()
		{
			base.ModifyPropertyData();

			var property = currentPropertyData.property;
			var enableIfAttribute = (EnableIfAttribute)attribute;

			if (enableIfAttribute.equalsAny == null || enableIfAttribute.equalsAny.Length == 0)
			{
				var boolProperty = property.GetPropertyByLocalPath(enableIfAttribute.checkPath);

				bool isEnabled;
				if (boolProperty == null)
				{
					isEnabled = (bool)property.InvokeFuncByLocalPath(enableIfAttribute.checkPath);
				}
				else
				{
					isEnabled = boolProperty.boolValue;
				}

				if (currentPropertyData.isEnabledChanged)
				{
					currentPropertyData.isEnabled &= isEnabled;
				}
				else
				{
					currentPropertyData.isEnabled = isEnabled;
					currentPropertyData.isEnabledChanged = true;
				}
				return;
			}

			var checkObject = property.GetPropertyObjectByLocalPath(enableIfAttribute.checkPath);
			foreach (var equalsObject in enableIfAttribute.equalsAny)
			{
				if ((checkObject == null && equalsObject == null) || (checkObject != null && checkObject.Equals(equalsObject)))
				{
					if (!currentPropertyData.drawPropertyChanged)
					{
						currentPropertyData.drawProperty = true;
					}
					currentPropertyData.isEnabledChanged = true;
					return;
				}
			}

			currentPropertyData.isEnabled = false;
			currentPropertyData.isEnabledChanged = true;
		}
	}
}