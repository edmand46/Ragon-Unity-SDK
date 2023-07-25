using System;

namespace Fusumity.Attributes.Specific
{
	public class AssetSelectorAttribute : FusumityDrawerAttribute
	{
		public Type type;

		public AssetSelectorAttribute(Type type = null)
		{
			this.type = type;
		}
	}
}