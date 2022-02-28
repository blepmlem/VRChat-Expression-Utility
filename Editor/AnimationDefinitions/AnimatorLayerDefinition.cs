using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ExpressionUtility
{
	internal class AnimatorLayerDefinition : IAnimationDefinition, IRealizable<AnimatorControllerLayer>
	{
		public AnimatorLayerDefinition(string name)
		{
			Name = name;
		}

		public AnimatorLayerDefinition(AnimatorControllerLayer controllerLayer) : this(controllerLayer.name)
		{
			Layer = controllerLayer;
			this.AddChild(new StateMachineDefinition(controllerLayer.stateMachine));
		}
		
		public string Name { get; }
		public bool IsRealized => Layer != null;
		public bool IsRealizedRecursive => this.IsRealizedRecursive();

		public void DeleteSelf()
		{
			if (IsRealized && Parent is AnimatorDefinition animDef && animDef.Animator != null)
			{
				Undo.RecordObject(animDef.Animator, "Delete Layer");
				animDef.Animator.RemoveLayer(Layer);
			}
		}

		public List<IAnimationDefinition> Children { get; } = new List<IAnimationDefinition>();
		public IAnimationDefinition Parent { get; set; }
		public AnimatorControllerLayer RealizeSelf()
		{
			if (!IsRealized)
			{
				Layer = new AnimatorControllerLayer
				{
					name = Name,
					defaultWeight = 1f,
				};
			}
			
			var stateMachine = Children.OfType<IRealizable<AnimatorStateMachine>>().FirstOrDefault()?.RealizeSelf();

			Layer.stateMachine = stateMachine;
			
			return Layer;
		}

		public override string ToString() => $"{Name} (Animator Layer)";
		public AnimatorControllerLayer Layer { get; private set; }
	}
}