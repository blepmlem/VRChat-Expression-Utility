using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;

namespace ExpressionUtility
{
	internal class AnimatorDefinition : IAnimationDefinition
	{
		public AnimatorController Animator { get; }

		public AnimatorDefinition(IAnimationDefinition parent, AnimatorType type, string name = null)
		{
			Name = name ?? type.ToString();
			Parents.Add(parent);
		}

		public AnimatorDefinition(IAnimationDefinition parent, AnimatorController animator, AnimatorType type)
		{
			bool animatorIsNull = animator == null;
			Parents.Add(parent);
			Animator = animator;
			Name = animatorIsNull ? type.ToString() : animator.name;
			Type = type;
			
			if (animatorIsNull)
			{
				return;
			}
			
			foreach (AnimatorControllerLayer animatorControllerLayer in animator.layers)
			{
				AddLayer(animatorControllerLayer);
			}
			foreach (var parameter in animator.parameters)
			{
				AddParameter(parameter);
			}
		}

		public AnimatorLayerDefinition AddLayer(string name = null)
		{
			return Children.AddChild(new AnimatorLayerDefinition(this, name));
		}

		public AnimatorLayerDefinition AddLayer(AnimatorControllerLayer controllerLayer)
		{
			return Children.AddChild(new AnimatorLayerDefinition(this, controllerLayer));
		}

		public ParameterDefinition AddParameter(ParameterDefinition.ParameterType type, string name = null)
		{
			return Children.AddChild(new ParameterDefinition(this, name, type));
		}

		public ParameterDefinition AddParameter(AnimatorControllerParameter parameter)
		{
			return Children.AddChild(new ParameterDefinition(this, parameter));
		}

		public string Name { get; }
		public bool IsRealized => Animator != null;
		public AnimatorType Type { get; set; }
		public List<IAnimationDefinition> Children { get; } = new List<IAnimationDefinition>();
		public List<IAnimationDefinition> Parents { get; } = new List<IAnimationDefinition>();
		
		public enum AnimatorType
		{
			Base,
			Additive,
			Gesture,
			Action,
			FX,
		}
		
		public override string ToString() => $"[{GetType().Name}] {Name}";
	}
}