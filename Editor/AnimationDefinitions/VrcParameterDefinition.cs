using System;
using System.Linq;
using UnityEditor;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace ExpressionUtility
{
	internal class VrcParameterDefinition : ParameterDefinition
	{
		public VrcParameterDefinition(string name, ParameterValueType type) : base(name, nameof(VrcParameterDefinition))
		{
			Type = type;
		}

		public VrcParameterDefinition(VRCExpressionParameters.Parameter parameter) : this(parameter.name, GetParameterType(parameter))
		{
			
		}
		
		public ParameterValueType Type { get; }

		public override bool IsRealized => Parameters.NotNull()?.parameters.Any(p => p.name == Name) ?? false;

		public override void DeleteSelf()
		{
			if (IsRealized)
			{
				Undo.RecordObject(Parameters, $"Remove parameter {Name}");
				var list = Parameters.parameters.ToList();
				var parameter = Parameters.FindParameter(Name);
				list.Remove(parameter);
				Parameters.parameters = list.ToArray();
				EditorUtility.SetDirty(Parameters);
			}
		}

		private static ParameterValueType GetParameterType(VRCExpressionParameters.Parameter parameter)
		{
			switch (parameter.valueType)
			{
				case VRCExpressionParameters.ValueType.Float:
					return ParameterValueType.Float;
				case VRCExpressionParameters.ValueType.Int:
					return ParameterValueType.Int;
				case VRCExpressionParameters.ValueType.Bool:
					return ParameterValueType.Bool;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
		
		public VRCExpressionParameters Parameters => (Parent as AvatarDefinition)?.VrcExpressionParameters;

		public override string ToString() => $"{Name} [{Type}] (VRC Parameter)";
	}
}