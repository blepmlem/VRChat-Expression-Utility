using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace ExpressionUtility
{
	internal class MenuDefinition : IAnimationDefinition
	{
		public MenuDefinition(IAnimationDefinition parent, VRCExpressionsMenu menu)
		{
			Name = menu.name;
			Parent = parent;
			Menu = menu;
			foreach (VRCExpressionsMenu.Control menuControl in menu.controls)
			{
				AddControl(menuControl);
			}
		}

		public MenuDefinition(IAnimationDefinition parent, string name = null)
		{
			Name = name ?? parent.Name;
			Parent = parent;
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
		public void DeleteSelf()
		{
			if (Menu != null)
			{
				Undo.DestroyObjectImmediate(Menu);
			}
		}

		public List<IAnimationDefinition> Children { get; } = new List<IAnimationDefinition>();
		public IAnimationDefinition Parent { get; }
		
		public override string ToString() => $"{Name} (Menu)";
	}
}