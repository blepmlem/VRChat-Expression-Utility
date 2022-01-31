using System;
using System.Collections.Generic;
using System.Linq;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace ExpressionUtility
{
	internal class MenuDefinition : IAnimationDefinition
	{
		public MenuDefinition(IAnimationDefinition parent, VRCExpressionsMenu menu)
		{
			Name = menu.name;
			Parents.Add(parent);
			Menu = menu;
			foreach (VRCExpressionsMenu.Control menuControl in menu.controls)
			{
				AddControl(menuControl);
			}
		}

		public MenuDefinition(IAnimationDefinition parent, string name = null)
		{
			Name = name ?? parent.Name;
			Parents.Add(parent);
		}

		public MenuControlDefinition AddControl(string name = null)
		{
			return Children.AddChild(new MenuControlDefinition(this, name));
		}
		
		public MenuControlDefinition AddControl(VRCExpressionsMenu.Control control)
		{
			return Children.AddChild(new MenuControlDefinition(this, control));
		}
		
		public VRCExpressionsMenu Menu { get; }
		public string Name { get; }
		public bool IsRealized => Menu != null;
		public List<IAnimationDefinition> Children { get; } = new List<IAnimationDefinition>();
		public List<IAnimationDefinition> Parents { get; } = new List<IAnimationDefinition>();
		
		public override string ToString() => $"[{GetType().Name}] {Name}";
	}
}