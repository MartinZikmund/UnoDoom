# UnoDOOM

A cross-platform DOOM game built with **Uno Platform**, play it on **iOS, MacCatalyst, Android, Windows, Linux, and WebAssembly**. Supports keyboard, touch, and game controller input.

Uses a modified C# Doom engine from [ManagedDoom](https://github.com/sinshu/managed-doom).

## Try it out [in the browser](https://happy-tree-0e73b8e10.3.azurestaticapps.net/)

**Forked from the amazing work by [@taublast](https://github.com/taublast) at https://github.com/taublast/Doom.Mobile**!

## Why Another .NET DOOM?

* True cross-platform implementation using Uno Platform for iOS, MacCatalyst, Android, Windows, Linux, and WebAssembly.
* Single codebase targeting multiple platforms with native performance.
* Mobile touch gestures and keyboard support for desktop.
* Custom UI to select weapons on mobile, tap left-bottom corner to open.
* Multi-channel stereo sound working on all platforms.

## How To Play

### Setup

While DOOM source code was openly released for non-profit use, it requires you to own a real copy of one of the DOOMs. 
This project code looks for a doom data `.wad` file. It searches for one of the following:
```
    "doom2.wad",
    "plutonia.wad",
    "tnt.wad",
    "doom.wad",
    "doom1.wad",
    "freedoom2.wad",
    "freedoom1.wad",
```
You can find out more about this subject by googling one of these filenames.

This repo contains `freedoom2.wad` for a fast start, [free content under BSD licence](https://freedoom.github.io/). You can replace it with your own file.

### Building

Use `UnoDoom.sln` to build and run the project. The solution supports multiple target frameworks:

* **Windows Desktop**: `net10.0-desktop`
* **WebAssembly**: `net10.0-browserwasm`
* **iOS/MacCatalyst/Android**: Additional platform targets available

Build tasks are available in VS Code:
* `build-wasm` - Build for WebAssembly
* `build-desktop` - Build for Windows Desktop
* `publish-wasm` - Publish WebAssembly build
* `publish-desktop` - Publish Desktop build

### Performance

* On Windows Desktop you can play a Debug version even when debugging.
* On Android to have playable fps you need to compile a Release and run it on a real device, it runs smoothly even on a slow device.
* On iOS both simulator and real device are fine to play without debugging.
* On Mac (Catalyst) when starting without debugging you can play even a Debug build.
* WebAssembly runs smoothly in modern browsers.

### Controls

* Inside MENU panning replaces arrow keys.
* ESC: left-top screen corner. ENTER is everywhere but this corner when menu is open.
* While playing: panning replaces mouse, tap to FIRE and tap on your avatar to USE, open doors etc. 
* Switch weapons by tapping in the lower-left corner of the screen.
* Open auto-map by tapping in the right-top corner.
* On desktop you can also use usual keyboard keys, default is FIRE with CONTROL, USE with SPACE.
* Mouse on desktop behaves differently from original DOOM as this version is touch-screen-friendly in the first place.

Could be much improved, not only the gestures code, but also maybe could add some HUD buttons for movement. Please leave your thoughts in Discussions.

## Behind The Scenes

Stack: [Uno Platform](https://platform.uno/), [SkiaSharp](https://github.com/mono/SkiaSharp).

* Reusing modified C# Doom engine of [ManagedDoom](https://github.com/sinshu/managed-doom).
* Video: Hardware-accelerated SkiaSharp rendering via Uno Platform's SkiaSharp integration.
* Input: Uno Platform's cross-platform input handling with touch gestures and full keyboard support.
* Game controller support on Android, iOS, and WASM
* Targets .NET 10.

## Dev Notes

* Use `UnoDoom.sln` to build and play.
* As DOOM computes its walls on CPU, do not try to debug this on mobile. See performance notes above.
* The final texture is rendered using SkiaSharp so it can be modified in the future to do more work on GPU for the DOOM engine.
* Projects are separated into shared code (`ManagedDoom.Shared`) and Uno Platform implementation (`UnoDoom`).
* The original ManagedDoom Silk for Windows implementation is kept in `ManagedDoom` folder to serve as a development reference.

## To Do

* Enhance Input with controller support and maybe add HUD buttons.
* UI for selecting one of the WADs when many are found.
* Track selected weapon and highlight its number in custom UI.
* MIDI music support.
* Additional platform targets (Linux native, etc.)

## Ancestors

[DOOM](https://github.com/id-Software/DOOM) -> [Linux Doom](https://github.com/id-Software/DOOM) -> [ManagedDoom](https://github.com/sinshu/managed-doom) -> [Doom.Mobile](https://github.com/taublast/Doom.Mobile) -> this project.

## License

The [GPLv2 license](LICENSE.txt) inherited from ancestors.
