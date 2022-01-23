using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using Object = UnityEngine.Object;

namespace ExpressionUtility.UI
{
	internal class Setup : IExpressionUI
	{
		private UIController _controller;
		private Messages _messages;
		private ScrollView _expressionScrollView;
		private ObjectField _folderField;
		private TextField _nameField;

		public void OnEnter(UIController controller, IExpressionUI previousUI)
		{
			_controller = controller;
			_messages = controller.Messages;
			_expressionScrollView = controller.ContentFrame.Q<ScrollView>("expression-buttons");
			_folderField = controller.ContentFrame.Q<ObjectField>("animation-folder");
			_nameField = controller.ContentFrame.Q<TextField>("expression-name"); 
				
			BuildNameSelection(controller);
			BuildExpressionSelection(controller);
			BuildAnimatorSelection(controller);
			BuildAnimationFolderSelection(controller);
			BuildMenuSelection(controller);
			ErrorValidate();
		}

		private void BuildNameSelection(UIController controller)
		{
			var nameField = controller.ContentFrame.Q<TextField>("expression-name");
			nameField.RegisterValueChangedCallback(e => SetName(e.newValue));
			
			SetName(controller.ExpressionInfo.ExpressionName);
			void SetName(string name)
			{
				nameField.SetValueWithoutNotify(name);
				if (controller.ExpressionInfo.ExpressionName != name)
				{
					controller.ExpressionInfo.ExpressionName = name;
				}

				ErrorValidate();
			}
		}

		private void BuildAnimationFolderSelection(UIController controller)
		{
			var animFolder = controller.ContentFrame.Q<ObjectField>("animation-folder");
			animFolder.objectType = typeof(DefaultAsset);
			animFolder.SetEnabled(true);
			animFolder.Q(null, "unity-object-field__selector").Display(true);
			animFolder.Q(null, "unity-object-field-display__label").Display(true);
			animFolder.RegisterValueChangedCallback(e => SetFolder(e.newValue as DefaultAsset));
			SetFolder(controller.ExpressionInfo.AnimationsFolder);
			
			void SetFolder(DefaultAsset folder)
			{
				animFolder.SetValueWithoutNotify(folder);
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
			var animatorsHolder = controller.ContentFrame.Q<Foldout>("avatar-animators");
			var activeAnimator = controller.ContentFrame.Q<ObjectField>("active-animator");
			
			void CleanObjectField(ObjectField field)
			{
				field.objectType = typeof(AnimatorController);
				field.Q(null, "unity-object-field__selector").Display(false);
				field.Q(null, "unity-object-field-display__label").Display(true);
			}

			CleanObjectField(activeAnimator);
			foreach (var objectField in animatorsHolder.Query<ObjectField>().Build().ToList())
			{
				CleanObjectField(objectField);
				switch (objectField.name)
				{
					case "animator-base": objectField.value = animatorLayers[0].animatorController; break;
					case "animator-additive": objectField.value = animatorLayers[1].animatorController; break;
					case "animator-gesture": objectField.value = animatorLayers[2].animatorController; break;
					case "animator-action": objectField.value = animatorLayers[3].animatorController; break;
					case "animator-fx": objectField.value = animatorLayers[4].animatorController; break;
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
				
				activeAnimator.SetValueWithoutNotify(obj);
				if (controller.ExpressionInfo.Controller != obj)
				{
					controller.ExpressionInfo.Controller = obj;
				}

				animatorsHolder.value = obj == null;
				ErrorValidate();
			}
		}
		
		private void BuildExpressionSelection(UIController controller)
		{
			var scroll = controller.ContentFrame.Q<ScrollView>("expression-buttons");
			foreach (var type in TypeCache.GetTypesDerivedFrom<IExpressionDefinition>())
			{
				if (controller.AssetsReferences.ExpressionDefinitionAssets.TryGetValue(type, out var result))
				{
					var btn = controller.AssetsReferences.ExpressionDefinitionPreviewButton.InstantiateTemplate<Button>(scroll.contentContainer);

					btn.Q<Label>("header").text = ObjectNames.NicifyVariableName(result.Name);
					btn.Q<Label>("description").text = result.Description;
					btn.Q("thumbnail").style.backgroundImage = result.Icon;
					btn.clickable = new Clickable(() => controller.SetFrame(type));
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
			
			IEnumerable<VRCExpressionsMenu> GetAllMenus(VRCExpressionsMenu m)
			{
				yield return m;
				foreach (VRCExpressionsMenu vrcExpressionsMenu in m.controls.Where(mControl => mControl.type == VRCExpressionsMenu.Control.ControlType.SubMenu && mControl.subMenu != null).SelectMany(mControl => GetAllMenus(mControl.subMenu)))
				{
					yield return vrcExpressionsMenu;
				}
			}

			string PrettifyName(VRCExpressionsMenu arg) => arg.name;

			var menus = GetAllMenus(menu).ToList();
			var menuSelector = new PopupField<VRCExpressionsMenu>(menus, menu, PrettifyName, PrettifyName)
			{
				label = "Expression menu",
			};

			var placeholder = controller.ContentFrame.Q("menu-selection");
			controller.ContentFrame.Replace(placeholder, menuSelector);
			
			menuSelector.RegisterValueChangedCallback(e => SetMenu(e.newValue));
			SetMenu(controller.ExpressionInfo.Menu);

			void SetMenu(VRCExpressionsMenu obj)
			{
				if (obj == null || !menus.Contains(obj))
				{
					obj = menus.FirstOrDefault();
				}
				
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
			
			bool folderEmpty = _folderField.value == null;
			bool invalidFolder = !folderEmpty && !Directory.Exists(AssetDatabase.GetAssetPath(_folderField.value));
			bool missingRootMenu = !expressionInfo.AvatarDescriptor.expressionsMenu;
			bool invalidAnimator = expressionInfo.Controller == null;
			bool noValidAnim = controllerLayers.All(a => a.animatorController == null || a.isDefault);
			bool notFxLayer = !invalidAnimator && expressionInfo.Controller != controllerLayers.LastOrDefault().animatorController;
			bool layerNameExists = expressionInfo.Controller.layers.Any(l => l.name.Equals(expressionInfo.ExpressionName, StringComparison.InvariantCultureIgnoreCase));
			bool parameterExists = expressionInfo.AvatarDescriptor.expressionParameters.parameters.Any(p => p.name.Equals(expressionInfo.ExpressionName, StringComparison.InvariantCultureIgnoreCase));
			bool nameEmpty = string.IsNullOrEmpty(expressionInfo.ExpressionName);
			bool inUse = !nameEmpty && (layerNameExists || parameterExists);

			_messages.SetActive(nameEmpty, "give-expr-name");
			_messages.SetActive(inUse, "expr-in-use");
			_messages.SetActive(invalidAnimator, "select-valid-animator");
			_messages.SetActive(noValidAnim, "no-valid-animators");
			_messages.SetActive(notFxLayer, "not-fx-layer");
			_messages.SetActive(missingRootMenu, "missing-root-menu");
			_messages.SetActive(folderEmpty, "specify-animation-folder");
			_messages.SetActive(invalidFolder, "animation-folder-invalid");
				
			bool hasErrors = invalidAnimator || noValidAnim || notFxLayer || nameEmpty || inUse;
			_expressionScrollView.SetEnabled(!hasErrors);
		}

		public void OnExit(IExpressionUI nextUI)
		{
			
		}
	}
}