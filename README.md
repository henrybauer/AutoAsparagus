AutoAsparagus v0.2
==================

AutoAsparagus mod for Kerbal Space Program

* What does this mod do?
It moves decouplers and sepratrons into proper stages for asparagus staging, based on your fuel lines. It can also create those fuel lines for you!

* What is Asparagus Staging?
See http://wiki.kerbalspaceprogram.com/wiki/Tutorial:Asparagus_Staging

* How do I install this mod?
Same as any other mod, put it in GameData.  See also http://wiki.kerbalspaceprogram.com/wiki/Addon#How_to_install_addons

* How do I use this mod?

In the VAB, build a ship.  The ship should have fuel tanks in symmetry, attached by decouplers to something.

If you already have fuel lines, go to the next step.  Otherwise, press the "Asparagus" or "Onion" buttons to create fuel lines, and then press "2. Connect fuel lines" to connect them.

Find the stage(s) where your decouplers are.  Add some empty stages below each stage.  This isn't required, but it will make a mess otherwise.  If you have 4-way symmetry, you'll need 2 stages total, so add one empty stage.  If you have 6-way, 3 stages total, so add two empty stages. 8-way, 4 stages total, add 3 empty stages.

Press "4. Stage decouplers and sepratrons".

After pushing the staging button, you must save the ship and reload it.  No, I don't know why.  If you don't do this, the staging can revert.  You can just press the button again though :)

* Does the mod support multiple aparagus rings?
  Yes!  You can have one ring that feeds into another, and you can have completely different rings.

* The GUI sucks!
  I know, I know... I was concentrating on functionality first.  Feel free to help with the GUI!

* Sometimes the fuel lines don't connect!
  Sometimes Unity lies to me and says there's nothing in the way, when really there is.  I'm still looking for a more foolproof way to detect collisions.
