using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using KSP.UI.Screens;

//#if DEBUG
//using KramaxReloadExtensions;
//#endif

namespace AutoAsparagus
{
//#if DEBUG
//	public class ASPStaging: ReloadableMonoBehaviour
//#else
	public class ASPStaging: MonoBehaviour
//#endif
	{

		static private void setPartStage (Part p, int stage)
		{
			ASPConsoleStuff.printPart ("Setting to stage " + stage.ToString (), p);
			p.inverseStage = stage;
			//p.UpdateStageability (false, true);
			return;
			ASPConsoleStuff.printPart ("staging icon: " + p.stagingIcon, p);
			StageIcon myIcon = null;
			foreach (KSP.UI.Screens.StageGroup sg in StageManager.Instance.Stages) {
				foreach (StageIcon i in sg.Icons) {
					ASPConsoleStuff.print ("Staging icon for stage " + sg.inverseStageIndex.ToString () + ": " + i.name + "/" + i.Part.name);
					if (i.Part == p) {
						ASPConsoleStuff.print ("found icon!");
						myIcon = i;
					}
				}
			}
			if (myIcon == null) {
				return;
			}
			myIcon.Stage.RemoveIcon (myIcon);
			//StageManager.Instance.Stages [p.inverseStage].RemoveIcon (myIcon);
			StageManager.Instance.Stages [stage].AddIcon (myIcon);
		}

		static private void stageChildren (Part p, int stage)
		{
			// Sepratrons should be children
			foreach (Part child in p.children) {
				if ((AutoAsparagus.stagesepratrons) && (child.name.ToLower ().Contains ("sepmotor"))) {
					ASPConsoleStuff.printPart ("..setting child Sepratron to stage " + stage.ToString (), child);
					setPartStage (child, stage);
				}
				if ((AutoAsparagus.stageParachutes) && (child.name.ToLower ().Contains ("parachute"))) {
					ASPConsoleStuff.printPart ("..setting child Parachute to stage " + stage.ToString (), child);
					setPartStage (child, stage);
				}
			}
		}

		static public bool isDecoupler (Part p)
		{
			if (p == null) {
				ASPConsoleStuff.AAprint ("isDecoupler passed a null!");
				return false;
			}
			if (p.name.ToLower ().Contains ("decoupler")) {
				return true;
			}
			foreach (PartModule pm in p.Modules) {
				if (pm.moduleName == "ModuleDecouple") {
					return true;
				}
				if (pm.moduleName == "ModuleAnchoredDecoupler") {
					return true;
				}
				if (pm.moduleName == "SSTUCustomRadialDecoupler") {
					return true;
				}
			}
			return false;
		}

		static public void stageChain (List<Part> chain)
		{
			ASPConsoleStuff.printPartList ("== Staging chain", "chain part", chain);

			int lowestStage = StageManager.StageCount - 1;
			foreach (Part p in chain) {
				if (p.parent.inverseStage < lowestStage) {
					lowestStage = p.parent.inverseStage;
				}
			}

			ASPConsoleStuff.AAprint ("..lowest stage is " + lowestStage.ToString () + " out of " + StageManager.StageCount.ToString () + " total stages");
			ASPConsoleStuff.AAprint ("..adding " + (chain.Count - 1).ToString () + " stages");

			int stage = lowestStage + chain.Count - 1;
			int partNumber = 0;
			int safetyfactor = 10000;
			while (partNumber < (chain.Count)) {
				safetyfactor = safetyfactor - 1;
				if (safetyfactor == 0) {
					AutoAsparagus.osd ("Infinite loop in stageChain, aborting :(");
					AutoAsparagus.mystate = AutoAsparagus.ASPState.IDLE;
					return;
				}
				// Parent should be a decoupler
				Part parent = chain [partNumber].parent;
				ASPConsoleStuff.printPart ("..parent is ", parent);
				if (isDecoupler (parent)) {
					ASPConsoleStuff.printPart ("..setting part " + partNumber.ToString () + " to stage " + stage.ToString (), chain [partNumber]);
					setPartStage (chain [partNumber].parent, stage);
					setPartStage (chain [partNumber], stage);
					stageChildren (chain [partNumber], stage);
				} else {
					ASPConsoleStuff.AAprint ("..parent is not a decoupler, ignoring!");
				}

				stage = stage - 1;
				partNumber = partNumber + 1;
			}
		}

		static public void StageLaunchClamps (int launchClampsStage)
		{
			var editor = EditorLogic.fetch;

			// Get all the parts of the ship
			var parts = editor.ship.parts;

			int stage = StageManager.StageCount - 1;
			if (launchClampsStage == 1) {
				stage = stage - 1;
			}

			foreach (Part p in parts) {
				if (p.name.ToLower ().Contains ("launchclamp")) {
					ASPConsoleStuff.printPart ("Putting launch clamp into stage " + stage.ToString () + ", launchClampsStage =" + launchClampsStage.ToString (), p);
					setPartStage (p, stage);
				}
			}

		}

		static public bool isFuelTank (Part p)
		{
			if (p == null) {
				ASPConsoleStuff.AAprint ("isFuelTank.p is null!");
				return false;
			}

			PartResourceList rl = p.Resources;
			if (rl == null) {
				ASPConsoleStuff.printPart ("isFuelTank: Part is NOT a fuel tank, no resources", p);
				return false;
			}

			if (rl.Count == 0) {
				ASPConsoleStuff.printPart ("isFuelTank: Part is NOT a fuel tank, no resources", p);
				return false;
			}

			// Check if this part has any resource that would actually flow through a fuel line
			PartResourceDefinition resource;
			foreach (PartResource pr in rl.list) {
				ASPConsoleStuff.printPart ("isFuelTank: part has resource named: " + pr.resourceName, p);
				resource = PartResourceLibrary.Instance.GetDefinition (pr.resourceName);
				if (resource.resourceFlowMode == ResourceFlowMode.STACK_PRIORITY_SEARCH) {
					ASPConsoleStuff.printPart ("isFuelTank: Part IS a fuel tank, has flowable resource: " + pr.resourceName, p);
					return true;
				} else {
					ASPConsoleStuff.printPart ("isFuelTank: resource is not flowable: " + resource.resourceFlowMode.ToString (), p);
				}
			}
			return false;
		}

		static public bool isConnectableFuelTank (Part p)
		{
			// If there are two tanks on top of each other, we don't want to connect both of them
			if (p.parent == null) {
				return (isFuelTank (p));
			} else {
				return ((isFuelTank (p)) && (!isFuelTank (p.parent)));
			}
		}

		static public List<Part> findFuelTanks (List<Part> parts)
		{
			List<Part> tanks = new List<Part> ();
			ASPConsoleStuff.AAprint ("=== Looking for fuel tanks");
			foreach (Part p in parts) {
				//if ((isFuelTank(p)) && (p.symmetryCounterparts.Count>0) && (!isFuelTank(p.parent))){
				if (isConnectableFuelTank (p)) {
					ASPConsoleStuff.printPart ("Adding fuel tank", p);
					tanks.Add (p);
				}
			}
			return tanks;
		}

		static public void AddEmptyStages ()
		{
			ASPConsoleStuff.print ("AddEmptyStages()");
			var editor = EditorLogic.fetch;

			// Get all the parts of the ship
			var parts = editor.ship.parts;

			List<Part> decouplers = new List<Part> ();
			foreach (Part p in parts) {
				if (isDecoupler (p)) {
					if (p.symmetryCounterparts.Count > 0) {
						decouplers.Add (p);
					}
				}
			}
			ASPConsoleStuff.printPartList ("AddEmptyStages decouplers", "decoupler", decouplers);
			List<Part> decouplers2 = decouplers;
			List<int> usedstages = new List<int> ();

			int safetyfactor = 10000;
			while (decouplers.Count > 0) {
				safetyfactor = safetyfactor - 1;
				if (safetyfactor == 0) {
					AutoAsparagus.osd ("Infinite loop in AddEmptyStages, aborting :(");
					AutoAsparagus.mystate = AutoAsparagus.ASPState.IDLE;
					return;
				}

				Part p = decouplers [0];
				ASPConsoleStuff.printPart ("Checking for empty stage", p);

				if (usedstages.Contains (p.inverseStage)) {
					ASPConsoleStuff.printPart ("Adding stage at " + (p.inverseStage + 1).ToString () + " for decoupler", p);
					StageManager.Instance.AddStageAt (p.inverseStage + 1);
					setPartStage (p, p.inverseStage + 1);
				}
				int x = p.symmetryCounterparts.Count / 2;
				while (x > 0) {
					ASPConsoleStuff.print ("AddEmptyStages: x=" + x.ToString ());
					x = x - 1;
					usedstages.Add (p.inverseStage);
				}
				decouplers.Remove (p);
				foreach (Part brother in p.symmetryCounterparts) {
					setPartStage (brother, p.inverseStage);
					decouplers.Remove (brother);
				}
			}

			foreach (Part p in decouplers2) {
				ASPConsoleStuff.printPart ("Adding stage at " + (p.inverseStage + 1).ToString () + " for decoupler2", p);
				StageManager.Instance.AddStageAt (p.inverseStage + 1);
			}
		}

		static public void AsaparagusTheShip (string partName)
		{
			var editor = EditorLogic.fetch;

			// Get all the parts of the ship
			var parts = editor.ship.parts;
			ASPConsoleStuff.printPartList ("All parts of ship", "Part", parts);

			// Find the symmetrical fuel tanks
			List<Part> tanks = findFuelTanks (parts);
		
			ASPConsoleStuff.AAprint ("=== Tanks ===");

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
			List<Part> tanksToStage = new List<Part>();
			foreach (Part p in tanks) {
				if (ASPFuelLine.countDecouplersToRoot (p) > 0) {
					tanksToStage.Add (p);
				}
			}
			int safetyfactor = 10000;
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
					safetyfactor = safetyfactor - 1;
					if (safetyfactor == 0) {
						AutoAsparagus.osd ("Infinite loop in AsaparagusTheShip:tanksToStage.Count:p!=null, aborting :(");
						AutoAsparagus.mystate = AutoAsparagus.ASPState.IDLE;
						return;
					}
					ASPConsoleStuff.printPart ("Adding to chain at position " + chain.Count, p);
					chain.Add (p);

					// don't try to put that tank in another chain
					tanksToStage.Remove (p);

					ASPConsoleStuff.printPart ("Following fuel line from", p);
					Part r = p;
					p = null;
					foreach (Part target in ASPFuelLine.getFuelLineTargets(r,partName)) {
						if (tanks.Contains (target)) {  // we're only following fuel lines in the asparagus
							ASPConsoleStuff.printPart ("..followed fuel line to", target);
							p = target;
						} 
					}
				}

				// Next, look for fuel lines that lead to the start of our chain

				int x = tanksToStage.Count;
				while (x > 0) {
					p = tanksToStage [x - 1]; // get last tank
					ASPConsoleStuff.printPart ("Checking tank " + x.ToString () + " of " + tanksToStage.Count + " to insert in chain", p);
					x = x - 1;

					foreach (Part target in ASPFuelLine.getFuelLineTargets(p,partName)) {
						if (chain [0] == target) {
							ASPConsoleStuff.printPart ("..prepending to chain", target);
							chain.Insert (0, p);
							tanksToStage.Remove (p);
							x = tanksToStage.Count; // reset the countdown
						} 
					}

				}
				ASPConsoleStuff.printPartList ("*** Completed chain", "chain part", chain);
				stageChain (chain);
			}

			// Update staging display
			//StageManager.Instance.DeleteEmptyStages ();
			//StageManager.GenerateStagingSequence (parts [0]);
			//StageManager.Instance.SortIcons (true);
			//StageManager.Instance.UpdateStageGroups (false);

			//foreach (KSP.UI.Screens.StageGroup s in StageManager.Instance.Stages) {
			//}
		}
	}
}

