using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ExpressionUtility.UI
{
	internal class UIController : IDisposable
	{
		private readonly Stack<Type> _history = new Stack<Type>();
		private IExpressionUI _activeContent;
		private readonly VisualElement _root;
		private readonly ToolbarBreadcrumbs _breadcrumbs = new ToolbarBreadcrumbs();
		
		public Messages Messages { get; }
		public VisualElement ContentFrame { get; }
		public Assets Assets { get; }
		public AvatarCache AvatarCache { get; } = new AvatarCache();
		public ExpressionInfo ExpressionInfo { get; }
		
		public UIController(EditorWindow window, Assets assets)
		{
			assets.Initialize();
			Assets = assets;
			_root = window.rootVisualElement;
			
			Assets.MainWindow.CloneTree(_root);

			ContentFrame = _root.Q("content-frame");
			
			_root.Q<Toolbar>("navigation").Add(_breadcrumbs);
			Assets.MiniAvatar.CloneTree(_root.Q("header"));

			ExpressionInfo = new ExpressionInfo(UpdateMiniAvatar);
			if (AvatarCache.GetAllAvatarInfo().Count == 1)
			{
				ExpressionInfo.SetInfo(AvatarCache.GetAllAvatarInfo().First());
			}
			AvatarCache.AvatarWasUpdated += OnAvatarWasUpdated;
			UpdateMiniAvatar(ExpressionInfo);
			Messages = new Messages(this, _root.Q("root"));
		}

		private void OnAvatarWasUpdated(AvatarCache.AvatarInfo info)
		{
			if (ExpressionInfo.AvatarInfo == info)
			{
				UpdateMiniAvatar(ExpressionInfo);
				if (!info.IsValid)
				{
					var active = _history.Peek();
					if (active == typeof(Intro) || active == typeof(AvatarSelection))
					{
						return;
					}
					NavigateHistory(typeof(AvatarSelection));
				}
			}
		}

		private void UpdateMiniAvatar(ExpressionInfo info)
		{
			var element = _root.Q("avatar-mini");
			element.Display(info.AvatarInfo?.IsValid ?? false);
			
			element.Q("thumbnail").style.backgroundImage = info.AvatarInfo?.Thumbnail;
			var ob = element.Q<ObjectField>("object-field");
			ob.objectType = typeof(GameObject);
			ob.allowSceneObjects = true;
			
			if (info.AvatarDescriptor != null)
			{
				ob.value = info.AvatarDescriptor.gameObject;
			}

			ob.Q(null, "unity-object-field__selector").Display(false);
			ob.Q(null, "unity-object-field-display__label").Display(true);
		}

		public void SetFrame<T>() where T : IExpressionUI => SetFrame(typeof(T));

		public void SetFrame(Type type)
		{
			if (type == null)
			{
				return;
			}

			Messages.Clear();
			if (!Assets.UIAssets.TryGetValue(type, out (IExpressionUI ui, VisualTreeAsset treeAsset) assets))
			{
				$"Failed to find assets for {type}".LogError();
				return;
			}

			_history.Push(type);
			_breadcrumbs.PushItem(ObjectNames.NicifyVariableName(type.Name), () => NavigateHistory(type));

			ContentFrame.Clear();
			assets.treeAsset.CloneTree(ContentFrame);

			IExpressionUI previousContent = _activeContent;
			_activeContent = assets.ui;
			previousContent?.OnExit(_activeContent);
			_activeContent.OnEnter(this, previousContent);
		}

		private void NavigateHistory(Type type)
		{
			if (_history.Peek() == type)
			{
				return;
			}

			while (_breadcrumbs.childCount > 0)
			{
				_breadcrumbs.PopItem();
				Type target;
				if ((target = _history.Pop()) == type)
				{
					SetFrame(target);
					return;
				}
			}
		}

		public void Dispose()
		{
			_activeContent?.OnExit(null);
			AvatarCache?.Dispose();
			ExpressionInfo?.Dispose();
		}
	}
}