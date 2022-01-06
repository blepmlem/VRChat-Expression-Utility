using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ExpressionUtility.UI
{
	internal class Setup : IExpressionUI
	{
			
		public void OnEnter(UIController controller, IExpressionUI previousUI)
		{
			foreach (var ob in controller.ContentFrame.Query<ObjectField>().Build().ToList())
			{
				var avatarAnimatorObjField = new AvatarAnimatorObjField(ob, controller.ExpressionInfo);
			}

			foreach (var type in TypeCache.GetTypesDerivedFrom<IExpressionDefinition>())
			{
				if (controller.AssetsReferences.ExpressionDefinitionAssets.TryGetValue(type, out var result))
				{
					var scrollView = controller.ContentFrame.Q<ScrollView>("expression-buttons");

					controller.AssetsReferences.ExpressionDefinitionPreviewButton.CloneTree(scrollView.contentContainer);
					
					var btn = scrollView.contentContainer.Children().Last() as Button;
					
					btn.Q<Label>("header").text = ObjectNames.NicifyVariableName(result.Name);
					btn.Q<Label>("description").text = result.Description;
					btn.Q("thumbnail").style.backgroundImage = result.Icon;
					btn.clickable = new Clickable(() => controller.SetFrame(type));
				}
			}
		}

		public void OnExit(IExpressionUI nextUI)
		{
			
		}

		private class AvatarAnimatorObjField
		{
			public AvatarAnimatorObjField(ObjectField objectField, ExpressionInfo controllerExpressionInfo)
			{
				ObjectField = objectField;
				objectField.objectType = typeof(AnimatorController);
				Button = objectField.Q<Button>(null, "unity-button");
				if (Button != null)
				{
					Button.clickable.clicked += OnClicked;
				}
				ExpressionInfo = controllerExpressionInfo;
				CleanObjectField(objectField);

				switch (objectField.name)
				{
					case "active-animator":
						controllerExpressionInfo.DataWasUpdated += UpdateActiveAnimator;
						UpdateActiveAnimator(controllerExpressionInfo);
						break;
					case "animator-base":
						ObjectField.value = controllerExpressionInfo.AvatarDescriptor.baseAnimationLayers[0].animatorController;
						break;
					case "animator-additive":
						ObjectField.value = controllerExpressionInfo.AvatarDescriptor.baseAnimationLayers[1].animatorController;
						break;
					case "animator-gesture":
						ObjectField.value = controllerExpressionInfo.AvatarDescriptor.baseAnimationLayers[2].animatorController;
						break;
					case "animator-action":
						ObjectField.value = controllerExpressionInfo.AvatarDescriptor.baseAnimationLayers[3].animatorController;
						break;
					case "animator-fx":
						ObjectField.value = controllerExpressionInfo.AvatarDescriptor.baseAnimationLayers[4].animatorController;
						break;
				}
				
				ObjectField.SetEnabled(ObjectField.value != null);
			}

			private void UpdateActiveAnimator(ExpressionInfo obj)
			{
				if (ObjectField != null)
				{
					ObjectField.value = obj.Controller;
				}
			}

			private void OnClicked() => ExpressionInfo.Controller = ObjectField.value as AnimatorController;

			private ObjectField ObjectField { get; }
			private Button Button { get; }
			
			private ExpressionInfo ExpressionInfo { get; }
			
			private void CleanObjectField(ObjectField ob)
			{
				ob.Q(null, "unity-object-field__selector").style.display = DisplayStyle.None;
				// ob.Q(null, "unity-label").style.display = DisplayStyle.None;
				ob.Q(null, "unity-object-field-display__label").style.display = DisplayStyle.Flex;
			}
		}
	}
}