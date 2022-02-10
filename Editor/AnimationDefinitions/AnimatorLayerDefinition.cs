using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ExpressionUtility
{
	internal class AnimatorLayerDefinition : IAnimationDefinition
	{
		public AnimatorLayerDefinition(AnimatorDefinition parent, string name = null)
		{
			Name = name ?? parent.Name;
			Parent = parent;
		}

		public AnimatorLayerDefinition(AnimatorDefinition parent, AnimatorControllerLayer controllerLayer)
		{
			Layer = controllerLayer;
			Name = controllerLayer.name;
			Parent = parent;
			
			AddStateMachine(controllerLayer.stateMachine);
		}

		public StateMachineDefinition AddStateMachine(string name = null)
		{
			return Children.AddChild(new StateMachineDefinition(this, name));
		}

		public StateMachineDefinition AddStateMachine(AnimatorStateMachine stateMachine)
		{
			return Children.AddChild(new StateMachineDefinition(this, stateMachine));
		}


		public string Name { get; }
		public bool IsRealized => Layer != null;

		public void DeleteSelf()
		{
			if (IsRealized && Parent is AnimatorDefinition animDef && animDef.Animator != null)
			{
				Undo.RecordObject(animDef.Animator, "Delete Layer");
				animDef.Animator.RemoveLayer(Layer);
			}
		}


		public List<IAnimationDefinition> Children { get; } = new List<IAnimationDefinition>();
		public IAnimationDefinition Parent { get; }

		public override string ToString() => $"{Name} (Animator Layer)";
		public AnimatorControllerLayer Layer { get; }
	}
}