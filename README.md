# Bloodbug Mode

A White Knuckle mod that adds a new binding called **Bloodbug Mode**. Pick it before a run and you play as a **Bloodbug**. You are tiny, you fly and you land on walls instead of climbing them.

Works in Campaign and Endless. Runs with this binding are not sent to the leaderboards.

Needs [CiCi's Trinket & Binding Framework](https://thunderstore.io/c/white-knuckle/p/CiCisMods/CiCisTrinketAndBindingFramework/).

I have spent countless hours researching things about flies, like about how they see and hear and overall perceive the world, and tried my best to replicate the effects through this mod.

Suggestions and bug reports are welcome through **Issues** in the GitHub repository!

## Showcase

https://github.com/user-attachments/assets/696da866-aa4a-494a-b858-7502df89421b

## Installing

With a Mod Manager like r2modman:

1. Pick White Knuckle and a profile
2. Search for Bloodbug Mode in the online mods and install it. BepInEx and the framework come along on their own, otherwise install them as well
3. Start the game modded

If you have the zip instead, use `Settings > Profile > Import local mod` in r2modman and install the framework from the online mods yourself.

Manual install:

1. Install [BepInEx 5](https://thunderstore.io/c/white-knuckle/p/BepInEx/BepInExPack/) into the game folder and start the game once
2. Install [CiCi's Trinket & Binding Framework](https://thunderstore.io/c/white-knuckle/p/CiCisMods/CiCisTrinketAndBindingFramework/)
3. Unzip this mod into its own folder (!) inside `BepInEx/plugins`. Keep the images next to the dll, the mod loads them from there

Then start a new run and pick Bloodbug Mode from the bindings.

## Buffs

- You can fly. Flying uses a stamina bar that fills up again on the ground and on walls
- You can land on walls and ceilings and crawl on them, while slowly regaining stamina
- Press `V` while flying to charge at a denizen. The charge also sets off anything a rebar would, like the flowers
- Hold `V` next to a body to drink from it. Also works on alive denizens. That gives you stamina and a bit of health and fills the hunger bar
- If a barnacle caught you with its tongue, press `V` to sting the tongue and get free
- A roach you hold can be eaten. It gives some stamina and fills the hunger bar a little, platinum roaches give more and the ruby the most
- Injectors make you fly faster while they last and pills give you infinite stamina for 35 seconds
- The Moon Rocks trinket makes flying drain 25% less of the flight stamina
- You float on water for a few seconds and can take off from it
- Falling only hurts from two times the usual height
- You can't crush roaches with your weight (just like in Roach Mode)
- Other Bloodbugs leave you alone

Additionally the Mother treats you as a relative, the same way she does in Roach Mode (can be configured through the mod setting `MotherKin`). You get the same scenes and the same choice at the end, with some of her lines changed to fit correctly. If you leave with her the run ends as "Bloodbug Mode - Escape (Mod)". It is saved under that name and leaves your Roach Mode progress alone.

## Debuffs

- Pick **Half Inventory** and **Survival Mode** for the full Bloodbug experience!
- You are 30% slower on foot
- You fly slower when your hunger bar is low, down to 75% of your speed when the hunger bar is empty
- You carry boxes and other props with half the strength, so they feel twice as heavy. Planks and vent covers come off as usual

Additionally you can't:
- Use hammers. You start without one and can't pick one up
- Throw rebar
- Place pitons of any kind
- Use computers
- Stay underwater for more than 10 seconds

Levers, cranks, buttons, vending machines and the roach trader will still work. If you grab a lever or a crank while flying, you will hang onto it so you don't drift away.

You can also grind yourself in the recycler and get whole **three roaches** for it! (don't even try this...)

## Viewpoint

The in-game camera bulges like a fisheye lens, you also have protanopia and things far away are blurry. You also have bug legs instead of hands.

If you don't like it you can turn off `BugView` in the config.

## Hearing

You only hear what is close, anything further than 15 metres away is silent. What you do hear is dull, because an actual fly does not have ears, and it's antennae pick up low hums much better than higher pithced sounds.

You can turn this off with `BugHearing` in the config.

## Settings

The settings are in `BepInEx/config/senkodev.whiteknuckle.bloodbugmode.cfg`. The file shows up after the first launch. If you have the "Mod Menu" mod installed you can change it all in game too.

The bite key is `V` by default. If you change it to a key the game already uses the mod will warn you.

`AllowClimbing` is on by default. Turn it off and handholds won't be grabbable anymore, you will be able to fly and land on surfaces only.

You can also get the perk in the middle of a run with the console command `addperk Perk_Senkodev_BloodbugForm`.

## Building

You will need the .NET SDK and the game with [BepInEx](https://old.thunderstore.io/c/white-knuckle/p/BepInEx/BepInExPack/) and [CiCisTrinketAndBindingFramework](https://old.thunderstore.io/c/white-knuckle/p/CiCisMods/CiCisTrinketAndBindingFramework/) **already installed**.

```
dotnet build src/BloodbugMode.csproj -c Release
```

This also copies the mod into your r2modman profile. If your game and/or profile are somewhere else, copy `LocalPaths.props.example` to `LocalPaths.props` and put your paths in there. Add `-p:Deploy=false` if you only want to build the mod.

`tools/package.ps1` makes the zip package for the Thunderstore listing.

## Credits

This mod is dedicated to **Sckurge**, who you can find on [Twitch](https://www.twitch.tv/sckurge) and [YouTube](https://www.youtube.com/@SckurgeWK)!
You can also find his playthrough of the mod on [YouTube](https://www.youtube.com/watch?v=xTYvCStfkWo) :)

- Dark Machine Games for White Knuckle
- CiCi for the [Trinket & Binding Framework](https://thunderstore.io/c/white-knuckle/p/CiCisMods/CiCisTrinketAndBindingFramework/)
- The [BepInEx](https://github.com/BepInEx/BepInEx) and [HarmonyX](https://github.com/BepInEx/HarmonyX) teams for the mod loader and patching
- sinai-dev and yukiaiji for [UnityExplorer](https://github.com/yukieiji/UnityExplorer)
- The idea was inspired by the original Roach Mode in the game. Code, idea, design and testing are by [senkodev](https://senko.dev)

## License

MIT.
