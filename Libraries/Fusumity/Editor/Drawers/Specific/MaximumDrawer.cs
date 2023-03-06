using System;
using Fusumity.Attributes.Specific;
using Fusumity.Editor.Extensions;
using UnityEditor;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(MaximumAttribute))]
	public class MaximumDrawer : FusumityPropertyDrawer
	{
		public override void ValidateBeforeDrawing()
		{
			base.ValidateBeforeDrawing();

			var property = currentPropertyData.property;
			var maxAttribute = (MaximumAttribute)attribute;

			var intmax = maxAttribute.maxInt;
			var floatmax = maxAttribute.maxFloat;

			if (!string.IsNullOrEmpty(maxAttribute.maxPath))
			{
				var maxProperty = property.GetPropertyByLocalPath(maxAttribute.maxPath);

				switch (maxProperty?.propertyType)
				{
					case SerializedPropertyType.Integer:
						intmax = Math.Max(maxProperty.intValue, intmax);
						floatmax = Math.Max((float)maxProperty.intValue, floatmax);
						break;
					case SerializedPropertyType.Float:
					case SerializedPropertyType.Vector2:
						intmax = Math.Max((int)maxProperty.floatValue, intmax);
						floatmax = Math.Max(maxProperty.floatValue, floatmax);
						break;
				}
			}

			switch (currentPropertyData.property.propertyType)
			{
				case SerializedPropertyType.Integer:
					if (currentPropertyData.property.intValue >intmax)
					{
						currentPropertyData.property.intValue = intmax;
					}
					break;
				case SerializedPropertyType.Float:
					if (currentPropertyData.property.floatValue > floatmax)
					{
						currentPropertyData.property.floatValue = floatmax;
					}
					break;
				case SerializedPropertyType.Vector2:
					var vector = currentPropertyData.property.vector2Value;
					if (vector.x > floatmax)
					{
						vector.x = floatmax;
						currentPropertyData.property.vector2Value = vector;
					}
					if (vector.y > floatmax)
					{
						vector.y = floatmax;
						currentPropertyData.property.vector2Value = vector;
					}
					break;
			}
		}
	}
}