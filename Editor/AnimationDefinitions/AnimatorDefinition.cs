using System.Collections.Generic;
using System.Linq;
using UnityEditor;
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
			Parent = parent;
		}

		public AnimatorDefinition(IAnimationDefinition parent, AnimatorController animator, AnimatorType type)
		{
			bool animatorIsNull = animator == null;
			Parent = parent;
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
		
		public AnimatorLayerDefinition AddLayer(AnimatorControllerLayer controllerLayer)
		{
			return Children.AddChild(new AnimatorLayerDefinition(this, controllerLayer));
		}
		
		public AnimatorParameterDefinition AddParameter(AnimatorControllerParameter parameter)
		{
			return Children.AddChild(new AnimatorParameterDefinition(this, parameter));
		}

		public string Name { get; }
		public bool IsRealized => Animator != null;
		public AnimatorType Type { get; set; }
		public void DeleteSelf()
		{
			if (Animator != null)
			{
				Undo.DestroyObjectImmediate(Animator);
			}
		}

		public IEnumerable<AnimatorParameterDefinition> ParameterDefinitions => Children.OfType<AnimatorParameterDefinition>();

		public List<IAnimationDefinition> Children { get; } = new List<IAnimationDefinition>();
		public IAnimationDefinition Parent { get; }
		
		public enum AnimatorType
		{
			Base,
			Additive,
			Gesture,
			Action,
			FX,
		}
		
		public override string ToString() => $"{Name} (Animator)";
	}
}