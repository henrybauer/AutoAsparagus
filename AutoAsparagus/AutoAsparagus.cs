// this define is only for development, remove it for production.
#define KSPdev

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Toolbar;
using KSP.IO;

// FIXME project-wide: change all parent/child checking to check grandparents/grandchildren if parent/child is a fuel tank (or anything except decoupler or fuel line)
// FIXME position fuel lines to avoid obstructions

namespace AutoAsparagus {

	//KSPAddon.Startup.EditorVAB
	//KSPAddon.Startup.EveryScene
	[KSPAddon(KSPAddon.Startup.EditorVAB, false)]
	public class AutoAsparagus: MonoBehaviour
	{
		private IButton aspButton;
		//private IButton onionButton;
		private bool visible = false;

		private Rect windowRect = new Rect(Screen.width/2,Screen.height * 0.1f,1,1);
		private float minwidth = 1;
		private float minheight = 1;

		public enum ASPState { IDLE, ADDASP, ADDONION, CONNECT, AFTERCONNECT, ADDSTAGES, STAGE, RELOAD, DELETEFUEL, AFTERSTAGE, CLAMPS };
		public static ASPState mystate = ASPState.IDLE;
		private int refreshwait = 0;

		private static Texture2D aspTexture = null;
		private static Texture2D onionTexture = null;
		private static Texture2D nofuelTexture = null;
		//private static Texture2D strutTexture = null;
		private static Texture2D parachuteTexture = null;
		private static Texture2D launchclampTexture = null;
		private static Texture2D sepratronTexture = null;

		public static bool stageParachutes = false;
		public static bool stageLaunchClamps = false;
		private static string[] launchClampsText = new string[] {"... in bottom stage", "... in next-to-bottom stage"};
		public static int launchClampsStage = 0;
		public static bool addStruts = false;
		public static bool stagesepratrons = true;

		private GUIStyle tooltipstyle = null;
		private GUIStyle buttonStyle = null;
		private GUIStyle picbutton = null;
		private GUIStyle togglestyle = null;
		private GUIStyle osdstyle = null;

		private string tooltip = "";
		private static string osdmessage = null;
		private static float osdtime = Time.time;

		private static Texture2D loadTexture(string path) {
			print ("=== AutoAsparagus loading texture: " + path);
			return GameDatabase.Instance.GetTexture(path, false);
		}

		private static bool researchedFuelLines(){
			if (ResearchAndDevelopment.Instance == null) {
				print ("no ResearchAndDevelopment.Instance, must be sandbox mode");
				return true;
			}
			AvailablePart ap = PartLoader.getPartInfoByName ("fuelLine");
			if (ap == null) {
				print ("no fuelLine AvailablePart()");
				return false;
			}

			print ("checking PartTechAvailable");
			return (ResearchAndDevelopment.PartTechAvailable(ap));

		}

		internal AutoAsparagus() {
			aspButton = ToolbarManager.Instance.add ("AutoAsparagus", "aspButton");
			aspButton.TexturePath = "AutoAsparagus/asparagus";
			aspButton.ToolTip = "AutoAsparagus";
			aspButton.Visibility = new GameScenesVisibility (GameScenes.EDITOR);
			aspButton.OnClick += (e) => {
				if (researchedFuelLines()) {
					visible = !visible;
				} else {
					osd("Fuel lines have not been researched yet!");
					visible = false;
				}
			};

			//ASPUpdateCheck u = new ASPUpdateCheck (); // doesn't work?
		}

		internal void setStyles() {
			tooltipstyle = new GUIStyle(GUI.skin.box);
			tooltipstyle.wordWrap = false;
			Texture2D blackTexture = new Texture2D (1, 1);
			blackTexture.SetPixel(0,0,Color.black);
			blackTexture.Apply();
			tooltipstyle.normal.background = blackTexture;
				
			buttonStyle = new GUIStyle(GUI.skin.button);
			buttonStyle.wordWrap = false;
			buttonStyle.fontStyle = FontStyle.Normal;
			buttonStyle.normal.textColor = Color.white;
			buttonStyle.alignment = TextAnchor.MiddleLeft;
			buttonStyle.padding = new RectOffset(6, 2, 4, 2);
			buttonStyle.stretchWidth = false;
			buttonStyle.stretchHeight = false;

			picbutton = new GUIStyle(GUI.skin.button);
			picbutton.wordWrap = false;
			picbutton.fontStyle = FontStyle.Normal;
			picbutton.normal.textColor = Color.white;
			picbutton.alignment = TextAnchor.MiddleCenter;
			picbutton.stretchHeight = false;
			picbutton.stretchWidth = false;
			//picbutton.fixedHeight = 42; // don't forget to bring a towel
			//picbutton.fixedWidth = 42;

			togglestyle = new GUIStyle (GUI.skin.toggle);
			togglestyle.wordWrap = false;
			togglestyle.fontStyle = FontStyle.Normal;
			togglestyle.normal.textColor = Color.white;
			togglestyle.alignment = TextAnchor.MiddleLeft;
			togglestyle.stretchHeight = false;
			togglestyle.stretchWidth = false;

			osdstyle = new GUIStyle ();
			osdstyle.stretchWidth = true;
			osdstyle.alignment = TextAnchor.MiddleCenter;
			osdstyle.fontSize = 32;
			osdstyle.fontStyle = FontStyle.Bold;
			osdstyle.normal.textColor = Color.white;
			osdstyle.normal.background = blackTexture;

		}

		internal void OnDestroy() {
			aspButton.Destroy ();
		}

		// Called after the scene is loaded.
		public void Awake() {
			//RenderingManager.AddToPostDrawQueue(0, OnDraw);
		}

		// Called after Awake()
		public void Start() {
			aspTexture = loadTexture ("AutoAsparagus/asparagus");
			onionTexture = loadTexture ("AutoAsparagus/onion");
			nofuelTexture = loadTexture ("AutoAsparagus/nofuel");
			launchclampTexture = loadTexture ("AutoAsparagus/launchclamp");
			parachuteTexture = loadTexture ("AutoAsparagus/parachute");
			//strutTexture = loadTexture ("AutoAsparagus/strut");
			sepratronTexture = loadTexture ("AutoAsparagus/sepratron");
		}

		internal static Rect clampToScreen(Rect rect) {
			rect.width = Mathf.Clamp(rect.width, 0, Screen.width);
			rect.height = Mathf.Clamp(rect.height, 0, Screen.height);
			rect.x = Mathf.Clamp(rect.x, 0, Screen.width - rect.width);
			rect.y = Mathf.Clamp(rect.y, 0, Screen.height - rect.height);
			return rect;
		}

		public static void osd(string message){
			osdmessage = message;
			osdtime = Time.time + 3.0f;
		}

		private void OnGUI(){
			if (tooltipstyle == null) {
				setStyles ();
			}

			if (osdtime > Time.time) {
				float osdheight = osdstyle.CalcSize (new GUIContent(osdmessage)).y;
				GUILayout.BeginArea(new Rect(0, Screen.height * 0.1f, Screen.width, osdheight), osdstyle);
				GUILayout.Label(osdmessage, osdstyle);
				GUILayout.EndArea();
			}

			if (visible) {
				if (refreshwait > 0) {
					refreshwait = refreshwait - 1;
				} else {
					switch (mystate) {
					case ASPState.IDLE:

						windowRect = clampToScreen(GUILayout.Window (9035768, windowRect, OnWindow, "AutoAsparagus"));

						if (tooltip != "") {
							GUI.depth = 0;
							Vector2 size = tooltipstyle.CalcSize (new GUIContent (tooltip));
							Rect rect = new Rect (Input.mousePosition.x + 20, (Screen.height - Input.mousePosition.y) + 20, size.x, size.y);
							rect = clampToScreen (rect);
							GUILayout.BeginArea (rect);
							GUILayout.Label (tooltip, tooltipstyle);
							GUILayout.EndArea ();
						}

						break;
					case ASPState.ADDASP:
						ASPFuelLine.AddFuelLines ();
						mystate = ASPState.CONNECT;
						osd("Connecting fuel lines...");
						refreshwait = 100;
						break;
					case ASPState.ADDONION:
						ASPFuelLine.AddOnionFuelLines ();
						mystate = ASPState.CONNECT;
						osd("Connecting fuel lines...");
						refreshwait = 100;
						break;
					case ASPState.CONNECT:
						ASPFuelLine.connectFuelLines ();
						refreshwait = 100;
						osd ("Refreshing ship...");
						mystate = ASPState.AFTERCONNECT;
						break;
					case ASPState.AFTERCONNECT:
						ReloadShip ();
						mystate = ASPState.ADDSTAGES;
						osd ("Adding empty stages...");
						refreshwait = 100;
						break;
					case ASPState.ADDSTAGES:
						ASPStaging.AddEmptyStages();
						mystate = ASPState.STAGE;
						osd("Staging decouplers...");
						refreshwait = 10;
						break;
					case ASPState.STAGE:
						ASPStaging.AsaparagusTheShip ();
						mystate = ASPState.AFTERSTAGE;
						osd ("Decoupler staging done, refreshing...");
						refreshwait = 10;
						break;
					case ASPState.CLAMPS:
						if (stageLaunchClamps) {
							ASPStaging.StageLaunchClamps ();
						}
						mystate = ASPState.IDLE;
						osd ("Done!");
						refreshwait = 10;
						break;
					case ASPState.DELETEFUEL:
						ASPFuelLine.DeleteAllFuelLines ();
						ReloadShip ();
						osd ("Fuel lines deleted.");
						mystate = ASPState.IDLE;
						break;
					case ASPState.AFTERSTAGE:
						ReloadShip ();
						if (stageLaunchClamps) {
							osd ("Staging launch clamps...");
							mystate = ASPState.CLAMPS;
							refreshwait = 10;
						} else {
							osd ("Done!");
							mystate = ASPState.IDLE;
						}
						break;
					}
				}
			}
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

			GUILayout.BeginHorizontal();
			if (GUILayout.Button(new GUIContent ("Asparagus",aspTexture, "Create fuel lines and stage the ship, asparagus-style"),picbutton)) {
				mystate = ASPState.ADDASP;
				osd("Adding fuel lines in asparagus style...");
			}
			if (GUILayout.Button (new GUIContent("Onion",onionTexture, "Create fuel lines and stage the ship, onion-style"), picbutton)) {
				mystate = ASPState.ADDONION;
				osd("Adding fuel lines in onion style...");
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			if (GUILayout.Button (new GUIContent("Delete fuel lines",nofuelTexture, "Delete fuel lines"), picbutton)) {
				osd("Deleting fuel lines...");
				mystate = ASPState.DELETEFUEL;
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label ("Options:");
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			stageParachutes = GUILayout.Toggle (stageParachutes, new GUIContent(" Stage parachutes", parachuteTexture, "Stage parachutes to fire with decouplers"), togglestyle);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			stagesepratrons = GUILayout.Toggle (stagesepratrons, new GUIContent(" Stage sepratrons", sepratronTexture, "Stage sepratrons to fire with decouplers"), togglestyle);
			GUILayout.EndHorizontal();

			/* not yet, but soon!
			GUILayout.BeginHorizontal();
			addStruts = GUILayout.Toggle (addStruts, new GUIContent(" Add struts", strutTexture, "Add struts along with fuel lines"), togglestyle);
			GUILayout.EndHorizontal();
			*/

			if (!stageLaunchClamps) {
				minheight = windowRect.height;
				minwidth = windowRect.width;
			}

			bool oldclamps = stageLaunchClamps;
			GUILayout.BeginHorizontal();
			stageLaunchClamps= GUILayout.Toggle (stageLaunchClamps, new GUIContent(" Stage launch clamps", launchclampTexture, "Move launch clamps to the bottom or next-to-bottom stage"), togglestyle);
			GUILayout.EndHorizontal();
			if (stageLaunchClamps != oldclamps) {
				if (!stageLaunchClamps) {
					// shrink window to old dimesions
					windowRect.height = minheight;
					windowRect.width = minwidth;
				}
			}

			if (stageLaunchClamps) {
				GUILayout.BeginHorizontal();
				//GUILayout.Label ("Launch clamps:");
				launchClampsStage=GUILayout.SelectionGrid(launchClampsStage,launchClampsText,1, togglestyle);
				GUILayout.EndHorizontal();
			}



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

			if (Event.current.type == EventType.Repaint) { // why, Unity, why?
				tooltip = GUI.tooltip;
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