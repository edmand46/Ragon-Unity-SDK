using System;

namespace Fusumity.Attributes.Specific
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	public class ShowIfAttribute : FusumityDrawerAttribute
	{
		public string checkPath;
		public object[] equalsAny;

		public ShowIfAttribute(string checkPath, params object[] equalsAny)
		{
			equalsAny ??= new object[] { null };

			this.checkPath = checkPath;
			this.equalsAny = equalsAny;
		}
	}
}