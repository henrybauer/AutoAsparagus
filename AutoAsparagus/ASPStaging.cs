using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AutoAsparagus
{
	public class ASPStaging: MonoBehaviour
	{

		static private void stageChildren(Part p, int stage){
			// Sepratrons should be children
			foreach (Part child in p.children){
				if ((AutoAsparagus.stagesepratrons) && (child.name.ToLower().Contains("sepmotor"))) {
					ASPConsoleStuff.printPart("..setting child Sepratron to stage "+stage.ToString(),child);
					child.inverseStage = stage;
				}
				if ((AutoAsparagus.stageParachutes) && (child.name.ToLower().Contains("parachute"))) {
					ASPConsoleStuff.printPart("..setting child Parachute to stage "+stage.ToString(),child);
					child.inverseStage = stage;
				}
			}

		}
		static public void stageChain(List<Part> chain){
			ASPConsoleStuff.printPartList ("== Staging chain", "chain part", chain);

			int lowestStage = Staging.StageCount-1;
			foreach (Part p in chain) {
				if (p.parent.inverseStage < lowestStage) {
					lowestStage = p.parent.inverseStage;
				}
			}

			print ("..lowest stage is "+lowestStage.ToString()+"/"+Staging.StageCount.ToString());
			print ("..adding " + (chain.Count - 1).ToString() + " stages");

			int stage = lowestStage + chain.Count - 1;
			int partNumber = 0;
			int safetyfactor 10000;
			while (partNumber<(chain.Count)) {
				safetyfactor = safetyfactor - 1;
				if (safetyfactor == 0) {
					AutoAsparagus.osd ("Infinite loop in stageChain, aborting :(");
					AutoAsparagus.mystate = AutoAsparagus.ASPState.IDLE;
					return;
				}
				// Parent should be a decoupler
				Part parent = chain [partNumber].parent;
				ASPConsoleStuff.printPart ("..parent is ", parent);
				if (parent.name.ToLower().Contains ("decoupler")) {
					ASPConsoleStuff.printPart ("..setting part " + partNumber.ToString () + " to stage " + stage.ToString (), chain [partNumber]);
					chain [partNumber].parent.inverseStage = stage;
					chain [partNumber].inverseStage = stage;
					stageChildren(chain[partNumber],stage);
				} else {
					print ("..parent is not a decoupler, ignoring!");
				}

				stage = stage - 1;
				partNumber = partNumber + 1;
			}
		}

		static public void StageLaunchClamps() {
			var editor = EditorLogic.fetch;

			// Get all the parts of the ship
			var parts = editor.ship.parts;

			int stage = Staging.StageCount - 1;
			if (AutoAsparagus.launchClampsStage == 1) {
				stage = stage - 1;
			}

			foreach (Part p in parts) {
				if (p.name.ToLower ().Contains ("launchclamp")) {
					ASPConsoleStuff.printPart ("Putting launch clamp into stage " + stage.ToString (), p);
					p.inverseStage = stage;
				}
			}

		}

		static public bool isFuelTank(Part p){
			    /* Sample names:
				 * fuelTank3
				 * cl_radial_cylTankFuel
				 * cl_radial_cylTankOxy
                 * KW2Sidetank
                 * KW1mtankL1
                 * KW1mtankPancake
                 * RCSTank1
                 */
			if ((p!=null) && (p.name != null)) {
				if ((p.name.ToLower ().Contains ("tank")) || (p.name.ToLower ().Contains ("fuselage"))) {
					// Check if this actually has any resources besides MonoPropellant
					// Mod packs have LiquidFuel-only and Oxydizer-only tanks, and Kethane tanks have Kethane,
					//   so rather than check for the existence of a predefined list of fuels, just check
					//   that this is not a MonoPropellant-only tank.
					PartResourceList rl = p.Resources;
					if (rl == null) {
						ASPConsoleStuff.printPart ("isFuelTank: Part is NOT a fuel tank, no resources", p);
						return false;
					}
					if (rl.Count == 0) {
						ASPConsoleStuff.printPart ("isFuelTank: Part is NOT a fuel tank, no resources", p);
						return false;
					}
					foreach (PartResource pr in rl.list) {
						print ("isFuelTank: resource name: " + pr.resourceName);
						if (pr.resourceName.ToLower () != "monopropellant") {
							ASPConsoleStuff.printPart ("isFuelTank: Part IS a fuel tank", p);
							return true;
						}
					}
					ASPConsoleStuff.printPart ("isFuelTank: Part is NOT a fuel tank, because it's a monopropellant tank", p);
					return false;
				} else {
					ASPConsoleStuff.printPart ("isFuelTank: Part is NOT a fuel tank, based on name", p);
					return false;
				}
			}
			return false;
		}

		static public List<Part> findSymettricalFuelTanks(List<Part> parts) {
			List<Part> tanks = new List<Part>();
			print ("=== Looking for symmetrical fuel tanks");
			foreach (Part p in parts) {
				if ((isFuelTank(p)) && (p.symmetryMode>0) && (!isFuelTank(p.parent))){
					ASPConsoleStuff.printPart ("Adding fuel tank", p);
					tanks.Add (p);
				}
			}
			return tanks;
		}

		/*
		static public void DeleteEmptyStages() {
			var editor = EditorLogic.fetch;

			// Get all the parts of the ship
			var parts = editor.ship.parts;

			List<int> usedstages = new List<int> ();

			while (usedstages.Count != Staging.StageCount) {
				usedstages = new List<int> ();
				foreach (Part p in parts) {
					if (!usedstages.Contains (p.inverseStage)) {
						usedstages.Add (p.inverseStage);
					}
				}
				for (int x = Staging.StageCount; x > 0; x = x - 1) {
					if (!usedstages.Contains (x)) {
						//StageGroup sg = new StageGroup();
						// how in the world do I get a StageGroup object for stage x??
						//Staging.DeleteStage (sg);

					}
				}

			}

		}*/

		static public void AddEmptyStages() {
			var editor = EditorLogic.fetch;

			// Get all the parts of the ship
			var parts = editor.ship.parts;

			List<Part> decouplers = new List<Part>();
			foreach (Part p in parts) {
				if (p.name.ToLower ().Contains ("decoupler")) {
					if (p.symmetryMode > 0) {
						decouplers.Add (p);
					}
				}
			}
			List<Part> decouplers2 = decouplers;
			List<int> usedstages = new List<int> ();

			int safetyfactor 10000;
			while (decouplers.Count>0){
				safetyfactor = safetyfactor - 1;
				if (safetyfactor == 0) {
					AutoAsparagus.osd ("Infinite loop in AddEmptyStages, aborting :(");
					AutoAsparagus.mystate = AutoAsparagus.ASPState.IDLE;
					return;
				}
				Part p = decouplers [0];

				if (usedstages.Contains (p.inverseStage)) {
					Staging.AddStageAt (p.inverseStage + 1);
					p.inverseStage = p.inverseStage + 1;
				}
				int x = p.symmetryMode / 2;
				while (x > 0) {
					usedstages.Add (p.inverseStage);
					x = x - 1;
				}
				decouplers.Remove (p);
				foreach (Part brother in p.symmetryCounterparts) {
					brother.inverseStage = p.inverseStage;
					decouplers.Remove (brother);
				}
			}

			foreach (Part p in decouplers2) {
				Staging.AddStageAt (p.inverseStage + 1);
			}
		}

		static public void AsaparagusTheShip(){
			var editor = EditorLogic.fetch;

			// Get all the parts of the ship
			var parts = editor.ship.parts;
			ASPConsoleStuff.printPartList("All parts of ship", "Part", parts);

			// Find the symmetrical fuel tanks
			List<Part> tanks = findSymettricalFuelTanks (parts);
		
			print("=== Tanks ===");

			// print out a list of tanks, partners, and children
			foreach (Part p in tanks) {
				ASPConsoleStuff.printPart ("Tank", p);
				foreach (Part partner in p.symmetryCounterparts) {
					ASPConsoleStuff.printPart ("partner", partner);
				}
				foreach (Part child in p.children) {
					ASPConsoleStuff.printPart ("child", child);
				}

			}

			// Make chains by following fuel lines
			List<Part> tanksToStage = tanks;
			int safetyfactor 10000;
			while (tanksToStage.Count > 0) {
				safetyfactor = safetyfactor - 1;
				if (safetyfactor == 0) {
					AutoAsparagus.osd ("Infinite loop in AsaparagusTheShip:tanksToStage.Count, aborting :(");
					AutoAsparagus.mystate = AutoAsparagus.ASPState.IDLE;
					return;
				}
				// start a new chain with the first tank
				Part p = tanksToStage [0];
				List<Part> chain = new List<Part> ();
				ASPConsoleStuff.printPart ("*** Starting new chain with", p);

				// First, follow the fuel lines
				while (p != null) {
					ASPConsoleStuff.printPart ("Adding to chain at position " + chain.Count, p);
					chain.Add (p);

					// don't try to put that tank in another chain
					tanksToStage.Remove (p);

					ASPConsoleStuff.printPart ("Following fuel line from", p);
					Part r = p;
					p = null;
					foreach (Part target in ASPFuelLine.getFuelLineTargets(r)) {
						if (tanks.Contains(target)) {  // we're only following fuel lines in the asparagus
							ASPConsoleStuff.printPart ("..followed fuel line to", target);
							p = target;
						} 
					}
				}

				// Next, look for fuel lines that lead to the start of our chain

				int x = tanksToStage.Count;
				while (x > 0) {
					p = tanksToStage [x-1]; // get last tank
					ASPConsoleStuff.printPart ("Checking tank "+x.ToString()+" of "+tanksToStage.Count+" to insert in chain", p);
					x = x - 1;

					foreach (Part target in ASPFuelLine.getFuelLineTargets(p)) {
						if (chain[0]==target) {
							ASPConsoleStuff.printPart ("..prepending to chain", target);
							chain.Insert (0, p);
							tanksToStage.Remove(p);
							x = tanksToStage.Count; // reset the countdown
						} 
					}

				}
				ASPConsoleStuff.printPartList ("*** Completed chain", "chain part", chain);
				stageChain (chain);
			}

			// Update staging display
			Staging.SortIcons ();
		}
	}
}

