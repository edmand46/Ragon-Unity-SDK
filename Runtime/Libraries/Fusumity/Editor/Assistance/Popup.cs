using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Assistance
{
	// MIT License
	//
	// Copyright (c) 2020 Alex Feer
	//
	// Permission is hereby granted, free of charge, to any person obtaining a copy
	// of this software and associated documentation files (the "Software"), to deal
	// in the Software without restriction, including without limitation the rights
	// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	// copies of the Software, and to permit persons to whom the Software is
	// furnished to do so, subject to the following conditions:
	//
	// The above copyright notice and this permission notice shall be included in all
	// copies or substantial portions of the Software.
	//
	// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
	// SOFTWARE.

	public class PopupWindowAnim : EditorWindow
	{
		private const float DEFAULT_WIDTH = 150f;
		private const float DEFAULT_HEIGHT = 250f;
		private const float ELEMENT_HEIGHT = 20f;

		private static Styles _styles;

		private static bool _sDirtyList = true;

		public Rect screenRect;

		public bool autoHeight;
		public bool autoClose;

		public char separator = '/';

		public bool useSearch = true;
		public string searchText = "";
		private bool HasSearch => useSearch && !string.IsNullOrEmpty(searchText);

		private float _anim;
		private int _animTarget = 1;
		private long _lastTime;

		private List<PopupItem> _submenu = new List<PopupItem>();

		private readonly List<string> _folderStack = new List<string>();

		private Element[] _tree;
		private Element[] _treeSearch;

		private readonly List<GroupElement> _stack = new List<GroupElement>();

		private bool _scrollToSelected;

		private Element[] ActiveTree => (!HasSearch ? _tree : _treeSearch);

		private GroupElement ActiveParent => _stack[(_stack.Count - 2) + _animTarget];

		private int _maxElementCount = 1;

		private Element ActiveElement
		{
			get
			{
				if (ActiveTree == null)
					return null;
				var children = GetChildren(ActiveTree, ActiveParent);
				return children.Count == 0 ? null : children[ActiveParent.selectedIndex];
			}
		}

		public static PopupWindowAnim Create(Rect screenRect, bool useSearch = true)
		{
			var popup = CreateInstance<PopupWindowAnim>();
			popup.screenRect = screenRect;
			popup.useSearch = useSearch;
			return popup;
		}

		public static PopupWindowAnim Create(float width, float height, bool useSearch = true)
		{
			var screenPos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
			return Create(new Rect(screenPos.x, screenPos.y, width, height), useSearch);
		}

		public static PopupWindowAnim Create(float width, bool useSearch = true)
		{
			return Create(width, DEFAULT_HEIGHT, useSearch);
		}

		public static PopupWindowAnim Create(bool useSearch = true)
		{
			return Create(DEFAULT_WIDTH, DEFAULT_HEIGHT, useSearch);
		}

		public new void Show()
		{
			if (_submenu.Count == 0)
				DestroyImmediate(this);
			else
				Init();
		}

		private void SetHeightByElementCount(int elementCount)
		{
			screenRect.height = elementCount * ELEMENT_HEIGHT + (useSearch ? 30f : 0f) + 26f;
		}

		private void SetHeightByElementCount()
		{
			SetHeightByElementCount(_maxElementCount);
		}

		public void Item(string itemTitle, Texture2D image, Action action, int order)
		{
			var folder = "";
			if (_folderStack.Count > 0)
				folder = _folderStack[_folderStack.Count - 1] ?? "";
			_submenu.Add(string.IsNullOrEmpty(folder)
				? new PopupItem(titleContent.text + separator + itemTitle, action) { image = image, order = order }
				: new PopupItem(titleContent.text + separator + folder + separator + itemTitle, action)
					{ image = image, order = order });
		}

		public void Item(string itemTitle, Texture2D image, Action<PopupItem> action, bool searchable, int order)
		{
			var folder = "";
			if (_folderStack.Count > 0)
				folder = _folderStack[_folderStack.Count - 1] ?? "";
			_submenu.Add(string.IsNullOrEmpty(folder)
				? new PopupItem(titleContent.text + separator + itemTitle, action)
					{ image = image, searchable = searchable, order = order }
				: new PopupItem(titleContent.text + separator + folder + separator + itemTitle, action)
					{ image = image, searchable = searchable, order = order });
		}

		private void Init()
		{
			CreateElementTree();

			if (autoHeight)
				SetHeightByElementCount();
			ShowAsDropDown(new Rect(screenRect.x, screenRect.y, 1, 1),
				new Vector2(screenRect.width, screenRect.height));
			Focus();
			wantsMouseMove = true;
		}

		private void CreateElementTree()
		{
			var list = new List<string>();
			var elements = new List<Element>();

			_submenu = _submenu.OrderBy(x => x.order).ThenBy(x => x.path).ToList();

			foreach (var submenuItem in _submenu)
			{
				var menuPath = submenuItem.path;
				var separators = new[] { separator };
				var pathParts = menuPath.Split(separators);

				while (pathParts.Length - 1 < list.Count)
				{
					list.RemoveAt(list.Count - 1);
				}

				while (list.Count > 0 && pathParts[list.Count - 1] != list[list.Count - 1])
				{
					list.RemoveAt(list.Count - 1);
				}

				while (pathParts.Length - 1 > list.Count)
				{
					elements.Add(new GroupElement(list.Count, pathParts[list.Count]));
					list.Add(pathParts[list.Count]);
				}

				elements.Add(new CallElement(list.Count, pathParts[pathParts.Length - 1], submenuItem));
			}

			_tree = elements.ToArray();
			foreach (var element in _tree)
			{
				var elChildren = GetChildren(_tree, element);
				if (elChildren.Count > _maxElementCount)
					_maxElementCount = elChildren.Count;
			}

			if (_stack.Count == 0)
			{
				_stack.Add(_tree[0] as GroupElement);
				goto to_research;
			}

			var parent = _tree[0] as GroupElement;
			var level = 0;
			to_startCycle:
			var stackElement = _stack[level];
			_stack[level] = parent;
			if (_stack[level] != null)
			{
				_stack[level].selectedIndex = stackElement.selectedIndex;
				_stack[level].scroll = stackElement.scroll;
			}

			level++;
			if (level != _stack.Count)
			{
				var children = GetChildren(ActiveTree, parent);
				var child = children.FirstOrDefault(x => _stack[level].Name == x.Name);
				if (child is GroupElement element)
				{
					parent = element;
				}
				else
				{
					while (_stack.Count > level)
					{
						_stack.RemoveAt(level);
					}
				}
				goto to_startCycle;
			}

			to_research:
			_sDirtyList = false;
			RebuildSearch();
		}

		private void RebuildSearch()
		{
			if (!HasSearch)
			{
				_treeSearch = null;
				if (_stack[_stack.Count - 1].Name == "Search")
				{
					_stack.Clear();
					_stack.Add(_tree[0] as GroupElement);
				}

				_animTarget = 1;
				_lastTime = DateTime.Now.Ticks;
			}
			else
			{
				var separatorSearch = new[] { ' ', separator };
				var searchLowerWords = searchText.ToLower().Split(separatorSearch);
				var firstElements = new List<Element>();
				var otherElements = new List<Element>();
				foreach (var element in _tree)
				{
					if (!(element is CallElement))
						continue;
					if (element.searchable == false) continue;
					var elementNameShortLower = element.Name.ToLower().Replace(" ", string.Empty);
					var itsSearchableItem = true;
					var firstContainsFlag = false;
					for (int i = 0; i < searchLowerWords.Length; i++)
					{
						var searchLowerWord = searchLowerWords[i];
						if (elementNameShortLower.Contains(searchLowerWord))
						{
							if (i == 0 && elementNameShortLower.StartsWith(searchLowerWord))
								firstContainsFlag = true;
						}
						else
						{
							itsSearchableItem = false;
							break;
						}
					}

					if (itsSearchableItem)
					{
						if (firstContainsFlag)
							firstElements.Add(element);
						else
							otherElements.Add(element);
					}
				}

				firstElements.Sort();
				otherElements.Sort();

				var searchElements = new List<Element>
					{ new GroupElement(0, "Search") };
				searchElements.AddRange(firstElements);
				searchElements.AddRange(otherElements);

				_treeSearch = searchElements.ToArray();
				_stack.Clear();
				_stack.Add(_treeSearch[0] as GroupElement);
				if (GetChildren(ActiveTree, ActiveParent).Count >= 1)
					ActiveParent.selectedIndex = 0;
				else
					ActiveParent.selectedIndex = -1;
			}
		}

		public void OnGUI()
		{
			if (_tree == null)
			{
				Close();
				return;
			}

			if (_styles == null)
				_styles = new Styles();

			if (_sDirtyList)
				CreateElementTree();
			HandleKeyboard();
			GUI.Label(new Rect(0, 0, position.width, position.height), GUIContent.none, _styles.background);

			if (useSearch)
			{
				GUILayout.Space(7f);
				var rectSearch = GUILayoutUtility.GetRect(10f, 20f);
				rectSearch.x += 8f;
				rectSearch.width -= 16f;
				EditorGUI.FocusTextInControl("ComponentSearch");
				GUI.SetNextControlName("ComponentSearch");
				if (SearchField(rectSearch, ref searchText))
					RebuildSearch();
			}

			ListGUI(ActiveTree, _anim, GetElementRelative(0), GetElementRelative(-1));
			if (_anim < 1f && _stack.Count > 1)
			{
				ListGUI(ActiveTree, _anim + 1f, GetElementRelative(-1), GetElementRelative(-2));
			}
			if (_anim != _animTarget && Event.current.type == EventType.Repaint)
			{
				var ticks = DateTime.Now.Ticks;
				var coefficient = (ticks - _lastTime) / 1E+07f;
				_lastTime = ticks;
				_anim = Mathf.MoveTowards(_anim, _animTarget, coefficient * 4f);
				if (_animTarget == 0 && _anim == 0f)
				{
					_anim = 1f;
					_animTarget = 1;
					_stack.RemoveAt(_stack.Count - 1);
				}

				Repaint();
			}
		}

		private void HandleKeyboard()
		{
			var current = Event.current;
			if (current.type != EventType.KeyDown) return;

			if (current.keyCode == KeyCode.DownArrow)
			{
				ActiveParent.selectedIndex++;
				ActiveParent.selectedIndex = Mathf.Min(ActiveParent.selectedIndex, GetChildren(ActiveTree, ActiveParent).Count - 1);
				_scrollToSelected = true;
				current.Use();
			}
			if (current.keyCode == KeyCode.UpArrow)
			{
				var element = ActiveParent;
				element.selectedIndex--;
				ActiveParent.selectedIndex = Mathf.Max(ActiveParent.selectedIndex, 0);
				_scrollToSelected = true;
				current.Use();
			}
			if (current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter)
			{
				GoToChild(ActiveElement, true);
				current.Use();
			}

			if (HasSearch) return;

			if (current.keyCode == KeyCode.LeftArrow || current.keyCode == KeyCode.Backspace)
			{
				GoToParent();
				current.Use();
			}
			if (current.keyCode == KeyCode.RightArrow)
			{
				GoToChild(ActiveElement, false);
				current.Use();
			}
			if (current.keyCode == KeyCode.Escape)
			{
				Close();
				current.Use();
			}
		}

		private static bool SearchField(Rect position, ref string text)
		{
			var rectField = position;
			rectField.width -= 15f;
			var startText = text;
			text = GUI.TextField(rectField, startText ?? "", _styles.searchTextField);

			var rectCancel = position;
			rectCancel.x += position.width - 15f;
			rectCancel.width = 15f;
			var styleCancel = text == "" ? _styles.searchCancelButtonEmpty : _styles.searchCancelButton;
			if (GUI.Button(rectCancel, GUIContent.none, styleCancel) && text != "")
			{
				text = "";
				GUIUtility.keyboardControl = 0;
			}

			return startText != text;
		}

		private void ListGUI(Element[] tree, float anim, GroupElement parent, GroupElement grandParent)
		{
			anim = Mathf.Floor(anim) + Mathf.SmoothStep(0f, 1f, Mathf.Repeat(anim, 1f));
			var rectArea = position;
			rectArea.x = position.width * (1f - anim) + 1f;
			rectArea.y = useSearch ? 30f : 0;
			rectArea.height -= useSearch ? 30f : 0;
			rectArea.width -= 2f;
			GUILayout.BeginArea(rectArea);
			{
				var rectHeader = GUILayoutUtility.GetRect(10f, 25f);
				var nameHeader = parent.Name;
				GUI.Label(rectHeader, nameHeader, _styles.header);
				if (grandParent != null)
				{
					var rectHeaderBackArrow = new Rect(rectHeader.x + 4f, rectHeader.y + 7f, 13f, 13f);
					if (Event.current.type == EventType.Repaint)
					{
						_styles.leftArrow.Draw(rectHeaderBackArrow, false, false, false, false);
					}
					if (Event.current.type == EventType.MouseDown && rectHeader.Contains(Event.current.mousePosition))
					{
						GoToParent();
						Event.current.Use();
					}
				}

				ListGUI(tree, parent);
			}
			GUILayout.EndArea();
		}

		private void ListGUI(Element[] tree, GroupElement parent)
		{
			parent.scroll = GUILayout.BeginScrollView(parent.scroll);
			EditorGUIUtility.SetIconSize(new Vector2(16f, 16f));
			var children = GetChildren(tree, parent);
			var rect = new Rect();

			for (var i = 0; i < children.Count; i++)
			{
				var element = children[i];
				var options = new[] { GUILayout.ExpandWidth(true) };
				var rectElement = GUILayoutUtility.GetRect(16f, ELEMENT_HEIGHT, options);
				if ((Event.current.type == EventType.MouseMove || Event.current.type == EventType.MouseDown)
				    && parent.selectedIndex != i && rectElement.Contains(Event.current.mousePosition))
				{
					parent.selectedIndex = i;
					Repaint();
				}

				var on = false;
				if (i == parent.selectedIndex)
				{
					on = true;
					rect = rectElement;
				}

				if (Event.current.type == EventType.Repaint)
				{
					var item = element.content.image != null ? _styles.componentItem : _styles.groupItem;
					item.Draw(rectElement, element.content, false, false, on, on);

					if (!(element is CallElement))
					{
						var rectElementForwardArrow = new Rect(rectElement.x + rectElement.width - 13f,rectElement.y + 4f, 13f, 13f);
						_styles.rightArrow.Draw(rectElementForwardArrow, false, false, false, false);
					}
				}

				if (Event.current.type == EventType.MouseDown && rectElement.Contains(Event.current.mousePosition))
				{
					Event.current.Use();
					parent.selectedIndex = i;
					GoToChild(element, true);
				}
			}

			EditorGUIUtility.SetIconSize(Vector2.zero);
			GUILayout.EndScrollView();
			if (_scrollToSelected && Event.current.type == EventType.Repaint)
			{
				_scrollToSelected = false;
				var lastRect = GUILayoutUtility.GetLastRect();
				if ((rect.yMax - lastRect.height) > parent.scroll.y)
				{
					parent.scroll.y = rect.yMax - lastRect.height;
					Repaint();
				}

				if (rect.y < parent.scroll.y)
				{
					parent.scroll.y = rect.y;
					Repaint();
				}
			}
		}

		private void GoToParent()
		{
			if (_stack.Count <= 1)
				return;

			_animTarget = 0;
			_lastTime = DateTime.Now.Ticks;
		}

		private void GoToChild(Element element, bool addIfComponent)
		{
			if (element is CallElement callElement)
			{
				if (!addIfComponent)
					return;
				callElement.action();
				if (autoClose)
				{
					Close();
				}
			}
			else if (!HasSearch)
			{
				_lastTime = DateTime.Now.Ticks;
				if (_animTarget == 0)
				{
					_animTarget = 1;
				}
				else if (_anim == 1f)
				{
					_anim = 0f;
					_stack.Add(element as GroupElement);
				}
			}
		}

		private List<Element> GetChildren(Element[] tree, Element parent)
		{
			var list = new List<Element>();
			var num = -1;
			var index = 0;
			while (index < tree.Length)
			{
				if (tree[index] == parent)
				{
					num = parent.level + 1;
					index++;
					break;
				}

				index++;
			}

			if (num == -1)
				return list;

			while (index < tree.Length)
			{
				var item = tree[index];
				if (item.level < num)
					return list;
				if (item.level <= num || HasSearch)
					list.Add(item);
				index++;
			}

			return list;
		}

		private GroupElement GetElementRelative(int rel)
		{
			var num = (_stack.Count + rel) - 1;
			return num < 0 ? null : _stack[num];
		}

		private class CallElement : Element
		{
			public readonly Action action;

			public CallElement(int level, string name, PopupItem item)
			{
				this.level = level;
				content = new GUIContent(name, item.image);
				action = () =>
				{
					item.action();
					content = new GUIContent(name, item.image);
				};
				searchable = item.searchable;
			}
		}

		[Serializable]
		private class GroupElement : Element
		{
			public Vector2 scroll;
			public int selectedIndex;

			public GroupElement(int level, string name)
			{
				this.level = level;
				content = new GUIContent(name);
				searchable = true;
			}
		}

		private class Element : IComparable
		{
			public GUIContent content;
			public int level;
			public bool searchable;

			public string Name => content.text;

			public int CompareTo(object o)
			{
				return string.Compare(Name, ((Element)o).Name, StringComparison.Ordinal);
			}
		}

		private class Styles
		{
			public readonly GUIStyle searchTextField = "SearchTextField";
			public readonly GUIStyle searchCancelButton = "SearchCancelButton";
			public readonly GUIStyle searchCancelButtonEmpty = "SearchCancelButtonEmpty";
			public readonly GUIStyle background = "grey_border";
			public readonly GUIStyle componentItem = new GUIStyle("PR Label");
			public readonly GUIStyle groupItem;
			public readonly GUIStyle header = new GUIStyle("In BigTitle");
			public readonly GUIStyle leftArrow = "AC LeftArrow";
			public readonly GUIStyle rightArrow = "AC RightArrow";

			public Styles()
			{
				header.font = EditorStyles.boldLabel.font;
				header.richText = true;
				componentItem.alignment = TextAnchor.MiddleLeft;
				componentItem.padding.left -= 15;
				componentItem.fixedHeight = 20f;
				componentItem.richText = true;
				groupItem = new GUIStyle(componentItem);
				groupItem.padding.left += 0x11;
				groupItem.richText = true;
			}
		}

		public class PopupItem
		{
			public int order;
			public Texture2D image;
			public bool searchable;
			public readonly string path;
			public readonly Action action;

			public PopupItem(string path, Action action)
			{
				searchable = true;
				this.path = path;
				this.action = action;
			}

			public PopupItem(string path, Action<PopupItem> action)
			{
				searchable = true;
				this.path = path;
				this.action = () => { action(this); };
			}
		}
	}

	public class Popup
	{
		private readonly PopupWindowAnim _window;

		public Rect ScreenRect
		{
			get => _window.screenRect;
			set => _window.screenRect = value;
		}

		public char Separator
		{
			get => _window.separator;
			set => _window.separator = value;
		}

		public string Title
		{
			get => _window.titleContent.text;
			set => _window.titleContent.text = value;
		}

		public bool AutoHeight
		{
			get => _window.autoHeight;
			set => _window.autoHeight = value;
		}

		public bool AutoClose
		{
			get => _window.autoClose;
			set => _window.autoClose = value;
		}

		public Popup(Rect screenRect, bool useSearch = true, string title = "Menu", char separator = '/')
		{
			_window = PopupWindowAnim.Create(screenRect, useSearch);
			Title = title;
			Separator = separator;
		}

		public Popup(float width, float height, bool useSearch = true, string title = "Menu", char separator = '/')
		{
			_window = PopupWindowAnim.Create(width, height, useSearch);
			Title = title;
			Separator = separator;
		}

		public Popup(float width, bool useSearch = true, string title = "Menu", char separator = '/', bool autoHeight = true)
		{
			_window = PopupWindowAnim.Create(width, useSearch);
			Title = title;
			Separator = separator;
			AutoHeight = autoHeight;
		}

		public Popup(bool useSearch = true, string title = "Menu", char separator = '/', bool autoHeight = true)
		{
			_window = PopupWindowAnim.Create(useSearch);
			Title = title;
			Separator = separator;
			AutoHeight = autoHeight;
		}

		public void Item(string name, Action action, int order = 0)
		{
			_window.Item(name, null, action, order);
		}

		public void Item(string name, Action<PopupWindowAnim.PopupItem> action, bool searchable, int order)
		{
			_window.Item(name, null, action, searchable, order);
		}

		public void Item(string name, Texture2D image, Action<PopupWindowAnim.PopupItem> action, bool searchable, int order)
		{
			_window.Item(name, image, action, searchable, order);
		}

		public void Show()
		{
			_window.Show();
		}
	}
}
