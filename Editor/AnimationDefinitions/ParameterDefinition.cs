using System;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace ExpressionUtility
{
	internal class ParameterDefinition : IAnimationDefinition
	{
		public enum ParameterType
		{
			Int,
			Float,
			Bool,
		}
		
		public ParameterDefinition(IAnimationDefinition parent, string name, ParameterType type)
		{
			Name = name ?? parent.Name;
			Parents.Add(parent);
		}
		
		public ParameterDefinition(IAnimationDefinition parent, AnimatorControllerParameter parameter)
		{
			Name = parameter.name;
			Parents.Add(parent);
			Type = GetParameterType(parameter);
			IsRealized = true;
		}
		
		public ParameterDefinition(IAnimationDefinition parent, VRCExpressionParameters.Parameter parameter)
		{
			Name = parameter.name;
			Parents.Add(parent);
			Type = GetParameterType(parameter);
			IsRealized = true;
		}

		public ParameterDefinition(IAnimationDefinition parent, VRCExpressionsMenu.Control.Parameter parameter)
		{
			Name = parameter.name;
			Parents.Add(parent);
			IsRealized = true;
		}

		public ParameterDefinition(ConditionDefinition parent, AnimatorConditionMode parameter, string name)
		{
			Name = name;
			Parents.Add(parent);
			Type = GetParameterType(parameter);
			IsRealized = true;
		}
		
		private ParameterType GetParameterType(AnimatorConditionMode condition)
		{
			switch (condition)
			{
				case AnimatorConditionMode.If:
				case AnimatorConditionMode.IfNot:
					return ParameterType.Bool;
				case AnimatorConditionMode.Greater:
				case AnimatorConditionMode.Less:
				case AnimatorConditionMode.Equals:
				case AnimatorConditionMode.NotEqual:
					return ParameterType.Float;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
		
		private ParameterType GetParameterType(AnimatorControllerParameter parameter)
		{
			switch (parameter.type)
			{
				case AnimatorControllerParameterType.Float:
					return ParameterType.Float;
				case AnimatorControllerParameterType.Int:
					return ParameterType.Int;
				case AnimatorControllerParameterType.Bool:
				case AnimatorControllerParameterType.Trigger:
					return ParameterType.Bool;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private ParameterType GetParameterType(VRCExpressionParameters.Parameter parameter)
		{
			switch (parameter.valueType)
			{
				case VRCExpressionParameters.ValueType.Float:
					return ParameterType.Float;
				case VRCExpressionParameters.ValueType.Int:
					return ParameterType.Int;
				case VRCExpressionParameters.ValueType.Bool:
					return ParameterType.Bool;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
		
		public ParameterType Type { get; }
		public string Name { get; }
		public bool IsRealized { get; set; }
		public List<IAnimationDefinition> Children { get; } = new List<IAnimationDefinition>();
		public List<IAnimationDefinition> Parents { get; } = new List<IAnimationDefinition>();

		public override string ToString() => $"[{GetType().Name}] {Name}";
	}
}