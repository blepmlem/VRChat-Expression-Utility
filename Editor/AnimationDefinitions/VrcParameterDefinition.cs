using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace ExpressionUtility
{
	internal class VrcParameterDefinition : ParameterDefinition, IAnimationDefinition
	{
		public VrcParameterDefinition(AvatarDefinition parent, string name, ParameterValueType type) : base(parent, name)
		{
			Parent = parent;
			Parameters = parent.VrcExpressionParameters;
			Type = type;
			Name = name;
		}
		
		public ParameterValueType Type { get; }

		public string Name { get; }

		public bool IsRealized => Parameters.NotNull()?.parameters.Any(p => p.name == Name) ?? false;

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

		public VRCExpressionParameters Parameters { get; }

		public List<IAnimationDefinition> Children => new List<IAnimationDefinition>();

		public IAnimationDefinition Parent { get; }

		public override string ToString() => $"{Name} [{Type}] (VRC Parameter)";
	}
}