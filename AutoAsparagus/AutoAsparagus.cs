// this define is only for development, remove it for production.
//#define KSPdev

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

// FIXME project-wide: change all parent/child checking to check grandparents/grandchildren if parent/child is a fuel tank (or anything except decoupler or fuel line)
// FIXME position fuel lines to avoid obstructions

namespace AutoAsparagus {

	[KSPAddon(KSPAddon.Startup.EditorVAB, false)]
	public class AutoAsparagus: MonoBehaviour
	{
		private static Rect windowRect = new Rect(0,200,50,50);

		// Called after the scene is loaded.
		public void Awake() {
			RenderingManager.AddToPostDrawQueue(0, OnDraw);
		}

		// Called after Awake()
		public void Start() {
		}

		private void OnDraw(){
			windowRect = GUILayout.Window(100, windowRect, OnWindow, "AutoAsparagus" );
		}

		private IEnumerator<object> reallyReloadShip(EditorLogic editor) {
			yield return null; // Wait for next frame.
			var shipCfg = editor.ship.SaveShip ();

			forRealReloadShip(editor, shipCfg);
		}

		private void forRealReloadShip(EditorLogic editor, ConfigNode shipCfg){
			// ship reloading routines are shamelessly stolen from SelectRoot: http://forum.kerbalspaceprogram.com/threads/43208-0-22-Oct17-SelectRoot-Set-a-new-root-part-0-22-fixes

			editor.ship.Parts.ForEach(p => UnityEngine.Object.Destroy(p.gameObject));
			editor.ship.Clear();

			ShipConstruction.ShipConfig = shipCfg;
			editor.ship.LoadShip(ShipConstruction.ShipConfig);
			EditorLogic.startPod = editor.ship.parts[0].localRoot;
			editor.SetHighlightRecursive(true, editor.ship);
			editor.SetBackup();
			var resetCrewFn = editor.GetType().GetMethod("ResetCrewAssignment", BindingFlags.NonPublic | BindingFlags.Instance);
			resetCrewFn.Invoke(editor, new object[] { ShipConstruction.ShipConfig }); // I'm sorry, Squad :/
			Staging.SortIcons();
		}

		private void ReloadShip(){
			var editor = EditorLogic.fetch;
			StartCoroutine (reallyReloadShip(editor));
		}

		// called every screen refresh
		private void OnWindow(int windowID){
			var scaling = false;

			GUIStyle buttonStyle = new GUIStyle();
			buttonStyle = new GUIStyle(GUI.skin.button);
			buttonStyle.wordWrap = false;
			buttonStyle.fontStyle = FontStyle.Normal;
			buttonStyle.normal.textColor = Color.white;
			buttonStyle.alignment = TextAnchor.MiddleLeft;
			buttonStyle.padding = new RectOffset(6, 2, 4, 2);

			GUILayout.BeginHorizontal();
			GUILayout.Label ("1. Add fuel lines:");
			if (GUILayout.Button("Asparagus",buttonStyle)) {
				ASPFuelLine.AddFuelLines ();
			}
			if (GUILayout.Button ("Onion", buttonStyle)) {
				ASPFuelLine.AddOnionFuelLines ();
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("2. Connect fuel lines",buttonStyle)) {
				ASPFuelLine.connectFuelLines();
				ReloadShip();
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label ("3. Add some empty stages for staging");
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("4. Stage decouplers and sepratrons",buttonStyle)) {
				ASPStaging.AsaparagusTheShip ();
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label ("5. Save and re-load ship");
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Delete all fuel lines",buttonStyle)) {
				ASPFuelLine.DeleteAllFuelLines ();
				ReloadShip();
			}
			GUILayout.EndHorizontal();


#if KSPdev

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("DEV - Reload ship",buttonStyle)) {
				ReloadShip();
			}
			GUILayout.EndHorizontal();


			GUILayout.BeginHorizontal();
			if (GUILayout.Button("DEV - Dump the ship",buttonStyle)) {
				ASPConsoleStuff.ListTheShip ();
			}
			GUILayout.EndHorizontal();


			GUILayout.BeginHorizontal();
			if (GUILayout.Button("DEV - flush output buffer",buttonStyle)) {
				// flush output buffer
				for (int i = 1; i <= 20; i++) {
					print ("");
				}
			}
			GUILayout.EndHorizontal();
#endif

			GUI.DragWindow();

			if (Event.current.type == EventType.MouseUp) {
				scaling = false;
			}
			else if (Event.current.type == EventType.MouseDown && 
			         GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition)) {
				scaling = true;
			}

			if (scaling) {
				windowRect = new Rect (windowRect.x, windowRect.y, 
				                   windowRect.width + Event.current.delta.x, windowRect.height + Event.current.delta.y);
			}
		}

	}
}


// The block below is only for use during development.  It loads a quicksave game name "dev" and goes right into the editor.
#if KSPdev
[KSPAddon(KSPAddon.Startup.MainMenu, false)]
public class Debug_AutoLoadQuicksaveOnStartup: UnityEngine.MonoBehaviour
{
	public static bool first = true;
	public void Start()
	{
		if (first)
		{
			first = false;
			HighLogic.SaveFolder = "dev";
			var game = GamePersistence.LoadGame("quicksave", HighLogic.SaveFolder, true, false);
			if (game != null && game.flightState != null && game.compatible)
			{
				HighLogic.LoadScene(GameScenes.EDITOR);
			}
		}
	}
}
#endif