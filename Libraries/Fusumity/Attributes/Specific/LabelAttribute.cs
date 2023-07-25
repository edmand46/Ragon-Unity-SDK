namespace Fusumity.Attributes.Specific
{
	public class LabelAttribute : FusumityDrawerAttribute
	{
		public string label;
		public bool useParentLabel;

		public LabelAttribute(string label)
		{
			this.label = label;
		}

		public LabelAttribute(bool useParentLabel)
		{
			this.useParentLabel = useParentLabel;
			this.label = null;
		}
	}
}