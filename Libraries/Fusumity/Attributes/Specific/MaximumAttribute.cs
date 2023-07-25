namespace Fusumity.Attributes.Specific
{
	public class MaximumAttribute : FusumityDrawerAttribute
	{
		public float maxFloat;
		public int maxInt;
		public string maxPath;
		public bool intExclusively;

		public MaximumAttribute(float max, string path = null)
		{
			maxFloat = max;
			maxInt = (int)max;
			maxPath = path;
		}

		public MaximumAttribute(int max, string path = null)
		{
			maxFloat = max;
			maxInt = max;
			maxPath = path;
		}

		public MaximumAttribute(string path)
		{
			maxFloat = float.PositiveInfinity;
			maxInt = int.MaxValue;
			maxPath = path;
		}
	}
}