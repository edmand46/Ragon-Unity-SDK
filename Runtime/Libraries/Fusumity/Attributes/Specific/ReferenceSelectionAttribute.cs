using System;

namespace Fusumity.Attributes.Specific
{
	public class ReferenceSelectionAttribute : FusumityDrawerAttribute
	{
		public Type type;
		public bool insertNull = true;

		public ReferenceSelectionAttribute(Type type = null)
		{
			this.type = type;
		}
	}
}
