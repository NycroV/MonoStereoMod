# MonoStereoMod Usage
## Adding MonoStereoMod to Your Mod

> Before you can start working with MonoStereoMod, you first need to add it as a project reference and mod reference. Under normal circumstances, you'd need to add all of the mod's dependencies as references as well - however, MonoStereoMod eliminates this step in the process by bundling itself, along with all of its dependencies, together into one single .dll file.

> Head on over to the [releases](https://github.com/NycroV/MonoStereoMod/releases) tab to find the latest dependency packages!

After downloading the .dll file, place it into a folder named `lib` within your mod directory.<br/>
Then, inside of your `build.txt` file, ensure that `MonoStereoMod` is included in your `modReferences`, and `MonoStereoMod.Dependencies` is included in your `dllReferences`.

![image](https://github.com/user-attachments/assets/c35180b8-1d23-41d6-b605-62cde00962c7)

Lastly, add the .dll as a project reference within your IDE. If you're in Visual Studio, right click your project in the solution explorer, and choose "Add Project Reference." Navigate to your .dll file within the `lib` folder, and select it.

Now you're ready to start developing!

***

## Accessing and Using MonoStereo Components

Now that MonoStereoMod has been set up to be interacted with, it's time to... well, interact with it!

MonoStereoMod does all the heavy lifting of implementing MonoStereo for you. It replaces Terraria's underlying audio engine, FMOD, with MonoStereo - while still leaving top level accessibility unchanged. This means that using or interacting with any of Vanilla's audio components still works perfectly fine, and will produce the desired results. However, it also exposes a number of methods as a public facing API that allow you to directly interact with those new underlying components.

> **Note:**<br/>
The following methods expect at least a basic understanding of how MonoStereo operates.<br/>
The explanations will still try to explain everything in as much detail as possible to allow you to work with MonoStereo purely off of information gained in this documentation, but keep in mind that some topics may be more advanced than is worth explaining here. You can always reference the [MonoStereo documentation](https://github.com/NycroV/MonoStereo/tree/master/docs) for more, in-depth documentation!

***
#### Music

<details>
<summary>Retrieving Music</summary>
  
### Retrieving Music

The main way to interact with MonoStereo music components is with the following:
```cs
MonoStereoAudioTrack music = MonoStereoMod.GetSong(int musicIndex);
```
`musicIndex` should be the index of the registered music track you want to access. This is commonly retrieved by either keeping track of the index when initially adding the track, or by accessing `Main.curMusic`.

The returned object is a `MonoStereoAudioTrack` that represents the specified track.

You have access to a number of properties and methods on track objects. You can access `IsPlaying`, `IsPaused`, and `IsStopped` for quick playback info -- alternatively, `PlaybackState` represents this as an enum. You can also access `Pitch` and `Pan` for basic audio adjustment on the fly, and `Position` or `Seek()` for repositioning.

</details>

<details>
<summary>Adding Filters to Music</summary>
  
### Adding Filters to Music

In order to utilize more advanced audio modification, you'll want to add some extra filters to the track. This can be done with `track.AddFilter(myFilter)`.<br/>
For a look at what filters are available, how to use them, and how to write your own filters, check out the [MonoStereo filter documentation](https://github.com/NycroV/MonoStereo/blob/master/docs/FILTERS.md).

If you want to globally apply a filter to ALL music tracks, you can use `AudioManager.MusicMixer.AddFilter(myFilter)`.<br/>
This applies a filter to the actual music mixer - all the currently playing music is mixed together, and then the filter is applied to that result.

To remove a filter once you're done, use `RemoveFilter(myFilter)`.

If you expect a track will have lots of filters applied to it at one time, make sure to read through the next section to ensure you're getitng the best performance.

</details>

<details>
<summary>Using Custom Music Sources</summary>
  
### Using Other Music Sources

MonoStereoMod also allows you to register your own music sources to Terraria's music loader!

If you have a track that you expect will have lots of filters (or particularly slow filters) applied to it at one time, it is recommended to use one of MonoStereoMod's custom "high performance" sources. This changes the way that MonoStereo buffers your audio, and can greatly improve performance when audio processing is expecting to take a slightly long time.

By default, MonoStereo will cache up to 5 seconds of a song at a time in memory, to ensure that there are always samples ready for processing and playback, which is still done in real-time. When a song is registered as high performance, the entire song is loaded into memory whenever the song is set to be played. This means that no matter what happens with any of the filters that are applied, source samples are always available for reading. When a song stops, its data is unloaded to reduce memory usage.

To register a song as high performance, use this:
```cs
MusicLoader.AddMusic(Mod mod, string musicPath); // Vanilla

// Replace the above with the following...

MonoStereoMod.AddHighPerformanceMusic(Mod mod, string musicPath); // MonoStereo
```

From this point on, you can interact with the track as normal. MonoStereoMod handles all the rest!

Additionally, if you think your mod may end up applying a lot of filters, or particularly slow filters, to potentially any or all songs in the game, you can signal to MonoStereoMod that you want ALL songs to use high performance readers instead of the default. Simply add the `MonoStereoMod.ForceHighPerformanceAttribute` attribute to your `Mod` class.
```cs
[MonoStereoMod.ForceHighPerformance]
public class MyMod : Mod
{ ... }
```

MonoStereoMod also supplies support for implementing your own custom music sources. Think of something like a live radio.<br/>
If you want to create a custom music source, it is highly recommended to read the [MonoStereo documentation](https://github.com/NycroV/MonoStereo/blob/master/docs/CUSTOM_SOURCES.md), as that topic is considerably more complex and requires more investment than would be worthwhile here.

If you have a custom music source you want to add as a song, you can use:
```cs
MonoStereoMod.AddCustomMusic(Mod mod, string musicName, ISongSource source);
// or...
MonoStereoMod.AddCustomMusic(Mod mod, string musicName, MonoStereoAudioTrack track);
```

Using the first overload will attach your `ISongSource` implementation to a default instance of the `MonoStereoAudioTrack` class.<br/>
Using the second overload allows you to directly supply your track instance, which means you can extend the class to add extra functionality!

Both of these methods should only be used if you know what you're doing.

</details>

***
#### Sounds

<details>
<summary>Retrieving Sounds</summary>

### Retrieving Sounds

When you want to retrieve sounds, you have a couple of options. The first is with how you actually play the sound.
```cs
// Instead of using...
ActiveSound sound = SoundEngine.PlaySound(in SoundStyle style, Vector2? position, SoundUpdateCallback callback);

// Use this.
MonoStereoSoundEffect sound = MonoStereoMod.PlaySound(in SoundStyle style, Vector2? position, SoundUpdateCallback callback);
</details>
```

All you need to do is trade out the `SoundEngine` class for `MonoStereoMod`, as all of the method parameters are exactly the same.<br/>
By doing this, the method will return the actual MonoStereo sound component, as opposed to the `SlotId`/`ActiveSound` layer that sits on top of it.<br/>
You can then hold onto this instance for as long as you need.

The other options are for when a sound has already been played and you only have the slot ID or ActiveSound representation:

```
bool success = MonoStereoMod.TryGetActiveSound(SlotId slotId, out MonoStereoSoundEffect sound);
// or...
bool success = MonoStereoMod.TryGetActiveSound(SlotId slotId, out MonoStereoSoundEffect sound);
```

These methods take in either a `SlotId` or `ActiveSound` object, returned by vanilla's methods, and attempts to retrieve the corresponding MonoStereo component.<br/>
This method will return true if the correct sound was able to be found, or false if it was not (such as if the `SlotId` was invalid, or the sound has been completed and picked up by garbage collection).

</details>

<details>
<summary>Adding Filters to Sounds</summary>

### Adding Filters to Sounds

In order to utilize more advanced audio modification, you'll want to add some extra filters to your sounds. This can be done with `sound.AddFilter(myFilter)`.<br/>
For a look at what filters are available, how to use them, and how to write your own filters, check out the [MonoStereo filter documentation](https://github.com/NycroV/MonoStereo/blob/master/docs/FILTERS.md).

If you want to globally apply a filter to ALL sound effects, you can use `AudioManager.SoundMixer.AddFilter(myFilter)`.<br/>
This applies a filter to the actual sound mixer - all the currently playing sounds are mixed together, and then the filter is applied to that result.

To remove a filter once you're done, use `RemoveFilter(myFilter)`.

</details>

<details>
<summary>Using Other Sound Sources</summary>
 
### Using Other Sound Sources

MonoStereoMod also supplies support for implementing your own custom sound sources. Think of something like a voice chat or radio.<br/>
If you want to create a custom sound source, it is highly recommended to read the [MonoStereo documentation](https://github.com/NycroV/MonoStereo/blob/master/docs/CUSTOM_SOURCES.md), as that topic is considerably more complex and requires more investment than would be worthwhile here.

Because Terraria does not keep track of sounds in the same way it does songs (since sounds are "fire-and-forget"), using custom sound sources is very easy and does not require any extra code to integrate your sound with Terraria's systems like songs do.

Once you have a custom sound source implementation, simply use the MonoStereo default usage to play it.
```cs
SoundEffect sound = SoundEffect.Create(myCustomSoundEffectSource);
sound.Play();
```

</details>

***

#### Other Tips

In addition to applying filters to songs and sounds, as well as the music and sound mixers, you also have access to the `AudioManager.MasterMixer`. This allows you to apply filters to the global audio output, meaning it is applied after EVERY other filter has been applied and audio source has been mixed.

Also keep in mind that, if you're using systems to apply filters to tracks, you remember to remove filters when necessary, such as when exiting a world. Leaving filters applied to tracks when unintentional can cause them to never be removed, as filters are not reset automatically. MonoStereoMod WILL remove filters for you when a track stops, and when the mod unloads, so you don't need to worry about doing anything then.

Once again, if you intend to do more complex feats such as custom sources, it is *highly* recommended to read and familiarize yourself with the [MonoStereo documentation](https://github.com/NycroV/MonoStereo/tree/master/docs) beforehand.<br/>
If you have any questions that you cannot find answers to within the documentation here nor within the MonoStereo documentation, feel free to reach out to me on Discord @nycro.

Happy coding!
