# MonoStereoMod
![icon_workshop](https://github.com/user-attachments/assets/d9573c26-69e0-45aa-a37b-c805f95a3231)

A mod for Terraria that replaces the vanilla audio engine ([FAudio](https://github.com/FNA-XNA/FAudio/tree/master)) with [MonoStereo](https://github.com/NycroV/MonoStereo), a custom audio engine built on top of [NAudio](https://github.com/naudio/NAudio/tree/master) and [PortAudio](https://github.com/PortAudio/portaudio).

Although MonoStereo was originally designed for [MonoGame](https://github.com/MonoGame/MonoGame) projects, it is itself standalone, and can be used in any C# project - like Terraria!

On its own, this mod does not make a difference for players - except for allowing the changing of the audio output device.

However, for mod developers, using MonoStereo opens up endless possibilities for custom audio effects.

With custom audio sources (both sound effects and songs), dynamic filter application, custom filter integration, and much more, the possibilities for audio effects and features are limitless.

## Getting Started
Before getting started with integrating MonoStereo into your mod, it is recommended to check out the [MonoStereo usage guides](https://github.com/NycroV/MonoStereo/tree/master/docs). You will not have to perform any setup, as MonoStereoMod handles all of it for you! However, it is a good idea to familiarize yourself with the MonoStereo programming style - things like applying filters, or how source reading works (if you plan to write any custom filters/sources).

After getting a basic understanding of MonoStereo, you can head over to MonoStereoMod's documentation to see how to effectively integrate it into your mod!

## Demos
Below are some demos of audio effects implemented into Terraria using MonoStereo's capabilities:
