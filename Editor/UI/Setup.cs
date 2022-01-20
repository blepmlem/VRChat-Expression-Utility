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
using VRC.SDK3.Avatars.ScriptableObjects;
using Object = UnityEngine.Object;

namespace ExpressionUtility.UI
{
	internal class Setup : IExpressionUI
	{
		private UIController _controller;
		private ScrollView _scrollView;
		private ObjectField _folderField;
		private Messages _messages;
		public void OnEnter(UIController controller, IExpressionUI previousUI)
		{
			var expressionInfo = controller.ExpressionInfo;
			foreach (var ob in controller.ContentFrame.Query<ObjectField>().Build().ToList())
			{
				var avatarAnimatorObjField = new AvatarAnimatorObjField(ob, expressionInfo);
			}

			_controller = controller;
			_messages = controller.Messages;
			_scrollView = controller.ContentFrame.Q<ScrollView>("expression-buttons");
			foreach (var type in TypeCache.GetTypesDerivedFrom<IExpressionDefinition>())
			{
				if (controller.AssetsReferences.ExpressionDefinitionAssets.TryGetValue(type, out var result))
				{
					var btn = controller.AssetsReferences.ExpressionDefinitionPreviewButton.InstantiateTemplate<Button>(_scrollView.contentContainer);

					btn.Q<Label>("header").text = ObjectNames.NicifyVariableName(result.Name);
					btn.Q<Label>("description").text = result.Description;
					btn.Q("thumbnail").style.backgroundImage = result.Icon;
					btn.clickable = new Clickable(() => controller.SetFrame(type));
				}
			}

			_folderField = controller.ContentFrame.Q<ObjectField>("animation-folder");
			_folderField.objectType = typeof(DefaultAsset);
			_folderField.value = expressionInfo.AnimationsFolder;
			
			_folderField.SetEnabled(true);
			_folderField.Q(null, "unity-object-field__selector").Display(true);
			_folderField.Q(null, "unity-object-field-display__label").Display(true);
			
			_folderField.RegisterValueChangedCallback(e => expressionInfo.AnimationsFolder = e.newValue as DefaultAsset);

			var menuSelection = BuildMenuSelection(controller);
			menuSelection.RegisterValueChangedCallback(e => _controller.ExpressionInfo.Menu = e.newValue);
			expressionInfo.DataWasUpdated += e => ErrorValidate();
			ErrorValidate();
		}

		private PopupField<VRCExpressionsMenu> BuildMenuSelection(UIController controller)
		{
			VRCExpressionsMenu menu = controller.ExpressionInfo.AvatarDescriptor.expressionsMenu;
			if (menu == null)
			{
				//TODO
				return null;
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
			var menuSelector = new PopupField<VRCExpressionsMenu>(menus, menu, PrettifyName, PrettifyName);
			menuSelector.label = "Expressions menu";
			var holder = controller.ContentFrame.Q("menu-selection");
			
			holder.Add(menuSelector);

			var targetMenu = controller.ExpressionInfo.Menu;
			if (targetMenu != null && menus.Contains(targetMenu))
			{
				menuSelector.value = targetMenu;
			}
			
			controller.ExpressionInfo.Menu = menuSelector.value;
			return menuSelector;
		}



		private void ErrorValidate()
		{
			var expressionInfo = _controller.ExpressionInfo;
			bool hasErrors = false;
			if (_messages.SetActive(_folderField.value == null, "specify-animation-folder"))
			{
				hasErrors = true;
			}
			else
			{
				var path = AssetDatabase.GetAssetPath(_folderField.value);
				if (_messages.SetActive(!Directory.Exists(path), "animation-folder-invalid"))
				{
					hasErrors = true;
				}
			}

			var controllerLayers = expressionInfo.AvatarDescriptor.baseAnimationLayers;

			var invalidAnimator = expressionInfo.Controller == null;
			hasErrors |= _messages.SetActive(invalidAnimator, "select-valid-animator");
			var noValidAnim = controllerLayers.All(a => a.animatorController == null || a.isDefault);
			_messages.SetActive(noValidAnim, "no-valid-animators");
			hasErrors |= noValidAnim;

			var notFxLayer = !invalidAnimator && expressionInfo.Controller != controllerLayers.LastOrDefault().animatorController;
			_messages.SetActive(notFxLayer, "not-fx-layer");
			
			_scrollView.SetEnabled(!hasErrors);
		}

		public void OnExit(IExpressionUI nextUI)
		{
			
		}

		private class AvatarAnimatorObjField
		{
			private readonly ObjectField _objectField;
			private readonly Button _button;
			private readonly ExpressionInfo _expressionInfo;

			public AvatarAnimatorObjField(ObjectField objectField, ExpressionInfo controllerExpressionInfo)
			{
				_objectField = objectField;
				objectField.objectType = typeof(AnimatorController);
				_button = objectField.Q<Button>(null, "unity-button");
				if (_button != null)
				{
					_button.clickable.clicked += OnClicked;
				}
				_expressionInfo = controllerExpressionInfo;
				CleanObjectField(objectField);

				switch (objectField.name)
				{
					case "active-animator":
						controllerExpressionInfo.DataWasUpdated += UpdateActiveAnimator;
						UpdateActiveAnimator(controllerExpressionInfo);
						_objectField.SetEnabled(false);
						break;
					case "animator-base":
						_objectField.value = controllerExpressionInfo.AvatarDescriptor.baseAnimationLayers[0].animatorController;
						_objectField.SetEnabled(_objectField.value != null);
						break;
					case "animator-additive":
						_objectField.value = controllerExpressionInfo.AvatarDescriptor.baseAnimationLayers[1].animatorController;
						_objectField.SetEnabled(_objectField.value != null);
						break;
					case "animator-gesture":
						_objectField.value = controllerExpressionInfo.AvatarDescriptor.baseAnimationLayers[2].animatorController;
						_objectField.SetEnabled(_objectField.value != null);
						break;
					case "animator-action":
						_objectField.value = controllerExpressionInfo.AvatarDescriptor.baseAnimationLayers[3].animatorController;
						_objectField.SetEnabled(_objectField.value != null);
						break;
					case "animator-fx":
						_objectField.value = controllerExpressionInfo.AvatarDescriptor.baseAnimationLayers[4].animatorController;
						_objectField.SetEnabled(_objectField.value != null);
						break;
				}
			}

			private void UpdateActiveAnimator(ExpressionInfo obj)
			{
				if (_objectField != null)
				{
					var value = obj.Controller;
					if (value != null && obj.AvatarDescriptor.OwnsAnimator(value))
					{
						_objectField.value = obj.Controller;
					}
				}
			}

			private void OnClicked() => _expressionInfo.Controller = _objectField.value as AnimatorController;


			private void CleanObjectField(ObjectField ob)
			{
				ob.Q(null, "unity-object-field__selector").Display(false);
				ob.Q(null, "unity-object-field-display__label").Display(true);
			}
		}
	}
}