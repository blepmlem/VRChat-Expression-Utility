using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.Core;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;

namespace ExpressionUtility.UI
{
	internal class UIController : IDisposable
	{
		public UIController(EditorWindow window, AssetReferences assetReferences)
		{
			assetReferences.Initialize();
			AssetsReferences = assetReferences;
			Window = window;
			Root = window.rootVisualElement;
			
			AssetsReferences.MainWindow.CloneTree(Root);

			ContentFrame = Root.Q("content-frame");
			
			Root.Q<Toolbar>("navigation").Add(Breadcrumbs);;
			AssetsReferences.MiniAvatar.CloneTree(Root.Q("header"));

			ExpressionInfo = new ExpressionInfo();
			ExpressionInfo.DataWasUpdated += UpdateMiniAvatar;
			if (AvatarCache.GetAllAvatarInfo().Count() == 1)
			{
				ExpressionInfo.SetInfo(AvatarCache.GetAllAvatarInfo().First());
			}
			AvatarCache.AvatarWasUpdated += OnAvatarWasUpdated;
			UpdateMiniAvatar(ExpressionInfo);
			Messages = new Messages(this, Root);
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

		private readonly Stack<Type> _history = new Stack<Type>();
		private EditorWindow Window { get; }
		private IExpressionUI ActiveContent { get; set; }
		private VisualElement Root { get; }
		private ToolbarBreadcrumbs Breadcrumbs { get; } = new ToolbarBreadcrumbs();
		public Messages Messages { get; }
		public VisualElement ContentFrame { get; }
		public AssetReferences AssetsReferences { get; }
		public AvatarCache AvatarCache { get; } = new AvatarCache();

		public ExpressionInfo ExpressionInfo { get; }

		private void UpdateMiniAvatar(ExpressionInfo info)
		{
			var element = Root.Q("avatar-mini");
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
			if (!AssetsReferences.UIAssets.TryGetValue(type, out (IExpressionUI ui, VisualTreeAsset treeAsset) assets))
			{
				$"Failed to find assets for {type}".LogError();
				return;
			}

			_history.Push(type);
			Breadcrumbs.PushItem(ObjectNames.NicifyVariableName(type.Name), () => NavigateHistory(type));

			ContentFrame.Clear();
			assets.treeAsset.CloneTree(ContentFrame);

			IExpressionUI previousContent = ActiveContent;
			ActiveContent = assets.ui;
			previousContent?.OnExit(ActiveContent);
			ActiveContent.OnEnter(this, previousContent);
		}

		private void NavigateHistory(Type type)
		{
			if (_history.Peek() == type)
			{
				return;
			}

			while (Breadcrumbs.childCount > 0)
			{
				Breadcrumbs.PopItem();
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
			ActiveContent?.OnExit(null);
			AvatarCache?.Dispose();
			ExpressionInfo?.Dispose();
		}
	}
}