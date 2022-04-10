---
permalink: /ManualInstallation.html
title: ManualInstallation
description: the flat-pack Kiea instructions, written in Kerbalese, unusally present
# layout: bare
tags: installation,directions,page,kerbal,ksp,zer0Kerbal,zedK
---

<!-- ManualInstallation.md v1.1.2.0
Adjustable Mod Panel (KAMP)
created: 01 Oct 2019
updated: 05 Apr 2022 -->

<!-- based upon work by Lisias -->

# Adjustable Mod Panel (KAMP)

This plugin allows choosing which stock toolbar buttons you want to see. You can show/hide mod stock toolbar buttons on different screens separately. It requires no additional support from the add-ons themselves, and should work with whichever add-ons it encounters. For Kerbal Space Program.

## Installation Instructions

### Using CurseForge/OverWolf app or CKAN

You should be all good! (check for latest version on CurseForge)

### If Downloaded from CurseForge/OverWolf manual download

To install, place the AdjustableModPaenl folder inside your Kerbal Space Program's GameData folder:

* **REMOVE ANY OLD VERSIONS OF THE PRODUCT BEFORE INSTALLING**, including any other fork:
  * Delete `<KSP_ROOT>/GameData/AdjustableModPanel`
* Extract the package's `AdjustableModPanel/` folder into your KSP's GameData folder as follows:
  * `<PACKAGE>/AdjustableModPanel` --> `<KSP_ROOT>/GameData`
    * Overwrite any preexisting folder/file(s).

### If Downloaded from SpaceDock / GitHub / other

To install, place the GameData folder inside your Kerbal Space Program folder:

* **REMOVE ANY OLD VERSIONS OF THE PRODUCT BEFORE INSTALLING**, including any other fork:
  * Delete `<KSP_ROOT>/GameData/AdjustableModPanel`
* Extract the package's `GameData/AdjustableModPanel` folder into your KSP's GameData folder as follows:
  * `<PACKAGE>/GameData` --> `<KSP_ROOT>`
    * Overwrite any preexisting file.

## The following file layout must be present after installation

```markdown
<KSP_ROOT>
  + [GameData]
    + [AdjustableModPanel]
      + [Localization]
        * ...
      + [Plugins]
        * AdjustableModPanel.dll
        * ...
      * #.#.#.#.htm
      * AdjustableModPanel.version
      * changelog.md
      * BSD-2-Clause.txt
      * readme.htm
    * ...
  * KSP.log
  * ...
```

### Dependencies

* none
