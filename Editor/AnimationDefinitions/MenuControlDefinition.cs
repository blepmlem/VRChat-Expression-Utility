using System.Collections.Generic;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace ExpressionUtility
{
	internal class MenuControlDefinition : IAnimationDefinition
	{
		public MenuControlDefinition(IAnimationDefinition parent, VRCExpressionsMenu.Control control)
		{
			Name = control.name;
			Parents.Add(parent);
			Control = control;
			Type = control.type;

			if (Type == VRCExpressionsMenu.Control.ControlType.SubMenu)
			{
				AddMenu(control.subMenu);
				return;
			}

			MainParameter = AddParameter(control.parameter);
			foreach (var controlSubParameter in control.subParameters)
			{
				SubParameters.Add(AddParameter(controlSubParameter));
			}
		}
		
		public ParameterDefinition MainParameter { get; }
		
		public List<ParameterDefinition> SubParameters { get; } = new List<ParameterDefinition>();
		public VRCExpressionsMenu.Control.ControlType Type { get; set; }

		public MenuControlDefinition(IAnimationDefinition parent, string name = null)
		{
			Name = name ?? parent.Name;
			Parents.Add(parent);
		}

		public MenuDefinition AddMenu(string name = null)
		{
			return Children.AddChild(new MenuDefinition(this, name));
		}

		public MenuDefinition AddMenu(VRCExpressionsMenu menu)
		{
			return Children.AddChild(new MenuDefinition(this, menu));
		}
		
		public ParameterDefinition AddParameter(ParameterDefinition.ParameterType type, string name = null)
		{
			return Children.AddChild(new ParameterDefinition(this, name, type));
		}

		public ParameterDefinition AddParameter(VRCExpressionsMenu.Control.Parameter  parameter)
		{
			return Children.AddChild(new ParameterDefinition(this, parameter));
		}
		
		public VRCExpressionsMenu.Control Control { get; }
		public string Name { get; }
		public bool IsRealized => Control != null;
		public List<IAnimationDefinition> Children { get; } = new List<IAnimationDefinition>();
		public List<IAnimationDefinition> Parents { get; } = new List<IAnimationDefinition>();
				
		public override string ToString() => $"[{GetType().Name}] {Name}";
	}
}