using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace ExpressionUtility
{
	internal class MenuControlDefinition : IAnimationDefinition, IRealizable<VRCExpressionsMenu.Control>
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

			MainParameter = this.AddChild(new ParameterDefinition(control.parameter.name, $"{nameof(MainParameter)}"));
			foreach (var controlSubParameter in control.subParameters)
			{
				SubParameters.Add(this.AddChild(new ParameterDefinition(controlSubParameter.name, $"{nameof(SubParameters)}")));
			}
		}

		public ParameterDefinition MainParameter { get; }
		public List<ParameterDefinition> SubParameters { get; } = new List<ParameterDefinition>();
		public VRCExpressionsMenu.Control.ControlType Type { get; set; }
		
		public VRCExpressionsMenu.Control Control { get; private set; }
		public string Name { get; private set; }
		
		public VRCExpressionsMenu.Control RealizeSelf(DirectoryInfo creationDirectory)
		{
			if (!IsRealized)
			{
				Control = new VRCExpressionsMenu.Control
				{
					name = Name,
					type = Type,
				};
			}
			
			var subMenu = Children.OfType<IRealizable<VRCExpressionsMenu>>().FirstOrDefault();
			if (subMenu != null)
			{
				Control.subMenu = subMenu.RealizeSelf(creationDirectory);
			}

			if (MainParameter != null)
			{
				Control.parameter = new VRCExpressionsMenu.Control.Parameter
				{
					name = MainParameter.Name,
				};
			}

			Control.subParameters = SubParameters.Select(s => new VRCExpressionsMenu.Control.Parameter {name = s.Name}).ToArray();

			return Control;
		}

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