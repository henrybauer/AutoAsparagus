using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AutoAsparagus
{
	public class ASPFuelLine: MonoBehaviour
	{

		private class FuelSet {
			public Part fromPart { get; set; }
			public Part toPart { get; set; }
			public FuelLine fl { get; set; }
		}

		private static List<FuelSet> fuelSetsToConnect;

		public static List<Part> getFuelLineTargets(Part p){
			ConsoleStuff.printPart ("...searching fuel lines of part", p);
			List<Part> targets = new List<Part> ();
			foreach (Part child in p.children) {
				if (child is FuelLine) {
					// FuelLine class has a "target" attribute; Part class doesn't, so we have to re-class to access ".target"
					Part fuelLineTarget = ((FuelLine)child).target;
					if (fuelLineTarget != null) {
						ConsoleStuff.printPart ("...found fuel line target", fuelLineTarget);
						targets.Add (fuelLineTarget);
					}
				}
			}
			return targets;
		}

		public static void AttachFuelLine(Part sourceTank, Part destTank){
			print ("=== AttachFuelLine ===");

			// Make a new FuelLine object
			AvailablePart ap = PartLoader.getPartInfoByName ("fuelLine");
			UnityEngine.Object obj = UnityEngine.Object.Instantiate(ap.partPrefab);
			FuelLine f = (FuelLine)obj;
			f.gameObject.SetActive(true);
			f.gameObject.name = "fuelLine";
			f.partInfo = ap;
			f.highlightRecurse = true;
			f.attachMode = AttachModes.SRF_ATTACH;

			print ("    set position in space");
			// set position in space, relative to source tank
			f.transform.localScale = sourceTank.transform.localScale;
			f.transform.parent = sourceTank.transform; // must be BEFORE localposition!

			Vector3 midway = Vector3.Lerp (sourceTank.transform.position, destTank.transform.position, 0.5f);

			Vector3 startPosition = new Vector3 ();
			Vector3 destPosition = new Vector3 ();

			ConsoleStuff.printVector3 ("sourceTank",sourceTank.transform.position);
			print("    dist: "+(Vector3.Distance(sourceTank.transform.position,midway)).ToString("F2"));
			ConsoleStuff.printVector3 ("destTank", destTank.transform.position);
			print("    dist: "+(Vector3.Distance(destTank.transform.position,midway)).ToString("F2"));
			ConsoleStuff.printVector3 ("midway",midway);

			Ray r = new Ray ();
			r.origin = midway;
			r.direction = (sourceTank.transform.position - midway).normalized;

			RaycastHit hit = new RaycastHit();
			if (sourceTank.collider.Raycast (r, out hit, 100)){
				startPosition = hit.point;
				ConsoleStuff.printVector3 ("startPosition", startPosition);
				print("    dist: "+(Vector3.Distance(startPosition,midway)).ToString("F2"));
			} else {
				print (" !!! ray failed!!!");
			}

			r = new Ray ();
			r.origin = midway;
			r.direction = (destTank.transform.position - midway).normalized;
			hit = new RaycastHit();
			if (destTank.collider.Raycast (r, out hit, 100)){
				destPosition = hit.point;
				ConsoleStuff.printVector3 ("destPosition", destPosition);
				print("    dist: "+(Vector3.Distance(destPosition,midway)).ToString("F2"));
			} else {
				print (" !!! ray failed!!!");
			}

			f.transform.position = startPosition;
			f.transform.LookAt (destTank.transform);
			f.transform.Rotate (0, 90, 0);  // need to correct results from LookAt... dunno why.

			print ("    attach to source tank");
			// attach to source tank
			AttachNode an = new AttachNode ();
			an.id = "srfAttach";
			an.attachedPart = sourceTank;
			an.attachMethod = AttachNodeMethod.HINGE_JOINT;
			an.nodeType = AttachNode.NodeType.Surface;
			an.size = 1;
			an.orientation = new Vector3 (0.12500000f, 0.0f, 0.0f); // seems to be a magic number
			f.srfAttachNode = an;

			print ("    attach to destination");
			// attach to destination tank
			f.target = destTank;

			print ("    direction");
			f.direction = f.transform.localRotation * f.transform.localPosition;

			print ("    targetposition");
			f.targetPosition = destPosition;

			// add to ship
			print ("    adding to ship");
			sourceTank.addChild (f);

			EditorLogic.fetch.ship.Add (f);

			FuelSet fs = new FuelSet();
			fs.fromPart = sourceTank;
			fs.toPart = destTank;
			fs.fl = f;
			fuelSetsToConnect.Add (fs);

			Staging.SortIcons ();

			#if KSPdev
			// flush output buffer
			for (int i = 1; i <= 20; i++) {
			print ("");
			}
			#endif

		}

		public static void connectFuelLines() {
			foreach (FuelSet fs in fuelSetsToConnect) {
				ConsoleStuff.printPart ("Connecting FuelLine", fs.fl);
				fs.fl.target = fs.toPart;
			}
		}

		public static void AddFuelLines() {
			// Get all the parts of the ship
			EditorLogic editor = EditorLogic.fetch;
			ShipConstruct ship = editor.ship;
			List<Part> parts = ship.parts;

			fuelSetsToConnect = new List<FuelSet>();

			Part destTank = parts [0];
			Part sourceTank = parts [4];

			ConsoleStuff.printPart ("sourceTank", sourceTank);
			ConsoleStuff.printPart ("destTank", destTank);

			AttachFuelLine(sourceTank,destTank);
		}
	}
}

