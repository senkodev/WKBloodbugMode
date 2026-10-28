# Changelog

## 1.0.0

- Initial release

## 1.0.1
- Update README.md and manifest description

## 1.0.2
- Refined binding descriptions for improved in-game readability
- Fixed the hardcoded bind key for the charge/feed button in the Bloodbug binding descriptions
- Reduced the on foot speed debuff from 35% to 30%
- Added a falling buff, a fall has to be twice as long as the climber's before it hurts
- Hunger slows you down a bit later, flight speed debuff (while hungry) changed from 60% to 75%
- Bigger stamina bar and a bit less drain while flying, stamina also refills a little faster while grounded
- Wet wings dry in 15 seconds instead of 20
- The charge does 3 damage instead of 2, and any denizen holds 70 stamina worth of blood instead of 60
- The camera tilts a few degrees more when flying sideways
- Bloodbug runs give a 0.35 score multiplier instead of 0.25
- The Moon Rocks trinket makes flying drain 25% less stamina

## 1.0.3
- **Half Inventory** and **Survival Mode are no longer added with the binding**. Pick them yourself for the full experience!
- Vent covers and other stuck props can be grabbed and thrown properly now
- Fans and vents now push the bloodbug as hard as the climber
- Hammers are refused on pickup and removed from the inventory by the mod itself, instead of relying on the framework
- A hammer spawned into the inventory no longer weighs you down (this was due to framework's behavior)
- Stinging the barnacle's tongue while grappled with the bite key now sets you free
- Mod setting `AllowClimbing` now lets you grab handholds, enabled by default
- Roaches you hold can be eaten for stamina, with the eating animation and sound of the lemon roach
- The `infinitestamina` console command now gives infinite flight stamina
- Drones and turrets can't be eaten now :D
- Grubs can be crushed again
- Own Bloodbug sounds and in-game music are no longer muffled by the bug hearing
- A Bloodbug you attack properly fights back now >:)
- Charging into the flowers from the Nest now works the same as shooting at them

## 1.0.4
- Adjusted the shot origin to better accommodate the Bloodbug's scale, shots no longer hit the ground in front of you on uneven surfaces
- You can now grind yourself in the recycler, it even pays whole **three roaches** for it :D (don't do it..)
- Various balancing changes to max stamina, stamina drain and regen
- Flying stamina now regains at half the speed when walking on a wall/ceiling
- Hanging onto handholds now regains flying stamina a bit faster
- Nerfed the Moon Rocks trinket from 25% to 20% less stamina drain
- Removed "fans" from the Light Body description since they no longer throw the Bloodbug further than the Climber
- Added a link to Sckurge's playthrough of the mod <3, as well as credits for UnityExplorer to the README

## 1.0.5
- CiCi's Trinket & Binding Framework is now optional. With it installed the binding still goes through it and shows up as a mod binding, without it the mod adds the binding to the game itself!
- Flight is disabled while a scripted event drags you along, like the Mother pulling you back to her, so it no longer forces you into flight and drains your stamina

## 1.0.6
- Fixed the mod not working after the Shortcuts and Halloween updates
- A game update removing a method the mod patches now only disables that one patch instead of the whole mod, with an error in the log saying which patch failed
- Added a second patch to avoid leaderboard uploads, just in case this gets updated in the game again
