// Toony Colors Pro+Mobile 2
// (c) 2014-2023 Jean Moreno

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using ToonyColorsPro.Utilities;

// Menu Options for Toony Colors Pro 2

namespace ToonyColorsPro
{
	public static class Menu
	{
		//Change this path if you want the Toony Colors Pro menu to appear elsewhere in the menu bar
		public const string MENU_PATH = @"Tools/Toony Colors Pro/";

		//--------------------------------------------------------------------------------------------------
		// DOCUMENTATION

		[MenuItem(MENU_PATH + "Documentation", false, 100)]
		static void OpenDocumentation()
		{
			TCP2_GUI.OpenHelp();
		}
	}
}