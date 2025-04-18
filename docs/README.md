# MonoStereoMod
![icon_workshop](https://github.com/user-attachments/assets/f838ce1e-8162-4556-b94a-8a57eae68b55)

A mod for Terraria that replaces the vanilla audio engine ([FAudio](https://github.com/FNA-XNA/FAudio/tree/master)) with [MonoStereo](https://github.com/NycroV/MonoStereo), a custom audio engine built on top of [NAudio](https://github.com/naudio/NAudio/tree/master) and [PortAudio](https://github.com/PortAudio/portaudio).

Although MonoStereo was originally designed for [MonoGame](https://github.com/MonoGame/MonoGame) projects, it is itself standalone, and can be used in any C# project - like Terraria!

## What is it?

On its own, this mod does not make a difference for players - except for allowing the changing of the audio output device.

However, for mod developers, using MonoStereo opens up endless possibilities for custom audio effects.

With custom audio sources (both sound effects and songs), dynamic filter application, custom filter integration, and much more, the possibilities for audio effects and features are limitless.

> It's also cross-platform!

If you do not want to use MonoStereo in your mod project, no worries! MonoStereoMod is built to work without any changes necessary from other mods, whether they want to support it or not, and does not actively disable FAudio. If you route something directly through FAudio yourself, it will still work completely fine!

## Getting Started
It is extremely easy to get started integrating MonoStereo into your mod! You can head over to the [MonoStereoMod documentation](https://github.com/NycroV/MonoStereoMod/blob/master/docs/USAGE.md) for a basic usage guide.

If you want to get even more advanced and start working on things like custom audio sources, it is recommended to check out the [MonoStereo usage guides](https://github.com/NycroV/MonoStereo/tree/master/docs). There you'll be able to find much more in-depth explanations and examples!

## Demos
### Real-time audio reversal for the Nameless Deity boss fight (Wrath of the Gods)
The music speed is slowed down, sped back up, and reversed in real-time via a custom filter when Nameless Deity's clock attack begins to spin backwards.

https://github.com/user-attachments/assets/031aab84-b8ce-4c7b-afcf-2505a490412a
