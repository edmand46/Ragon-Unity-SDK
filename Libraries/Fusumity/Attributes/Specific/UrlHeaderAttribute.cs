using UnityEngine;

namespace Fusumity.Attributes.Specific
{
	public class UrlHeaderAttribute : FusumityDrawerAttribute
	{
		public string label;
		public string url;
		public Color color = new Color(0.27f,0.77f, 0.84f, 1);

		public UrlHeaderAttribute(string label, string url)
		{
			this.label = label;
			this.url = url;
		}

		public UrlHeaderAttribute(string url)
		{
			this.label = url;
			this.url = url;
		}
	}
}