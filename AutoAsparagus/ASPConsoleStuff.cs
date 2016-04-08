using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

//#if DEBUG
//using KramaxReloadExtensions;
//#endif

namespace AutoAsparagus
{
//#if DEBUG
//	public class ASPConsoleStuff: ReloadableMonoBehaviour
//#else
	public class ASPConsoleStuff: MonoBehaviour
//#endif
	{

		static public void AAprint (string tacos)
		{
			print ("[AutoAsparagus]: " + tacos); // tacos are awesome
		}

		static public void printTransform (string header, Transform t)
		{
			AAprint (header + ": localpos:" + t.localPosition.ToString ("F8") + "/localrot:" + t.localRotation.ToString ("F8"));
			AAprint ("    pos:" + t.position.ToString ("F8") + "/rot:" + t.rotation.ToString ("F8"));
			AAprint ("    scale:" + t.localScale.ToString ("F8") + "/up:" + t.up.ToString ("F8") + "/for:" + t.forward.ToString ("F8"));
		}

		static public void printVector3 (string header, Vector3 v)
		{
			AAprint (header + ": " + v.ToString ("F8"));
		}

		static public void printAttachNode (string header, AttachNode an)
		{
			AAprint ("attachNofde: " + an.id.ToString () + "/" + an.attachMethod.ToString () + "/" + an.nodeType.ToString ());
			AAprint ("      pos:" + an.position.ToString ("F8") + " or:" + an.orientation.ToString ("F8") + " offset:" + an.offset.ToString () + " size:" + an.size.ToString () + "/radius: " + an.radius.ToString ());
			AAprint ("    rq:" + an.requestGate.ToString ());
			if (an.attachedPart == null) {
				AAprint ("    no attachedPart");
			} else {
				print ("    attachedPart" + an.attachedPart.name);  // don't do printPart or it will loop
			}
			if (an.nodeTransform == null) {
				AAprint ("    no nodeTransform");
			} else {
				printTransform ("    nodeTransform", an.nodeTransform);
			}
		}

		static public void dumpPartModule (PartModule pm)
		{
			AAprint ("pm: " + pm.moduleName);
			AAprint ("pm.enabled: " + pm.enabled.ToString () + "/" + pm.isEnabled);
			AAprint ("pm.gettype: " + pm.GetType ().ToString ());
			if (pm.moduleName == "CModuleFuelLine") {
				AAprint ("FUEL LINE!");
				CompoundPart cp = (CompoundPart)pm.part;
				if (cp.target == null) {
					print ("target is null");
				} else {
					printPart ("target", cp.target);
				}

			}

		}

		static private Dictionary<string,string> uglyfriends = new Dictionary<string,string> ();
		static private int friendlyNameCount = 0;
		static private int friendlyNameGeneration = 0;
		static private string[] friendlyNamesStatic = {
			"James",
			"John",
			"Robert",
			"Michael",
			"Mary",
			"William",
			"David",
			"Richard",
			"Charles",
			"Joseph",
			"Thomas",
			"Patricia",
			"Christopher",
			"Linda",
			"Barbara",
			"Daniel",
			"Paul",
			"Mark",
			"Elizabeth",
			"Donald",
			"Jennifer",
			"George",
			"Maria",
			"Kenneth",
			"Susan",
			"Steven",
			"Edward",
			"Margaret",
			"Brian",
			"Ronald",
			"Dorothy",
			"Anthony",
			"Lisa",
			"Kevin",
			"Nancy",
			"Karen",
			"Betty",
			"Helen",
			"Jason",
			"Matthew",
			"Gary",
			"Timothy",
			"Sandra",
			"Jose",
			"Larry",
			"Jeffrey",
			"Frank",
			"Donna",
			"Carol",
			"Ruth",
			"Scott",
			"Eric",
			"Stephen",
			"Andrew",
			"Sharon",
			"Michelle",
			"Laura",
			"Sarah",
			"Kimberly",
			"Deborah",
			"Jessica",
			"Raymond",
			"Shirley",
			"Cynthia",
			"Angela",
			"Melissa",
			"Brenda",
			"Amy",
			"Jerry",
			"Gregory",
			"Anna",
			"Joshua",
			"Virginia",
			"Rebecca",
			"Kathleen",
			"Dennis",
			"Pamela",
			"Martha",
			"Debra",
			"Amanda",
			"Walter",
			"Stephanie",
			"Willie",
			"Patrick",
			"Terry",
			"Carolyn",
			"Peter",
			"Christine",
			"Marie",
			"Janet",
			"Frances",
			"Catherine",
			"Harold",
			"Henry",
			"Douglas",
			"Joyce",
			"Ann",
			"Diane",
			"Alice",
			"Jean",
			"Julie",
			"Carl",
			"Kelly",
			"Heather",
			"Arthur",
			"Teresa",
			"Gloria",
			"Doris",
			"Ryan",
			"Joe",
			"Roger",
			"Evelyn",
			"Juan",
			"Ashley",
			"Jack",
			"Cheryl",
			"Albert",
			"Joan",
			"Mildred",
			"Katherine",
			"Justin",
			"Jonathan",
			"Gerald",
			"Keith",
			"Samuel",
			"Judith",
			"Rose",
			"Janice",
			"Lawrence",
			"Ralph",
			"Nicole",
			"Judy",
			"Nicholas",
			"Christina",
			"Roy",
			"Kathy",
			"Theresa",
			"Benjamin",
			"Beverly",
			"Denise",
			"Bruce",
			"Brandon",
			"Adam",
			"Tammy",
			"Irene",
			"Fred",
			"Billy",
			"Harry",
			"Jane",
			"Wayne",
			"Louis",
			"Lori",
			"Steve",
			"Tracy",
			"Jeremy",
			"Rachel",
			"Andrea",
			"Aaron",
			"Marilyn",
			"Robin",
			"Randy",
			"Leslie",
			"Kathryn",
			"Eugene",
			"Bobby",
			"Howard",
			"Carlos",
			"Sara",
			"Louise",
			"Jacqueline",
			"Anne",
			"Wanda",
			"Russell",
			"Shawn",
			"Victor",
			"Julia",
			"Bonnie",
			"Ruby",
			"Chris",
			"Tina",
			"Lois",
			"Phyllis",
			"Jamie",
			"Norma",
			"Martin",
			"Paula",
			"Jesse",
			"Diana",
			"Annie",
			"Shannon",
			"Ernest",
			"Todd",
			"Phillip",
			"Lee",
			"Lillian",
			"Peggy",
			"Emily",
			"Crystal",
			"Kim",
			"Craigg"
		};

		static public string getFriendlyName (string uglyName)
		{
			if (uglyfriends.ContainsKey (uglyName)) {
				return uglyfriends [uglyName];
			} else {
				if (friendlyNameCount > friendlyNamesStatic.Length - 1) {
					friendlyNameCount = 0;
					friendlyNameGeneration = friendlyNameGeneration + 1;
				}
				string affableName = friendlyNamesStatic [friendlyNameCount];
				if (friendlyNameGeneration > 0) {
					affableName = affableName + " " + friendlyNameGeneration.ToString ();
				}
				friendlyNameCount = friendlyNameCount + 1;
				uglyfriends [uglyName] = affableName;
				return affableName;
			}
		}

		static public void printPart (string header, Part p)
		{
			if (p == null) {
				AAprint (header + ": part is null!");
			} else {
				AAprint (header + ": " + p.name.ToString () + ": "
				+ getFriendlyName (p.craftID.ToString ())
				+ "/" + p.craftID.ToString () + "/"
				+ p.symmetryCounterparts.Count.ToString () + "/"
				+ p.children.Count.ToString () + "/"
				+ p.attachMode.ToString () + "/"
				+ p.inverseStage.ToString () + "/"
				+ p.stageOffset.ToString ()
				);
			}
			#if superDEBUG
			printTransform ("transform", p.transform);
			//AAprint ("  isAttached:" + p.isAttached.ToString () + "/isConnected:" + p.isConnected.ToString ());
			foreach (AttachNode an in p.attachNodes) {
			printAttachNode ("    ", an);
			}

			foreach (PartModule pm in p.Modules) {
			dumpPartModule (pm);
			}
			foreach (Part child in p.children) {
			AAprint ("child: "+child.name.ToString () + ": " + child.craftID().ToString () + "/" + child.symmetryCounterparts.Count.ToString ()+"/"+child.children.Count.ToString());
			}
			#endif
		}

		static public void printPartList (string title, string header, List<Part> parts)
		{
			AAprint ("=== " + title + ": " + parts.Count.ToString () + " parts ===");
			foreach (Part p in parts) {
				printPart (header, p);
			}
		}

		static public void ListTheShip ()
		{
			var editor = EditorLogic.fetch;

			// Get all the parts of the ship
			var parts = editor.ship.parts;
			printPartList ("All parts of ship", "Part", parts);
		}
	}
}

