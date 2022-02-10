using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using VRC.SDK3.Avatars.Components;

namespace ExpressionUtility
{
	internal class VrcParameterDriverDefinition : IAnimationDefinition
	{
		public VrcParameterDriverDefinition(StateDefinition parent, VRCAvatarParameterDriver driver)
		{
			Name = parent.Name;
			Parent = parent;
			Behaviour = driver;
			
			foreach (var driverParameter in driver.parameters)
			{
				AddParameter(driverParameter.name, nameof(VRCAvatarParameterDriver), true);
			}
		}

		public ParameterDefinition AddParameter(string parameter, string label, bool isRealized)
		{
			return Children.AddChild(new ParameterDefinition(this, parameter, label) {IsRealized = isRealized});
		}
		
		public VRCAvatarParameterDriver Behaviour { get; }
		public string Name { get; }
		public bool IsRealized => Behaviour != null;
		public void DeleteSelf()
		{
			if (IsRealized && Parent.IsRealized && Parent is StateDefinition stateDefinition)
			{
				Undo.RegisterCompleteObjectUndo(stateDefinition.State, $"Remove behaviour: {Behaviour.name}");
				var behaviours = stateDefinition.State.behaviours.ToList();
				behaviours.Remove(Behaviour);
				stateDefinition.State.behaviours = behaviours.ToArray();
				Undo.DestroyObjectImmediate(Behaviour);
				EditorUtility.SetDirty(stateDefinition.State);
			}
		}

		public List<IAnimationDefinition> Children { get; } = new List<IAnimationDefinition>();
		public IAnimationDefinition Parent { get; }
		public override string ToString()
		{
			return $"{Name} (Parameter Driver)";
		}
	}
}