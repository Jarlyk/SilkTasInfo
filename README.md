# Hollow Knight Silksong TAS Tooling

This project provides tooling that runs within the Linux-based Hollow Knight Silksong instance and provides information
to a lua script running on the libTAS side to improve the experience of creating HKSS TASes.  This is a fork
of DemoJameson's original tooling and includes a lot of additional features.  These features are far more
invasive than the original tooling and are not compatible with syncing the unmodified game.  They do, however,
provide mitigations against desyncs, as well as a variety of other more niche features.

## Installation

The release distribution is a zip file that contains one or more HKSS version numbers.  Each corresponds to
a base dll that was used for installing the tooling.  If building the tooling from source, this will be
created in the `bin/Silk TAS Info Tool` folder.

To install, choose the version that corresponds to the version of HKSS you are using and copy the contents into
the main folder of your installation.  This will overwrite the `Assembly-CSharp.dll` under `Hollow_Knight_Silksong_Data/Managed`
as well as copy in some Monomod runtime injection dlls.  It will also place a `SilkTasInfo.config` file
as well as a `SilkTasInfo.lua` file in the main folder of your HKSS installation.  These files are used
for integration on the libTAS side.

## Building

If you wish to build the source directly, you'll need to copy in the dlls from the `Hollow Knight Silksong_Data/Managed` folder into a folder `lib/vxxxxx/` (I recommend using the version number for whatever patch you wish to build for). Then you should be able to build the project by running:

```bash
dotnet build Assembly-CSharp.TasInfo.mm.csproj -c v28324
```

### Build/Deploy Scripts

The repo includes example `build.sh` and `deploy.sh` scripts. You can customize these for your specific setup. The former can be configured for the patch you're using and the latter can be edited to include the folder where you're storing the game.

### Forward Compatibility

Note that the RNG sync features rely on hooking specific usages of random calls in the game code. If the developers add new random calls, the hooking code may need to be updated to reference these new call sites. Currently, this process has been done for v28714 and v28324 and there's still some rng that's not being caught yet.

##  Basic Usage

While the game is running in libTAS, click on the `Tools | Lua Console...` menu item.  From there, click
the `Script | Add script file` menu item.  This will bring up a file dialog; choose the `SilkTasInfo.lua` file that was copied into your HK installation.  The tooling is now running 
and will provide information to the libTAS OSD.  It only loads after the main menu is loaded, so if you don't see anything, 
allow the game to run until the main menu loads.  If you still don't see anything, make sure the `Video | OSD | Lua` 
option is enabled.  Any setting changes will require stepping forward at least one frame to be visible.

While the tooling is running, you can update the configuration at any time by editing the `SilkTasInfo.config`
file at runtime.  The tooling will automatically reload the configuration file when it detects a change.  This allows
for toggling various features (like hitbox display and various text entries) at any time during TASing.  If you make
a mistake while editing the config file, like entering text for a numeric value, it's possible that this might cause 
the tooling to crash.  If this happens, you can fix this by fixing the config file and then reverting to a prior
savestate.

## Configuration Parameters

Editing the configuration file is the primary way to interact with the tooling.  The following parameters are supported:

* `Enabled`: If set to false, will disable the changes that impact the display.  This is effectively a way
to quickly override the various other display parameters without having to edit them individually.
* `ShowTimeOnly`: If set to true, will suppress all of the text in the upper right corner except for Time
and UnscaledTime, if enabled.  This can be used as an easy to embed the loadless timer into an encoded video.
* `ShowCustomInfo`: Whether to display various custom info in the text area.  The custominfo feature is described 
in more detail later in this document.
* `ShowKnightInfo`: Whether to display various knight state flags information, as well as
position and velocity.  Note that when interpreting state flags, they apply to the most recently rendered
frame.  This means if you want to attack on the earliest possible frame, you should send the input on the
frame just prior to the flag appearing in the OSD.
* `ShowSceneName`: Whether to display the name of the currently loaded scene.  In a glitched context with multiple
scenes loaded, note that this displays the name of the scene that `GameManager` thinks is loaded.
* `ShowTime`: Whether to display the current load-removed elapsed time.  This should match the LRT tracked by
the LiveSplit autosplitter.
* `ShowUnscaledTime`: Whether to show real time elapsed from the start of the game.  This is primarily useful
in the context of the MultiSync feature, which is described later in this document.
* `ShowTimeMinusFixedTime`: Whether to display the T-FT for the most recent frame.  This is very useful
in various advanced TASing contexts for HK, since a wide variety of subtle game behaviors are tied to the T-FT value.
* `ShowRng`: Whether to show information on the most recent RNG state.  This tells you how frequently the RNG
is being advanced, which makes it easier to understand what actions are manipulating RNG.
* `ShowEnemyHp`: Whether to display current enemy HP near their hitboxes.  This works for the vast majority of
enemies, though there are some niche exceptions like Watcher Knights in some states.
* `ShowEnemyPosition`: Whether to display the position of enemies near their hitboxes.  This is useful when
attempting to do very precise control of positioning by manipulating enemy movements.
* `ShowEnemyVelocity`: Whether to display the velocity of enemies near their hitboxes.  This is mostly useful
for manipulating the RNG movement of flying enemies to be able to predict how they will move.
* `ShowHitbox`: Whether to display standard hitboxes, including collision hitboxes, enemy hitboxes, 
nail hitboxes, and player hitboxes.
* `ShowOtherHitbox`: Whether to display less frequently used hitboxes, including triggers, background objects
and various enemy sensor boxes.  This can slow down libTAS performance in scenes with a very large number
of hitboxes, so it's recommended to disable this when encoding or catching up.
* `PositionPrecision`: The number of decimal places to display for position.
* `VelocityPrecision`: The number of decimal places to display for velocity.
* `PauseTimer`: While true, this explicitly pauses the loadless timer.  This can be useful when doing spliced TASes.
where you need to start the timer at a specific point in time in the encoding.
* `StartTimer`: When true, this explicitly starts the loadless timer, ignoring the value of StartingSplit.
* `StartingSplit`: The split that starts the timer. Supported options are Act1Start and StartNewGame.
* `EndingSplit`: The split that ends the timer. Supported options are MossMotherTrans, Spool1, Mask1,
MagnetiteBrooch, Widow, SlabKeyHeretic, Mask4, Act2Started, BlastedStepsStation, PutrifiedDuctsStation,
EggofFlealia, MaggotsRemoved, Lace1, YarnabySlap, SisterSplinter, Curveclaw, GreatTasteReward, LastJudge,
SlabStation, Sylphsong, EndingSplit.
* `PauseOnFileSelect`: When true, the timer pauses on the file select menu if there are no black threaded slots.
* `CameraZoom`: The camera zoom level.  This is useful when TASing large rooms where you want to be able to
see distant enemies or colliders.  This is implemented in such a way that it shouldn't cause desyncs, so you
can freely change this while TASing.
* `CameraFollow`: While true, will lock the camera to track the knight's center position.  This is helpful
when doing inventory drops or aquiring far more horizontal momentum than the game intends you to have, as
a way to more easily see what's happening.
* `DisableCameraShake`: Whether to disable camera shake.  This improves the TASing experience and possibly
the viewing experience of the final encoded video if you don't want the shake.
 `DisableFFDuringLoads`: Whether to disable fast forward during loads. This can help improve sync stability, particularly on patch 1432.  Take
care when setting savestates near to loads while using this feature, as a savestate inside a non-FF zone might
preempt the fast forward protection, especially if immediately adjacent to the actual scene change.

## Custom Info Text

In the config file, under [CustomInfoTemplate], you can specify various custom text to display in the upper right info.
The format can be inferred somewhat from the commented out example entries in the config file, but here are the
major features:

* All entries can specify fixed text as well as a bound value.  The bound value is in curly braces.  For example,
`paused: {GameManager.isPaused}` will display whether the game is paused.
* Fields can be referenced using `{ClassName.fieldName}`, where `GameManager`, `HeroController`, 
`PlayerData` and `HeroControllerStates` all have special support to find them throuugh the active `GameManager` instance.
* Other classes are found using `Object.FindObjectdOfType`.
* In general, you can pass simple methods in place of fields and it will call them.  For example, 
`canAttack: {PlayerData.CanAttack()}`.
* For game objects, you can specify the game object name and it will call `GameObject.Find` internally to find it.  For
example, `crawler hp: {Crawler Fixed.LocateMyFSM(health_manager_enemy).FsmVariables.FindFsmInt(HP)}` will look for
an object named `Crawler` and then look up the HP variable on its health_manager_enemy FSM.  The supported methods here
are `LocateMyFSM` and `GetComponentInChildren`.

## RNG Synchronization

Silksong has particularly unstable RNG, which normally makes input splicing effectively impossible and can cause
desyncs.  The tooling supports synchronizing RNG on a scene by scene basis, which
can, in principle, be used for splicing.  This involves recording every RNG call in the game and creating a log file
in a `Recording/RNG` folder as described above.  If the game then finds a `Playback/RNG` folder, it will then
attempt to play back these RNG values while the game is running.

To output a RNG recording up to the current frame, send a '=' input. Make sure writing to disk is not disabled when you do this.

## Commands File

When segmenting into multiple movies (which is highly recommended for any long TASes of this game), you sometimes need to synchronize starting silk and health, as you have done a S+Q at a point which nominally should have been continuous. You can accomplish this by using a placing a `Commands.txt` in the `Playback` folder at the same level as the game, which is a series of comma-delimited lines of the format:

```
Time,Command,Value
```

For example, this `Commands.txt` sets Silk to 9 and Health to 4 at 20 seconds in to the movie. Currently these are the only two supported command types. In order for this to work, make sure to set this _after_ you're loaded into a scene in the movie.

```
20.0,SetSilk,9
20.0,SetHealth,4
```

## Acknowedgements

* Thanks to Kilaye for creating the libTAS tool, which makes all of this possible
* Thanks to DemoJameson for the initial version of the HK TAS tooling on which this was based
* Thanks to all of my fellow TASers on the Hollow Knight TAS Discord, both for direct contributions 
to the tooling and also for general feedback and suggestions

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details