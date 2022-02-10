using System.Collections.Generic;
using UnityEditor;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace ExpressionUtility
{
	internal class MenuControlDefinition : IAnimationDefinition
	{
		public MenuControlDefinition(IAnimationDefinition parent, VRCExpressionsMenu.Control control)
		{
			Name = control.name;
			Parent = parent;
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
			Parent = parent;
		}
		
		public MenuDefinition AddMenu(VRCExpressionsMenu menu)
		{
			return Children.AddChild(new MenuDefinition(this, menu));
		}
		
		public ParameterDefinition AddParameter(VRCExpressionsMenu.Control.Parameter  parameter)
		{
			return Children.AddChild(new ParameterDefinition(this, parameter.name));
		}
		
		public VRCExpressionsMenu.Control Control { get; }
		public string Name { get; }
		public bool IsRealized => Control != null;
		public void DeleteSelf()
		{
			if (Control != null && this.TryGetFirstParent<MenuDefinition>(out var menuDefinition) && menuDefinition.IsRealized)
			{
				var menu = menuDefinition.Menu;
				Undo.RecordObject(menu, $"Remove menu control {Name}");
				menuDefinition.Menu.controls.Remove(Control);
				EditorUtility.SetDirty(menu);
			}
		}

		public List<IAnimationDefinition> Children { get; } = new List<IAnimationDefinition>();
		public IAnimationDefinition Parent { get; }
				
		public override string ToString() => $"{Name} [{ObjectNames.NicifyVariableName(Type.ToString())}] (Menu Control)";
	}
}