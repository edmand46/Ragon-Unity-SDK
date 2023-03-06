using System;
using Fusumity.Attributes.Specific;
using Fusumity.Editor.Extensions;
using UnityEditor;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(MinimumAttribute))]
	public class MinimumDrawer : FusumityPropertyDrawer
	{
		public override void ValidateBeforeDrawing()
		{
			base.ValidateBeforeDrawing();

			var property = currentPropertyData.property;
			var minAttribute = (MinimumAttribute)attribute;

			var intMin = minAttribute.minInt;
			var floatMin = minAttribute.minFloat;

			if (!string.IsNullOrEmpty(minAttribute.minPath))
			{
				var minProperty = property.GetPropertyByLocalPath(minAttribute.minPath);

				switch (minProperty?.propertyType)
				{
					case SerializedPropertyType.Integer:
						intMin = Math.Max(minProperty.intValue, intMin);
						floatMin = Math.Max((float)minProperty.intValue, floatMin);
						break;
					case SerializedPropertyType.Float:
					case SerializedPropertyType.Vector2:
						intMin = Math.Max((int)minProperty.floatValue, intMin);
						floatMin = Math.Max(minProperty.floatValue, floatMin);
						break;
				}
			}

			switch (currentPropertyData.property.propertyType)
			{
				case SerializedPropertyType.Integer:
					if (currentPropertyData.property.intValue < intMin)
					{
						currentPropertyData.property.intValue = intMin;
					}
					break;
				case SerializedPropertyType.Float:
					if (currentPropertyData.property.floatValue < floatMin)
					{
						currentPropertyData.property.floatValue = floatMin;
					}
					break;
				case SerializedPropertyType.Vector2:
					var vector = currentPropertyData.property.vector2Value;
					if (vector.x < floatMin)
					{
						vector.x = floatMin;
						currentPropertyData.property.vector2Value = vector;
					}
					if (vector.y < floatMin)
					{
						vector.y = floatMin;
						currentPropertyData.property.vector2Value = vector;
					}
					break;
			}
		}
	}
}