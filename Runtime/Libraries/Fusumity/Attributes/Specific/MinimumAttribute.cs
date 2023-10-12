namespace Fusumity.Attributes.Specific
{
	public class MinimumAttribute : FusumityDrawerAttribute
	{
		public float minFloat;
		public int minInt;
		public string minPath;

		public MinimumAttribute(float min, string path = null)
		{
			minFloat = min;
			minInt = (int)min;
			minPath = path;
		}

		public MinimumAttribute(int min, string path = null)
		{
			minFloat = min;
			minInt = min;
			minPath = path;
		}

		public MinimumAttribute(string path)
		{
			minFloat = float.NegativeInfinity;
			minInt = int.MinValue;
			minPath = path;
		}
	}
}
