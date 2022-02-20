using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ExpressionUtility.UI
{
	internal class ObjectHolder : ScriptableObject
	{
		private void SetupDeletion(Button deleteButton, ObjectField field, IAnimationDefinition def,  AvatarParameterData data)
		{
			string deleteText = "x";
			string undeleteText = "+";
			deleteButton.text = deleteText;
			void DeleteClicked()
			{
				var noDelete = !data.MarkForDeletion(this);
				field.SetEnabled(noDelete);
				deleteButton.AddToClassList("button--danger--green");
				deleteButton.text = undeleteText;
				if (noDelete)
				{
					deleteButton.text = deleteText;
					deleteButton.RemoveFromClassList("button--danger--green");
					data.UnMarkForDeletion(def);
				}
			}
			deleteButton.clicked += DeleteClicked;
		}

		public static VisualElement CreateHolder(VrcParameterDefinition def, AvatarParameterData data)
		{
			var avatar = def.FindAncestor<AvatarDefinition>();
			var fields = CreateHolder(() => Selection.activeObject = avatar.VrcExpressionParameters, $"{def.Name} ({def.Type})", def);
			fields.SetupDeletion(fields.DeleteButton, fields.ObjectField, def, data);
			
			return fields.Container;
		}

		public static VisualElement CreateHolder(AnimatorLayerDefinition def, AvatarParameterData data)
		{
			var animDef = def.FindAncestor<AnimatorDefinition>();
			var fields = CreateHolder(() => animDef.Animator.SelectAnimatorLayer(def.Layer), $"{animDef.Name}/{def.Layer.name}", def);
			fields.SetupDeletion(fields.DeleteButton, fields.ObjectField, def, data);
			return fields.Container;
		}

		public static VisualElement CreateHolder(MotionDefinition def, AvatarParameterData data)
		{
			void Action()
			{
				var layer = def.FindAncestors<AnimatorLayerDefinition>().FirstOrDefault();
				var animator = def.FindAncestors<AnimatorDefinition>().FirstOrDefault();
				if((layer?.IsRealized ?? false) && (animator?.IsRealized ?? false))
				{
					animator.Animator.SelectAnimatorLayer(layer.Layer);
					EditorApplication.delayCall += () => Selection.activeObject = def.Motion;
				}
			}
			
			var fields = CreateHolder(Action, $"{def}", def);
			fields.SetupDeletion(fields.DeleteButton, fields.ObjectField, def, data);
			return fields.Container;
		}
		
		public static VisualElement CreateHolder(VrcParameterDriverDefinition def, AvatarParameterData data)
		{
			void Action()
			{
				var layer = def.FindAncestors<AnimatorLayerDefinition>().FirstOrDefault();
				var animator = def.FindAncestors<AnimatorDefinition>().FirstOrDefault();
				if((layer?.IsRealized ?? false) && (animator?.IsRealized ?? false) && def.Parent is StateDefinition state)
				{
					animator.Animator.SelectAnimatorLayer(layer.Layer);
					EditorApplication.delayCall += () => Selection.activeObject = state.State;
				}
			}

			var fields = CreateHolder(Action, $"{def}", def);
			fields.SetupDeletion(fields.DeleteButton, fields.ObjectField, def, data);
			return fields.Container;
		}
		
		public static VisualElement CreateHolder(MenuControlDefinition def, AvatarParameterData data)
		{
			var menu = def.FindAncestor<MenuDefinition>();
			var name = $"{menu.Menu.name}/{def.Name}";
			var fields = CreateHolder(() => Selection.activeObject = menu.Menu, name, def);
			fields.SetupDeletion(fields.DeleteButton, fields.ObjectField, def, data);
			return fields.Container;
		}


		private static ObjectHolder CreateHolder(Action selectionAction, string text, IAnimationDefinition def)
		{
			return CreateInstance<ObjectHolder>().Setup(selectionAction, text, def);
		}

		private ObjectHolder Setup(Action selectionAction, string text, IAnimationDefinition def)
		{
			Container = new VisualElement();
			ObjectField = new ObjectField();
			DeleteButton = new Button();
			AnimationDefinition = def;
			
			Container.Add(DeleteButton);
			Container.Add(ObjectField);
			Container.style.flexDirection = FlexDirection.Row;

			ObjectField.RemoveObjectSelector();
			ObjectField.AddToClassList("object-field--left-flushed");
			DeleteButton.AddToClassList("button--danger");
			
			name = $"{text}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 ";
			SelectionAction = selectionAction;

			void Callback(MouseDownEvent e)
			{
				e.StopImmediatePropagation();
				selectionAction?.Invoke();
			}

			ObjectField.RegisterCallback<MouseDownEvent>(Callback);
			ObjectField.value = this;
			return this;
		}

		public IAnimationDefinition AnimationDefinition { get; private set; }

		public Button DeleteButton { get; private set; }

		public ObjectField ObjectField { get; private set; }

		public VisualElement Container { get; private set; }

		[CustomEditor(typeof(ObjectHolder))]
		class HolderInspector : Editor
		{
			public override void OnInspectorGUI() => ((ObjectHolder)target).SelectionAction?.Invoke();
		}

		public Action SelectionAction { get; private set; }
	}
}