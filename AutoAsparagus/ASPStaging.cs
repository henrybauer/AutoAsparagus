using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AutoAsparagus
{
	public class ASPStaging: MonoBehaviour
	{
		static public void stageChain(List<Part> chain){
			ConsoleStuff.printPartList ("== Staging chain", "chain part", chain);

			int lowestStage = Staging.StageCount-1;
			foreach (Part p in chain) {
				if (p.parent.inverseStage < lowestStage) {
					lowestStage = p.parent.inverseStage;
				}
			}

			print ("..lowest stage is "+lowestStage.ToString()+"/"+Staging.StageCount.ToString());
			print ("..adding " + (chain.Count - 1).ToString() + " stages");

			// this screws things up, don't do it
			/*			int x = chain.Count-1;
			while (x > 0) {
				Staging.AddStageAt (lowestStage+1);
				x = x - 1;
			}*/

			int stage = lowestStage + chain.Count - 1;
			int partNumber = 0;
			while (partNumber<(chain.Count-1)) {
				ConsoleStuff.printPart ("..setting part "+partNumber.ToString()+" to stage " + stage.ToString (), chain [partNumber]);

				// Parent should be a decoupler
				Part parent = chain [partNumber].parent;
				ConsoleStuff.printPart ("..parent is ", parent);
				if (parent.name.ToLower().Contains ("decoupler")) {
					chain [partNumber].parent.inverseStage = stage;
				} else {
					print ("..parent is not a decoupler, ignoring!");
				}

				// Sepratrons should be children
				foreach (Part child in chain[partNumber].children){
					if (child.name.ToLower().Contains("sepmotor")) {
						ConsoleStuff.printPart("..setting child Sepratron to stage "+stage.ToString(),child);
						child.inverseStage = stage;
					}
				}

				stage = stage - 1;
				partNumber = partNumber + 1;
			}
		}

		static public void AsaparagusTheShip(){
			var editor = EditorLogic.fetch;

			// Get all the parts of the ship
			var parts = editor.ship.parts;
			ConsoleStuff.printPartList("All parts of ship", "Part", parts);

			// Find the symmetrical fuel tanks
			List<Part> tanks = new List<Part>();
			print ("=== Looking for symmetrical fuel tanks");
			foreach (Part p in parts) {
				/*				 Sample names:
				 * fuelTank3
				 * cl_radial_cylTankFuel
				 * cl_radial_cylTankOxy
                 * KW2Sidetank
                 * KW1mtankL1
                 * KW1mtankPancake
                 * RCSTank1
                 */

				if ((p.name.ToLower().Contains("tank") || p.name.ToLower().Contains("fuselage")) && p.symmetryMode>0) {
					// Check if this actually has any resources besides MonoPropellant
					// Mod packs have LiquidFuel-only and Oxydizer-only tanks, and Kethane tanks have Kethane,
					//   so rather than check for the existence of a predefined list of fuels, just check
					//   that this is not a Mono-Propellant-only tank.
					PartResourceList rl = p.Resources;
					if (rl == null) {
						continue;
					}
					if (rl.Count == 0) {
						continue;
					}
					bool onlyHasMono = true;
					foreach (PartResource pr in rl.list) {
						print ("resource name: " + pr.resourceName);
						if (pr.resourceName.ToLower () != "monopropellant") {
							onlyHasMono = false;
						}
					}
					if (!onlyHasMono) {
						ConsoleStuff.printPart ("Adding fuel tank", p);
						tanks.Add (p);
					} else {
						// monopropellant doesn't use fuel lines!
						ConsoleStuff.printPart("Not adding monopropellant-only tank",p);
					}
				}
			}

			print("=== Tanks ===");

			// print out a list of tanks, partners, and children
			foreach (Part p in tanks) {
				ConsoleStuff.printPart ("Tank", p);
				foreach (Part partner in p.symmetryCounterparts) {
					ConsoleStuff.printPart ("partner", partner);
				}
				foreach (Part child in p.children) {
					ConsoleStuff.printPart ("child", child);
				}

			}

			// Make chains by following fuel lines
			List<Part> tanksToStage = tanks;
			while (tanksToStage.Count > 0) {
				// start a new chain with the first tank
				Part p = tanksToStage [0];
				List<Part> chain = new List<Part> ();
				ConsoleStuff.printPart ("*** Starting new chain with", p);

				// First, follow the fuel lines
				while (p != null) {
					ConsoleStuff.printPart ("Adding to chain at position " + chain.Count, p);
					chain.Add (p);

					// don't try to put that tank in another chain
					tanksToStage.Remove (p);

					ConsoleStuff.printPart ("Following fuel line from", p);
					Part r = p;
					p = null;
					foreach (Part target in ASPFuelLine.getFuelLineTargets(r)) {
						if (tanks.Contains(target)) {  // we're only following fuel lines in the asparagus
							ConsoleStuff.printPart ("..followed fuel line to", target);
							p = target;
						} 
					}
				}

				// Next, look for fuel lines that lead to the start of our chain

				int x = tanksToStage.Count;
				while (x > 0) {
					p = tanksToStage [x-1]; // get last tank
					ConsoleStuff.printPart ("Checking tank "+x.ToString()+" of "+tanksToStage.Count+" to insert in chain", p);
					x = x - 1;

					foreach (Part target in ASPFuelLine.getFuelLineTargets(p)) {
						if (chain[0]==target) {
							ConsoleStuff.printPart ("..prepending to chain", target);
							chain.Insert (0, p);
							tanksToStage.Remove(p);
							x = tanksToStage.Count; // reset the countdown
						} 
					}

				}
				ConsoleStuff.printPartList ("*** Completed chain", "chain part", chain);
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
	}
}

