using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace ExpressionUtility.UI
{
	internal class Setup : ExpressionUI
	{
		private UIController _controller;
		private Messages _messages;
		private ScrollView _expressionScrollView;
		private ObjectField _folderField;
		private TextField _nameField;
		private Foldout _avatarAnimators;
		private ObjectField _activeAnimator;
		private VisualElement _menuSelectionPlaceholder;

		public override void BindControls(VisualElement root)
		{
			_expressionScrollView = root.Q<ScrollView>("expression-buttons");
			_folderField = root.Q<ObjectField>("animation-folder");
			_nameField = root.Q<TextField>("expression-name");
			_avatarAnimators = root.Q<Foldout>("avatar-animators");
			_activeAnimator = root.Q<ObjectField>("active-animator");
			_menuSelectionPlaceholder = root.Q("menu-selection");
		}

		public override void OnEnter(UIController controller, ExpressionUI previousUI)
		{
			_controller = controller;
			_messages = controller.Messages;


			BuildNameSelection(controller);
			BuildExpressionSelection(controller);
			BuildAnimatorSelection(controller);
			BuildAnimationFolderSelection(controller);
			BuildMenuSelection(controller);
			ErrorValidate();
		}

		private void BuildNameSelection(UIController controller)
		{
			_nameField.RegisterValueChangedCallback(e => SetName(e.newValue));
			
			SetName(controller.ExpressionInfo.ExpressionName);
			void SetName(string expressionName)
			{
				_nameField.SetValueWithoutNotify(expressionName);
				if (controller.ExpressionInfo.ExpressionName != expressionName)
				{
					controller.ExpressionInfo.ExpressionName = expressionName;
				}

				ErrorValidate();
			}
		}

		private void BuildAnimationFolderSelection(UIController controller)
		{
			_folderField.objectType = typeof(DefaultAsset);
			_folderField.SetEnabled(true);
			// _folderField.Q(null, "unity-object-field__selector").Display(true);
			// _folderField.Q(null, "unity-object-field-display__label").Display(true);
			_folderField.RegisterValueChangedCallback(e => SetFolder(e.newValue as DefaultAsset));
			SetFolder(controller.ExpressionInfo.AnimationsFolder);
			
			void SetFolder(DefaultAsset folder)
			{
				_folderField.SetValueWithoutNotify(folder);
				if (controller.ExpressionInfo.AnimationsFolder != folder)
				{
					controller.ExpressionInfo.AnimationsFolder = folder;
				}

				ErrorValidate();
			}
		}

		private void BuildAnimatorSelection(UIController controller)
		{
			var animatorLayers = controller.ExpressionInfo.AvatarDescriptor.baseAnimationLayers;
			
			void CleanObjectField(ObjectField field)
			{
				field.objectType = typeof(AnimatorController);
				field.RemoveObjectSelector();
				// field.Q(null, "unity-object-field-display__label").Display(true);
				field.RegisterValueChangedCallback(e => field.SetValueWithoutNotify(e.previousValue));
			}

			CleanObjectField(_activeAnimator);
			foreach (var objectField in _avatarAnimators.Query<ObjectField>().Build().ToList())
			{
				CleanObjectField(objectField);
				switch (objectField.name)
				{
					case "animator-base": objectField.SetValueWithoutNotify(animatorLayers[0].animatorController); break;
					case "animator-additive": objectField.SetValueWithoutNotify(animatorLayers[1].animatorController); break;
					case "animator-gesture": objectField.SetValueWithoutNotify(animatorLayers[2].animatorController); break;
					case "animator-action": objectField.SetValueWithoutNotify(animatorLayers[3].animatorController); break;
					case "animator-fx": objectField.SetValueWithoutNotify(animatorLayers[4].animatorController); break;
				}
				objectField.SetEnabled(objectField.value != null);
				var button = objectField.Q<Button>(null, "unity-button");
				button.clickable.clicked += () => SetAnimator(objectField.value as AnimatorController);
			}

			SetAnimator(controller.ExpressionInfo.Controller);

			void SetAnimator(AnimatorController obj)
			{
				if (!controller.ExpressionInfo.AvatarDescriptor.OwnsAnimator(obj))
				{
					var last = animatorLayers.Last();
					obj = !last.isDefault && last.animatorController != null ? last.animatorController as AnimatorController: null;
				}
				
				_activeAnimator.SetValueWithoutNotify(obj);
				if (controller.ExpressionInfo.Controller != obj)
				{
					controller.ExpressionInfo.Controller = obj;
				}

				_avatarAnimators.value = obj == null;
				ErrorValidate();
			}
		}
		
		private void BuildExpressionSelection(UIController controller)
		{
			foreach (ExpressionUI expressionUI in controller.Assets.UIAssets.SelectMany(u => u.Value))
			{
				if (expressionUI is IExpressionDefinition)
				{
					var btn = controller.Assets.ExpressionDefinitionPreviewButton.InstantiateTemplate<Button>(_expressionScrollView.contentContainer);

					expressionUI.Icon.mipMapBias = -1;
					btn.Q<Label>("header").text = ObjectNames.NicifyVariableName(expressionUI.Name);
					btn.Q<Label>("description").text = expressionUI.Description;
					btn.Q("thumbnail").style.backgroundImage = expressionUI.Icon;
					btn.clickable = new Clickable(() => controller.SetFrame(expressionUI));
				}
			}
		}

		private void BuildMenuSelection(UIController controller)
		{
			VRCExpressionsMenu menu = controller.ExpressionInfo.AvatarDescriptor.expressionsMenu;

			if (menu == null)
			{
				ErrorValidate();
				return;
			}

			string PrettifyName(VRCExpressionsMenu arg)
			{
				if (arg == null)
				{
					return "None";
				}
				return arg.name;
			}

			var menus = menu.GetMenusRecursively().ToList();
			menus.Add(null);
			var menuSelector = new PopupField<VRCExpressionsMenu>(menus, menu, PrettifyName, PrettifyName)
			{
				label = "Expression menu",
			};
			
			controller.ContentFrame.Replace(_menuSelectionPlaceholder, menuSelector);
			
			menuSelector.RegisterValueChangedCallback(e => SetMenu(e.newValue));
			SetMenu(controller.ExpressionInfo.Menu);

			void SetMenu(VRCExpressionsMenu obj)
			{
				menuSelector.SetValueWithoutNotify(obj);
				if (controller.ExpressionInfo.Menu != obj)
				{
					controller.ExpressionInfo.Menu = obj;
				}
				ErrorValidate();
			}
		}
		
		private void ErrorValidate()
		{
			var expressionInfo = _controller.ExpressionInfo;
			var controllerLayers = expressionInfo.AvatarDescriptor.baseAnimationLayers;

			bool folderEmpty = expressionInfo.AnimationsFolder == null;
			bool invalidFolder = !folderEmpty && !Directory.Exists(AssetDatabase.GetAssetPath(expressionInfo.AnimationsFolder));
			bool missingRootMenu = !expressionInfo.AvatarDescriptor.expressionsMenu;
			bool menuIsNone = !missingRootMenu && !expressionInfo.Menu;
			bool invalidAnimator = expressionInfo.Controller == null;
			bool noValidAnim = controllerLayers.All(a => a.animatorController == null || a.isDefault);
			bool notFxLayer = !invalidAnimator && expressionInfo.Controller != controllerLayers.LastOrDefault().animatorController;
			bool layerNameExists = !invalidAnimator && expressionInfo.Controller.layers.Any(l => l.name.Equals(expressionInfo.ExpressionName, StringComparison.InvariantCultureIgnoreCase));
			bool parameterExists = !invalidAnimator && expressionInfo.AvatarDescriptor.expressionParameters.parameters.Any(p => p.name.Equals(expressionInfo.ExpressionName, StringComparison.InvariantCultureIgnoreCase));
			bool nameEmpty = string.IsNullOrEmpty(expressionInfo.ExpressionName);
			bool inUse = !nameEmpty && (layerNameExists || parameterExists);

			_messages.SetActive(menuIsNone, "menu-is-none");
			_messages.SetActive(nameEmpty, "give-expr-name");
			_messages.SetActive(inUse, "expr-in-use");
			_messages.SetActive(!noValidAnim && invalidAnimator, "select-valid-animator");
			_messages.SetActive(noValidAnim, "no-valid-animators");
			_messages.SetActive(notFxLayer, "not-fx-layer");
			_messages.SetActive(missingRootMenu, "missing-root-menu");
			_messages.SetActive(folderEmpty, "specify-animation-folder");
			_messages.SetActive(invalidFolder, "animation-folder-invalid");
				
			bool hasErrors = invalidAnimator || noValidAnim || nameEmpty || inUse || folderEmpty || invalidFolder || missingRootMenu;
			_expressionScrollView.SetEnabled(!hasErrors);
		}
	}
}