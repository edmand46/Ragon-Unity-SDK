namespace Fusumity.Attributes.Specific
{
	public class DrawOffsetAttribute : FusumityDrawerAttribute
	{
		public bool disableIfHasSubBody;

		public float yOffset;
		public int offsetLines;

		public float xOffset;
		public int indent;
		public int foldoutIndent;
	}
}