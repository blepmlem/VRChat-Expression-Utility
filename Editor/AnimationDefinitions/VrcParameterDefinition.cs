using System;
using System.IO;
using System.Linq;
using UnityEditor;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace ExpressionUtility
{
	internal class VrcParameterDefinition : ParameterDefinition, IRealizable<VRCExpressionParameters.Parameter>
	{
		public VrcParameterDefinition(string name, ParameterValueType type) : base(name, nameof(VrcParameterDefinition))
		{
			Type = type;
		}

		public VrcParameterDefinition(VRCExpressionParameters.Parameter parameter) : this(parameter.name, GetParameterType(parameter))
		{
			Parameter = parameter;
		}
		
		public ParameterValueType Type { get; }

		public VRCExpressionParameters.Parameter RealizeSelf(DirectoryInfo creationDirectory)
		{
			if (Parameters == null)
			{
				throw new NullReferenceException("VRC Parameters list is null");
			}
			
			if (!IsRealized)
			{
				Parameter = new VRCExpressionParameters.Parameter()
				{
					name = Name,
					valueType = GetParameterType(Type),
					saved = true,
				};
				
				Undo.RecordObject(Parameters, $"Add parameter {Name}");
				var list = Parameters.parameters.ToList();
				list.Add(Parameter);
				Parameters.parameters = list.ToArray();
				EditorUtility.SetDirty(Parameters);
			}

			return Parameter;
		}

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

		private static VRCExpressionParameters.ValueType GetParameterType(ParameterValueType parameter)
		{
			switch (parameter)
			{
				case ParameterValueType.Float: return VRCExpressionParameters.ValueType.Float;
				case ParameterValueType.Int: return VRCExpressionParameters.ValueType.Int;
				case ParameterValueType.Bool: return VRCExpressionParameters.ValueType.Bool;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
		
		private static ParameterValueType GetParameterType(VRCExpressionParameters.Parameter parameter)
		{
			switch (parameter.valueType)
			{
				case VRCExpressionParameters.ValueType.Float: return ParameterValueType.Float;
				case VRCExpressionParameters.ValueType.Int: return ParameterValueType.Int;
				case VRCExpressionParameters.ValueType.Bool: return ParameterValueType.Bool;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public VRCExpressionParameters.Parameter Parameter { get; private set; }
		
		public VRCExpressionParameters Parameters => (Parent as AvatarDefinition)?.VrcExpressionParameters;

		public override string ToString() => $"{Name} [{Type}] (VRC Parameter)";
	}
}