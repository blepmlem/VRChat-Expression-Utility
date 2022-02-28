using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ExpressionUtility
{
	internal class AnimatorParameterDefinition : ParameterDefinition, IRealizable<AnimatorControllerParameter>
	{
		public AnimatorParameterDefinition(string name, ParameterValueType type) : base(name)
		{
			Type = type;
		}

		public AnimatorParameterDefinition(AnimatorControllerParameter parameter) : this(parameter.name, GetParameterType(parameter))
		{
		}

		public ParameterValueType Type { get; }

		public AnimatorControllerParameter RealizeSelf()
		{
			if (IsRealized)
			{
				return Parameter;
			}

			return new AnimatorControllerParameter
			{
				name = Name,
				type = GetParameterType(Type),
			};
		}

		public override bool IsRealized => Parameter != null;

		public bool IsRealizedRecursive => this.IsRealizedRecursive();

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
		
		private static AnimatorControllerParameterType GetParameterType(ParameterValueType parameter)
		{
			switch (parameter)
			{
				case ParameterValueType.Float:
					return AnimatorControllerParameterType.Float;
				case ParameterValueType.Int:
					return AnimatorControllerParameterType.Int;
				case ParameterValueType.Bool:
					return AnimatorControllerParameterType.Bool;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public override string ToString() => $"{Name} [{Type}] (Animator Parameter)";
	}
}