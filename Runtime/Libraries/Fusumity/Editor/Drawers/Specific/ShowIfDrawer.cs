using Fusumity.Attributes.Specific;
using Fusumity.Editor.Extensions;
using UnityEditor;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(ShowIfAttribute))]
	public class ShowIfDrawer : FusumityPropertyDrawer
	{
		public override void ModifyPropertyData()
		{
			base.ModifyPropertyData();

			var property = currentPropertyData.property;
			var showIfAttribute = (ShowIfAttribute)attribute;

			if (showIfAttribute.equalsAny == null || showIfAttribute.equalsAny.Length == 0)
			{
				var isShow = property.GetResultByLocalPath<bool>(showIfAttribute.checkPath);

				if (currentPropertyData.drawPropertyChanged)
				{
					currentPropertyData.drawProperty &= isShow;
				}
				else
				{
					currentPropertyData.drawProperty = isShow;
					currentPropertyData.drawPropertyChanged = true;
				}
				return;
			}

			var checkObject = property.GetPropertyObjectByLocalPath(showIfAttribute.checkPath);
			foreach (var equalsObject in showIfAttribute.equalsAny)
			{
				if ((checkObject == null && equalsObject == null) || (checkObject != null && checkObject.Equals(equalsObject)))
				{
					if (!currentPropertyData.drawPropertyChanged)
					{
						currentPropertyData.drawProperty = true;
					}
					currentPropertyData.drawPropertyChanged = true;
					return;
				}
			}

			currentPropertyData.drawProperty = false;
			currentPropertyData.drawPropertyChanged = true;
		}
	}
}