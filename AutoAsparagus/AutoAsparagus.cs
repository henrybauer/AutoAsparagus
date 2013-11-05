// this define is only for development
//#define KSPdev

using System;
using KSP.IO;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AutoAsparagus {

[KSPAddon(KSPAddon.Startup.EditorAny, false)]
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

		private void printPart(string header, Part p){
			print (header +": "+p.name.ToString () + ": " + p.uid.ToString () + "/" + p.symmetryMode.ToString ());
		}

		private void printPartList(string title, string header, List<Part> parts){
			print ("=== "+title+" ===");
			foreach (Part p in parts) {
				printPart(header,p);
			}
		}

		private List<Part> getFuelLineTargets(Part p){
			printPart ("...searching fuel lines of part", p);
			List<Part> targets = new List<Part> ();
			foreach (Part child in p.children) {
				if (child is FuelLine) {
					// FuelLine class has a "target" attribute; Part class doesn't, so we have to re-class to access ".target"
					Part fuelLineTarget = ((FuelLine)child).target;
					if (fuelLineTarget != null) {
						printPart ("...found fuel line target", fuelLineTarget);
						targets.Add (fuelLineTarget);
					}
				}
			}
			return targets;
		}

		private void stageChain(List<Part> chain){
			printPartList ("== Staging chain", "chain part", chain);

			int lowestStage = Staging.StageCount-1;
			foreach (Part p in chain) {
				if (p.parent.inverseStage < lowestStage) {
					lowestStage = p.parent.inverseStage;
				}
			}

			print ("..lowest stage is "+lowestStage.ToString()+"/"+Staging.StageCount.ToString());
			print ("..adding " + (chain.Count - 1).ToString() + " stages");

			// this screws things up
			/*int x = chain.Count-1;
			while (x > 0) {
				Staging.AddStageAt (lowestStage+1);
				x = x - 1;
			}*/

			int stage = lowestStage + chain.Count - 1;
			int partNumber = 0;
			while (partNumber<(chain.Count-1)) {
				printPart ("..setting part "+partNumber.ToString()+" to stage " + stage.ToString (), chain [partNumber]);

				// Parent should be a decoupler
				Part parent = chain [partNumber].parent;
				printPart ("..parent is ", parent);
				if (parent.name.ToLower().Contains ("decoupler")) {
					chain [partNumber].parent.inverseStage = stage;
				} else {
					print ("..parent is not a decoupler, ignoring!");
				}
				
				// Sepratrons should be children
				foreach (Part child in chain[partNumber].children){
					if (child.name.ToLower().Contains("sepmotor")) {
						printPart("..setting child Sepratron to stage "+stage.ToString(),child);
						child.inverseStage = stage;
					}
				}

				stage = stage - 1;
				partNumber = partNumber + 1;
			}
		}

		private void AsaparagusTheShip(){
			var editor = EditorLogic.fetch;

			// Get all the parts of the ship
			var parts = editor.ship.parts;
			printPartList("All parts of ship", "Part", parts);

			// Find the symmetrical fuel tanks
			List<Part> tanks = new List<Part>();
			foreach (Part p in parts) {
				// deliberately leave off first letter, so we don't have to worry about case
				if ((p.name.ToLower().Contains("fueltank") || p.name.ToLower().Contains("fuselage")) && p.symmetryMode>0) {
					tanks.Add (p);
				}
			}

			print("=== Tanks ===");

			// print out a list of tanks, partners, and children
			foreach (Part p in tanks) {
				printPart ("Tank", p);
				foreach (Part partner in p.symmetryCounterparts) {
					printPart ("partner", partner);
				}
				foreach (Part child in p.children) {
					printPart ("child", child);
				}

			}

			// Make chains by following fuel lines
			List<Part> tanksToStage = tanks;
			while (tanksToStage.Count > 0) {
				// start a new chain with the first tank
				Part p = tanksToStage [0];
				List<Part> chain = new List<Part> ();
				printPart ("*** Starting new chain with", p);

				// First, follow the fuel lines
				while (p != null) {
					printPart ("Adding to chain at position " + chain.Count, p);
					chain.Add (p);

					// don't try to put that tank in another chain
					tanksToStage.Remove (p);

					printPart ("Following fuel line from", p);
					Part r = p;
					p = null;
					foreach (Part target in getFuelLineTargets(r)) {
						if (tanks.Contains(target)) {  // we're only following fuel lines in the asparagus
							printPart ("..followed fuel line to", target);
							p = target;
						} 
					}
				}

				// Next, look for fuel lines that lead to the start of our chain

				int x = tanksToStage.Count;
				while (x > 0) {
					p = tanksToStage [x-1]; // get last tank
					printPart ("Checking tank "+x.ToString()+" of "+tanksToStage.Count+" to insert in chain", p);
					x = x - 1;

					foreach (Part target in getFuelLineTargets(p)) {
						if (chain[0]==target) {
							printPart ("..prepending to chain", target);
							chain.Insert (0, p);
							tanksToStage.Remove(p);
							x = tanksToStage.Count; // reset the countdown
						} 
					}

				}
				printPartList ("*** Completed chain", "chain part", chain);

				stageChain (chain);
			}

#if KSPdev
			// flush output buffer
			for (int i = 1; i <= 20; i++) {
				print ("");
			}
#endif

			// Update staging display
			Staging.SortIcons ();
		}

		// called every screen refresh
		private void OnWindow(int windowID){
			var scaling = false;

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("AutoAsparagus!")) {
				AsaparagusTheShip ();
			}
			GUILayout.EndHorizontal();

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
				//FlightDriver.StartAndFocusVessel(game, game.flightState.activeVesselIdx);
				HighLogic.LoadScene(GameScenes.EDITOR);
			
			}
			//CheatOptions.InfiniteFuel = true;
		}
	}
}
#endif