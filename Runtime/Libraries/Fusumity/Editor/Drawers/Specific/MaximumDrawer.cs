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
			var intExclusively = maxAttribute.intExclusively;

			var intMax = intExclusively && maxAttribute.maxInt > int.MinValue ? maxAttribute.maxInt - 1 : maxAttribute.maxInt;
			var floatMax = maxAttribute.maxFloat;

			if (!string.IsNullOrEmpty(maxAttribute.maxPath))
			{
				var maxValue = property.GetResultByLocalPath(maxAttribute.maxPath);
				switch (maxValue)
				{
					case int intValue:
						if (intExclusively && intValue > int.MinValue)
							intValue--;
						intMax = Math.Min(intValue, intMax);
						floatMax = Math.Min((float)intValue, floatMax);
						break;
					case float floatValue:
						intMax = Math.Min((int)floatValue, intMax);
						floatMax = Math.Min(floatValue, floatMax);
						break;
				}
			}

			switch (currentPropertyData.property.propertyType)
			{
				case SerializedPropertyType.Integer:
					if (currentPropertyData.property.intValue > intMax)
					{
						currentPropertyData.property.intValue = intMax;
					}
					break;
				case SerializedPropertyType.Float:
					if (currentPropertyData.property.floatValue > floatMax)
					{
						currentPropertyData.property.floatValue = floatMax;
					}
					break;
				case SerializedPropertyType.Vector2:
					var vector = currentPropertyData.property.vector2Value;
					if (vector.x > floatMax)
					{
						vector.x = floatMax;
						currentPropertyData.property.vector2Value = vector;
					}
					if (vector.y > floatMax)
					{
						vector.y = floatMax;
						currentPropertyData.property.vector2Value = vector;
					}
					break;
			}
		}
	}
}