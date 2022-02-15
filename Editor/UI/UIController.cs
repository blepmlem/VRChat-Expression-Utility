using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDK3.Avatars.ScriptableObjects;

#pragma warning disable 4014

namespace ExpressionUtility.UI
{
	internal class UIController : IDisposable
	{
		private ExpressionUI _activeContent;
		private readonly Stack<ExpressionUI> _history = new Stack<ExpressionUI>();
		private readonly VisualElement _root;
		private readonly ToolbarBreadcrumbs _breadcrumbs = new ToolbarBreadcrumbs();
		private readonly EditorWindow _window;
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
			_window = window;

			Assets.UIAssets.TryGetValue(typeof(MainWindow), out var mainWindow);
			mainWindow?.FirstOrDefault()?.Layout.CloneTree(_root);

			ContentFrame = _root.Q("content-frame");
			
			_root.Q<Toolbar>("navigation").Add(_breadcrumbs);
			Assets.MiniAvatar.CloneTree(_root.Q("header"));
			var miniAvatarObjectField = _root.Q("avatar-mini").Q<ObjectField>("object-field");
			miniAvatarObjectField.RegisterValueChangedCallback(e => miniAvatarObjectField.SetValueWithoutNotify(e.previousValue));
			
			ExpressionInfo = new ExpressionInfo(UpdateMiniAvatar);

			if (AvatarCache.GetAllAvatarInfo().Count == 1)
			{
				ExpressionInfo.SetInfo(AvatarCache.GetAllAvatarInfo().First());
			}
			AvatarCache.AvatarWasUpdated += OnAvatarWasUpdated;
			UpdateMiniAvatar(ExpressionInfo);
			Messages = new Messages(this, _root.Q("info-box"));

			SetupFooter();
			SetupUpdater();
		}

		private void SetupFooter()
		{
			var githubIcon = _root.Q("github-icon");
			githubIcon.style.backgroundImage = EditorGUIUtility.isProSkin ? Assets.GithubLight : Assets.GithubDark;
			var githubButton = _root.Q<Button>("github-button");
			githubButton.clicked += () => Application.OpenURL("https://github.com/blepmlem/VRChat-Expression-Utility");
			var donateButton = _root.Q<Button>("donate-button");
			donateButton.clicked += () => Application.OpenURL("https://ko-fi.com/blepblem");
		}

		private async Task SetupUpdater()
		{
			var updater = await Updater.GetUpdater();
			var updateBox = _root.Q("update-info");
			var updateText = _root.Q<Label>("update-text");
			var updateButton = _root.Q<Button>("update-button");
			var versionLabel = _root.Q<Label>("version-label");
			
			versionLabel.text = $"Version {updater.LocalPackage.version}";
			updateBox.Display(updater.HasNewerVersion);
			updateText.text = $"There is a new update available!\nVersion {updater.LatestOnlineVersion} is out now.";

			void Clicked()
			{
				updateText.text = $"Now updating...";
				updateButton.Display(false);
				updater.InstallUpdate(Close);
			}

			updateButton.clicked += Clicked;
		}

		private void OnAvatarWasUpdated(AvatarCache.AvatarInfo info)
		{
			if (ExpressionInfo.AvatarInfo == info)
			{
				UpdateMiniAvatar(ExpressionInfo);
				if (!info.IsValid)
				{
					var active = _history.Peek();
					if (active is Intro || active is AvatarSelection)
					{
						return;
					}
					NavigateHistory(active);
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
				ob.SetValueWithoutNotify(info.AvatarDescriptor.gameObject);
			}

			ob.Q(null, "unity-object-field__selector").Display(false);
			ob.Q(null, "unity-object-field-display__label").Display(true);
		}

		public void SetFrame<T>() where T : ExpressionUI => SetFrame(typeof(T));

		private void SetFrame(Type type)
		{
			if (type == null)
			{
				return;
			}
			if (!Assets.UIAssets.TryGetValue(type, out var instances))
			{
				$"Failed to find assets for {type}".LogError();
				return;
			}
			SetFrame(instances.FirstOrDefault());
		}

		public void SetFrame(ExpressionUI instance)
		{
			Messages.Clear();
			if(instance != _activeContent)
			{
				_history.Push(instance);
				_breadcrumbs.PushItem(ObjectNames.NicifyVariableName(instance.Name), () => NavigateHistory(instance));
			}
			ContentFrame.Clear();
			
			instance.Layout.CloneTree(ContentFrame);

			ExpressionUI previousContent = _activeContent;
			_activeContent = instance;
			if(previousContent != null)
			{
				previousContent.OnExit(_activeContent);
			}

			SetExpressionInfoBoxActive(instance, ExpressionInfo);
			_activeContent.BindControls(ContentFrame);
			_activeContent.OnEnter(this, previousContent);
		}

		private void SetExpressionInfoBoxActive(ExpressionUI instance, ExpressionInfo expressionInfo)
		{
			var box = _root.Q("expression-info-box");

			if (!(instance is IExpressionDefinition))
			{
				box.style.display = DisplayStyle.None;
				return;
			}

			box.style.display = DisplayStyle.Flex;
			var expMenu = box.Q<ObjectField>("expression-menu");
			var expAnimator = box.Q<ObjectField>("expression-animator");
			var expAnimFolder = box.Q<ObjectField>("expression-animation-folder");
			expMenu.objectType = typeof(VRCExpressionsMenu);
			expAnimator.objectType = typeof(RuntimeAnimatorController);
			expAnimFolder.objectType = typeof(DefaultAsset);
			
			var expName = box.Q<Label>("expression-name");
			expName.text = expressionInfo.ExpressionName;
			var expDefinitionName= box.Q<Label>("expression-definition-name");
			expDefinitionName.text = instance.Name;
			
			expMenu.SetValueWithoutNotify(expressionInfo.Menu);
			expAnimator.SetValueWithoutNotify(expressionInfo.Controller);
			expAnimFolder.SetValueWithoutNotify(expressionInfo.AnimationsFolder);
			
			expMenu.Q(null, "unity-object-field__selector").Display(false);
			expAnimator.Q(null, "unity-object-field__selector").Display(false);
			expAnimFolder.Q(null, "unity-object-field__selector").Display(false);
			
			expMenu.RegisterValueChangedCallback(e => expMenu.SetValueWithoutNotify(e.previousValue));
			expAnimator.RegisterValueChangedCallback(e => expAnimator.SetValueWithoutNotify(e.previousValue));
			expAnimFolder.RegisterValueChangedCallback(e => expAnimFolder.SetValueWithoutNotify(e.previousValue));
		}
		
		private void NavigateHistory(ExpressionUI instance)
		{
			if (_history.Peek() == instance)
			{
				return;
			}

			while (_breadcrumbs.childCount > 0)
			{
				_breadcrumbs.PopItem();
				ExpressionUI target;
				if ((target = _history.Pop()) == instance)
				{
					SetFrame(target);
					return;
				}
			}
		}

		public void Dispose()
		{
			if (_activeContent != null)
			{
				_activeContent.OnExit(null);
			}
			AvatarCache?.Dispose();
			ExpressionInfo?.Dispose();
		}

		public void Close() => _window.Close();
	}
}