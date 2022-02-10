using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ExpressionUtility
{
	internal class AnimatorParameterDefinition : ParameterDefinition, IAnimationDefinition
	{
		public AnimatorParameterDefinition(AnimatorDefinition parent, string name, ParameterValueType type) : base(parent, name)
		{
			Parent = parent;
			Type = type;
			Name = name;
		}

		public AnimatorParameterDefinition(AnimatorDefinition parent, AnimatorControllerParameter parameter) : this(parent, parameter.name, GetParameterType(parameter))
		{
		}

		public ParameterValueType Type { get; }

		public string Name { get; }

		public bool IsRealized => Parameter != null;

		private AnimatorControllerParameter Parameter => ParentAnimator?.Animator.parameters.FirstOrDefault(p => p.name == Name);
		
		private AnimatorDefinition ParentAnimator => Parent as AnimatorDefinition;

		public override void DeleteSelf()
		{
			if (IsRealized)
			{
				var animator = ParentAnimator.Animator;
				Undo.RecordObject(animator, $"Remove parameter {Name}");
				animator.RemoveParameter(Parameter);
				EditorUtility.SetDirty(animator);
			}
		}
		
		private static ParameterValueType GetParameterType(AnimatorControllerParameter parameter)
		{
			switch (parameter.type)
			{
				case AnimatorControllerParameterType.Float:
					return ParameterValueType.Float;
				case AnimatorControllerParameterType.Int:
					return ParameterValueType.Int;
				case AnimatorControllerParameterType.Bool:
				case AnimatorControllerParameterType.Trigger:
					return ParameterValueType.Bool;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
		
		public List<IAnimationDefinition> Children => new List<IAnimationDefinition>();

		public IAnimationDefinition Parent { get; }

		public override string ToString() => $"{Name} [{Type}] (Animator Parameter)";
	}
}