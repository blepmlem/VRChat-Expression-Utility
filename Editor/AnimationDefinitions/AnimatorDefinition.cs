using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ExpressionUtility
{
	internal class AnimatorDefinition : IAnimationDefinition, IRealizable<AnimatorController>
	{
		public AnimatorController Animator { get; private set; }

		public AnimatorDefinition(AnimatorType type, string name)
		{
			Name = name;
			Type = type;
		}

		public AnimatorDefinition(AnimatorController animator, AnimatorType type) : this(type, animator.name)
		{
			Animator = animator;
			foreach (AnimatorControllerLayer animatorControllerLayer in animator.layers)
			{
				this.AddChild(new AnimatorLayerDefinition(animatorControllerLayer));
			}
			foreach (var parameter in animator.parameters)
			{
				this.AddChild(new AnimatorParameterDefinition(parameter));
			}
		}

		public string Name { get; }
		public AnimatorController RealizeSelf()
		{
			if (!IsRealized)
			{
				Animator = new AnimatorController();
			}
			
			foreach (var realizable in Children.OfType<IRealizable<AnimatorControllerLayer>>())
			{
				var layer = realizable.RealizeSelf();
				
				if(Animator.layers.Any(l => l.name == layer.name))
				{
					continue;
				}
				
				Animator.AddLayer(layer);
			}

			foreach (var realizable in Children.OfType<IRealizable<AnimatorControllerParameter>>())
			{
				var parameter = realizable.RealizeSelf();
				if (Animator.parameters.Any(p => p.name == parameter.name))
				{
					continue;
				}
				
				Animator.AddParameter(parameter);;
			}
			
			return Animator;
		}

		public bool IsRealized => Animator != null;
		public bool IsRealizedRecursive => this.IsRealizedRecursive();
		public AnimatorType Type { get; }
		public void DeleteSelf()
		{
			if (Animator != null)
			{
				Undo.DestroyObjectImmediate(Animator);
			}
		}

		public IEnumerable<AnimatorParameterDefinition> ParameterDefinitions => Children.OfType<AnimatorParameterDefinition>();

		public List<IAnimationDefinition> Children { get; } = new List<IAnimationDefinition>();
		public IAnimationDefinition Parent { get; set; }
		
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