/*
 * Originally based on the InstallChecker from the Kethane mod for Kerbal Space Program.
 * This version is based off taraniselsu's version from https://github.com/Majiir/Kethane/blob/b93b1171ec42b4be6c44b257ad31c7efd7ea1702/Plugin/InstallChecker.cs
 * 
 * Original is (C) Copyright Majiir.
 * CC0 Public Domain (http://creativecommons.org/publicdomain/zero/1.0/)
 * http://forum.kerbalspaceprogram.com/threads/65395-CompatibilityChecker-Discussion-Thread?p=899895&viewfull=1#post899895
 * 
 * This file has been modified extensively and is released under the same license.
 */

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace AutoAsparagus
{
	// Be sure to target .NET 3.5 or you'll get some bogus error on startup!

	[KSPAddon(KSPAddon.Startup.MainMenu, true)]
	internal class ASPInstallChecker : MonoBehaviour
	{
		protected void Start()
		{
			const string modName = "AutoAsparagus";
			const string expectedPath = "AutoAsparagus";
			// Search for this mod's DLL existing in the wrong location. This will also detect duplicate copies because only one can be in the right place.
			var assemblies = AssemblyLoader.loadedAssemblies.Where (a => a.assembly.GetName ().Name == Assembly.GetExecutingAssembly ().GetName ().Name).Where (a => a.url != expectedPath);

			if (assemblies.Any()) {
				var badPaths = assemblies.Select(a => a.path).Select(
					p => Uri.UnescapeDataString(new Uri(Path.GetFullPath(KSPUtil.ApplicationRootPath)).MakeRelativeUri(new Uri(p)).ToString().Replace('/', Path.DirectorySeparatorChar))
				);
				PopupDialog.SpawnPopupDialog("Incorrect " + modName + " Installation",
				    modName + " has been installed incorrectly and will not function properly. All files should be located in KSP/GameData/" + 
					expectedPath + ". Do not move any files from inside that folder.\n\nIncorrect path(s):\n" +
					String.Join("\n", badPaths.ToArray()),
					"OK", false, HighLogic.Skin);
			}
		}
	}
}

