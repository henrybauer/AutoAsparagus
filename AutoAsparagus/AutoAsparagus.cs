// this define is only for development, remove it for production.
#define KSPdev

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

		private void printTransform(string header, Transform t){
			print (header + ": pos:" + t.localPosition.ToString () + "/rot:" + t.localRotation+"/dir:");
			print ("    scale:" + t.localScale + "/up:" + t.up.ToString ());
		}
		private void printAttachNode(string header, AttachNode an){
			print ("attachNode: " + an.id.ToString()+"/attached id:"+ an.attachedPartId.ToString() + "/" + an.attachMethod.ToString () + "/" + an.nodeType.ToString ());
			print("      pos:"+an.position.ToString()+" or:"+an.orientation.ToString()+" offset:"+an.offset.ToString()+" size:"+an.size.ToString()+"/radius: "+an.radius.ToString());
			print ("    rq:" + an.requestGate.ToString ());
			if (an.attachedPart == null) {
				print ("    null attachedPart");
			} else {
				printPart ("    attachedPart: ", an.attachedPart);
			}
		}

		private void dumpPart(Part p){
			print ("==== "+p.name.ToString () + ": " + p.uid.ToString () + "/" + p.symmetryMode.ToString ()+"/"+p.children.Count.ToString());
			printTransform ("transform", p.transform);
			print ("  isAttached:" + p.isAttached.ToString () + "/isConnected:" + p.isConnected.ToString ());
			foreach (AttachNode an in p.attachNodes) {
				printAttachNode ("    ", an);
			}
			if (p is FuelLine) {
				FuelLine f = (FuelLine)p;

				print ("FuelLine: fueldir:" + f.flowDirection.ToString () + "/open:" + f.fuelLineOpen.ToString () + "/maxLength:" + f.maxLength.ToString ());
				printPart ("    fuelLookupTarget", f.fuelLookupTarget);
				printPart ("    target", f.target);
				print ("     dir:" + f.direction.ToString () + "/targetpos:" + f.targetPosition.ToString () + "/target");
				printTransform ("startCap", f.startCap);
				printTransform ("endCap", f.endCap);
				printTransform ("line", f.line);
				printTransform ("targetAnchor", f.targetAnchor);
				printAttachNode ("srfAttachNode: ", f.srfAttachNode);
			}
		}


		private void printPart(string header, Part p){
			if (p==null){
				print (header + ": null!");
			} else {
				print (header +": "+p.name.ToString () + ": " + p.uid.ToString () + "/" + p.symmetryMode.ToString ()+"/"+p.children.Count.ToString());
			}
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

		private void ListTheShip(){
			var editor = EditorLogic.fetch;

			// Get all the parts of the ship
			var parts = editor.ship.parts;
			printPartList("All parts of ship", "Part", parts);
			foreach (Part p in parts) {
				dumpPart (p);
			}
			// flush output buffer
			for (int i = 1; i <= 20; i++) {
				print ("");
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

		//private void AttachFuelLine(Part sourceTank, Part destinationTank){
		private void AttachFuelLine(){
			print ("=== AttachFuelLine ===");
			// I have no idea what I'm doing

			// Get all the parts of the ship
			EditorLogic editor = EditorLogic.fetch;
			ShipConstruct ship = editor.ship;
			List<Part> parts = ship.parts;

			Part rootpart = parts [0];

			// Make a new Part object
			AvailablePart ap = PartLoader.getPartInfoByName ("probeStackLarge");
			UnityEngine.Object obj = UnityEngine.Object.Instantiate(ap.partPrefab);
			Part newPart = (Part)obj;

			newPart.gameObject.SetActive(true);
			newPart.gameObject.name = "probeStackLarge";
			newPart.partInfo = ap;
			//newPart.highlightRecurse = true;
			newPart.transform.localScale = rootpart.transform.localScale;
			newPart.transform.parent = rootpart.transform; // must be BEFORE localposition!
			Vector3 v = new Vector3 ();
			v.x = 0.0f;
			v.y = -0.4f;
			v.z = 0.0f;

			Quaternion q = new Quaternion ();
			q.x = 0.0f;
			q.y = 0.0f;
			q.z = 0.0f;
			q.w = 1.0f;
			newPart.transform.localPosition = v;
			newPart.transform.localRotation = q;

			rootpart.addChild (newPart);
			ship.Add (newPart);

			AttachNode newAN = newPart.findAttachNodes ("top") [0];
			newAN.attachedPart = rootpart;
			AttachNode rootAN = rootpart.findAttachNodes ("bottom") [0];
			rootAN.attachedPart = newPart;

			//Staging.SortIcons ();

			return;
			print (".. before SpawnPart");
			//editor.SpawnPart (ap);

			/*
			ConfigNode c = new ConfigNode();
			c.AddNode("foo");
			c.SetValue("foo","bar");
			*/
			ship.Add (newPart);

			newPart = ap.partPrefab;
			ship [0].addChild (newPart);
			//ShipConstruct newShip = new ShipConstruct ("shipname", 0, ship);
			//editor.SpawnPart (newShip);


			return;

			print (".. before ShipConstruct.Add");
			//ship.Add (p);

		
			print (".. before FuelLine");
			FuelLine f = new FuelLine ();


			//print (".. before transform");
			//f.transform.localPosition = v;
			//f.transform.localRotation = q;

			print (".. before PartLoader");
			PartLoader.Instantiate(f,v,q);
			

			print (".. before editorlogic");


			print (".. before availablepart");
			ap = new AvailablePart ("FTX-2 External Fuel Duct");
			print (".. before spawnpart");
			if (ap == null) {
				print (".. ap is null!");
			} else {
				editor.SpawnPart (ap);
			}

			/*Transform t = transform;
			t.localPosition = v;
			t.localRotation = q;

			sourceTank.addChild (f);
			f.target = destinationTank;
			f.targetAnchor = t;
			f.targetPosition = v;
			f.AddAttachNode(c);
			*/
		}

		// called every screen refresh
		private void OnWindow(int windowID){
			var scaling = false;

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("AutoAsparagus!")) {
				AsaparagusTheShip ();
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("add fuel lines")) {
				AttachFuelLine();
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Dump the ship")) {
				ListTheShip ();
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