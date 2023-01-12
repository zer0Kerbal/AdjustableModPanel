---
permalink: /ManualInstallation.html
title: Manual Installation
description: the flat-pack Kiea instructions, written in Kerbalese, unusally present
tags: installation,directions,page,kerbal,ksp,zer0Kerbal,zedK
---
<!-- ManualInstallation.md v1.1.8.1
Adjustable Mod Panel (KAMP)
created: 01 Oct 2019
updated: 29 Jul 2022 -->

<!-- based upon work by Lisias -->

# Adjustable Mod Panel (KAMP)

[Home](./index.md)

Adjustable Mod Panel (KAMP) plugin provides a window for more precise maneuver node editing in map view.

## Installation Instructions

### Using CurseForge/OverWolf app or CKAN

This plugin allows you to pick which buttons on the stock toolbar you want to see. You can show/hide mod stock toolbar buttons on different screens separately. It requires no additional support from the add-ons themselves, and should work with whichever add-ons it encounters. For Kerbal Space Program (KSP).

### If Downloaded from CurseForge/OverWolf manual download

To install, place the `Morse` folder inside your Kerbal Space Program's GameData folder:

* **REMOVE ANY OLD VERSIONS OF THE PRODUCT BEFORE INSTALLING**
  * Delete `<KSP_ROOT>/GameData/Morse/AdjustableModPanel` * Extract the package's `Morse/` folder into your KSP's GameData folder as follows:
  * `<PACKAGE>/Morse` --> `<KSP_ROOT>/GameData`
    * Overwrite any preexisting folder/file(s).
  * you should end up with `<KSP_ROOT>/GameData/Morse/AdjustableModPanel`

### If Downloaded from SpaceDock / GitHub / other

To install, place the `GameData` folder inside your Kerbal Space Program folder:

* **REMOVE ANY OLD VERSIONS OF THE PRODUCT BEFORE INSTALLING**
  * Delete `<KSP_ROOT>/GameData/Morse/AdjustableModPanel`
* Extract the package's `GameData` folder into your KSP's root folder as follows:
  * `<PACKAGE>/GameData` --> `<KSP_ROOT>`
    * Overwrite any preexisting file.
  * you should end up with `<KSP_ROOT>/GameData/Morse/AdjustableModPanel`

## The following file layout must be present after installation

```markdown
<KSP_ROOT>
  + [GameData]
    + [Morse]
      ...
      + [AdjustableModPanel]
        + [Localization]
          ...
        + [Plugins]
          ...
        * #.#.#.#.htm
        * AdjustableModPanel.version
        * Attributions.htm
        * BSD-2-Clause.txt
        * changelog.md
        * ManualInstallation.htm
        * readme.htm
      ...
    * ModuleManager.ConfigCache
  * KSP.log
  ...
```

### Dependencies

* none