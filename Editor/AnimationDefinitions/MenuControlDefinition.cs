using System.Collections.Generic;
using UnityEditor;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace ExpressionUtility
{
	internal class MenuControlDefinition : IAnimationDefinition
	{
		public MenuControlDefinition(string name, VRCExpressionsMenu.Control.ControlType type)
		{
			Name = name;
			Type = type;
		}
		
		public MenuControlDefinition(VRCExpressionsMenu.Control control) : this(control.name, control.type)
		{
			Control = control;
			
			if (Type == VRCExpressionsMenu.Control.ControlType.SubMenu)
			{
				this.AddChild(new MenuDefinition(control.subMenu));
				return;
			}

			MainParameter = this.AddChild(new ParameterDefinition(control.parameter.name, $"{nameof(MenuControlDefinition)}: Main Parameter"));
			foreach (var controlSubParameter in control.subParameters)
			{
				SubParameters.Add(this.AddChild(new ParameterDefinition(controlSubParameter.name, $"{nameof(MenuControlDefinition)}: Sub-parameter")));
			}
		}
		
		public ParameterDefinition MainParameter { get; }
		public List<ParameterDefinition> SubParameters { get; } = new List<ParameterDefinition>();
		public VRCExpressionsMenu.Control.ControlType Type { get; set; }
		public VRCExpressionsMenu.Control Control { get; }
		public string Name { get; }
		public bool IsRealized => Control != null;
		public void DeleteSelf()
		{
			if (Control != null && this.FindAncestor<MenuDefinition>() is var menuDefinition && (menuDefinition?.IsRealized ?? false))
			{
				var menu = menuDefinition.Menu;
				Undo.RecordObject(menu, $"Remove menu control {Name}");
				menuDefinition.Menu.controls.Remove(Control);
				EditorUtility.SetDirty(menu);
			}
		}

		public List<IAnimationDefinition> Children { get; } = new List<IAnimationDefinition>();
		public IAnimationDefinition Parent { get; set; }
				
		public override string ToString() => $"{Name} [{ObjectNames.NicifyVariableName(Type.ToString())}] (Menu Control)";
	}
}