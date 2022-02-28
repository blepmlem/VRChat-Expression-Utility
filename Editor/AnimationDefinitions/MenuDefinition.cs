using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace ExpressionUtility
{
	internal class MenuDefinition : IAnimationDefinition
	{
		public MenuDefinition(VRCExpressionsMenu menu) : this(menu.name)
		{
			Menu = menu;
			foreach (VRCExpressionsMenu.Control menuControl in menu.controls)
			{
				this.AddChild(new MenuControlDefinition(menuControl));
			}
		}

		public MenuDefinition(string name)
		{
			Name = name;
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
		public IAnimationDefinition Parent { get; set; }
		
		public override string ToString() => $"{Name} (Menu)";
	}
}