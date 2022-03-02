using UnityEditor;

namespace ExpressionUtility
{
	public static class Settings
	{
		private const string CHECK_UPDATES = "expression-ui-check-updates";
		private const string SKIP_INTRO_PAGE = "expression-ui-skip-intro";
		private const string CONNECT_VRC_API = "expression-ui-connect-vrc";
		
		public static bool AllowCheckForUpdates
		{
			get => EditorPrefs.GetBool(CHECK_UPDATES, true);
			set => EditorPrefs.SetBool(CHECK_UPDATES, value);
		}

		
		public static bool SkipIntroPage
		{
			get => EditorPrefs.GetBool(SKIP_INTRO_PAGE, false);
			set => EditorPrefs.SetBool(SKIP_INTRO_PAGE, value);
		}
		
		public static bool AllowConnectToVrcApi
		{
			get => EditorPrefs.GetBool(CONNECT_VRC_API, false);
			set => EditorPrefs.SetBool(CONNECT_VRC_API, value);
		}
	}
}