using System;

namespace Fusumity.Attributes.Specific
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	public class HideIfAttribute : FusumityDrawerAttribute
	{
		public string checkPath;
		public object[] equalsAny;

		public HideIfAttribute(string checkPath, params object[] equalsAny)
		{
			equalsAny ??= new object[] { null };

			this.checkPath = checkPath;
			this.equalsAny = equalsAny;
		}
	}
}