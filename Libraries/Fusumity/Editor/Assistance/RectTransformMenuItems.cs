using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Assistance
{
	public class RectTransformMenuItems : MonoBehaviour
	{
		[MenuItem("CONTEXT/RectTransform/Rect To Anchors")]
		static void RectToAnchors(MenuCommand command)
		{
			var rectTransform = (RectTransform)command.context;

			if (rectTransform.parent is RectTransform parent)
			{
				Undo.RecordObject(rectTransform, nameof(RectToAnchors));

				var parentRect = parent.rect;
				var parentSize = new Vector2(parentRect.width, parentRect.height);

				var pivot = rectTransform.pivot;
				var anchoredPosition = rectTransform.anchoredPosition / parentSize;
				var sizeDelta = rectTransform.sizeDelta / parentSize;

				rectTransform.anchorMin += anchoredPosition - sizeDelta * pivot;
				rectTransform.anchorMax += sizeDelta * (Vector2.one - pivot) + anchoredPosition;
				rectTransform.sizeDelta = Vector2.zero;
				rectTransform.anchoredPosition = Vector2.zero;
			}
		}
	}
}