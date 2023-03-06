using System;
using UnityEngine;

namespace Fusumity.Attributes.Specific
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	public class BackgroundColorAttribute : FusumityDrawerAttribute
	{
		public Color color;
		public string conditionPath;
		public string invertConditionPath;

		public BackgroundColorAttribute(float r, float g, float b, float a = 1f, string conditionPath = null, string invertConditionPath = null)
		{
			color = new Color(r, g, b, a);
			this.conditionPath = conditionPath;
			this.invertConditionPath = invertConditionPath;
		}

		public BackgroundColorAttribute(BackgroundColor color, float a = 1f, string conditionPath = null, string invertConditionPath = null)
		{
			this.color = color switch
			{
				BackgroundColor.Red => Color.red,
				BackgroundColor.Green => Color.green,
				BackgroundColor.Blue => Color.blue,
				BackgroundColor.White => Color.white,
				BackgroundColor.Black => Color.black,
				BackgroundColor.Gray => Color.gray,
				BackgroundColor.Magenta => Color.magenta,
				BackgroundColor.Yellow => Color.yellow,
				BackgroundColor.Cyan => Color.cyan,
				BackgroundColor.Clear => Color.clear,
			};
			if (color != BackgroundColor.Clear)
				this.color.a = a;
			this.conditionPath = conditionPath;
			this.invertConditionPath = invertConditionPath;
		}
	}

	public enum BackgroundColor
	{
		Red,
		Green,
		Blue,
		White,
		Black,
		Gray,
		Magenta,
		Yellow,
		Cyan,
		Clear,
	}
}