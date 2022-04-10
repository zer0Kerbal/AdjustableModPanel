/*
BSD 2-Clause License

Copyright © (c) 2017,2019 radistmorse
Copyright © (c) 2021,2022 zer0Kerbal (zer0Kerbal at hotmail dot com)
All rights reserved.


Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.Linq;
using UnityEngine;
using KSP.UI.Screens;
using System.Collections.Generic;
using KSP.Localization;

namespace AdjustableModPanel {
  [KSPAddon (KSPAddon.Startup.MainMenu, true)]
  public class AdjustableModPanel : MonoBehaviour {
    private class ModDescriptor {
      public string module;
      public string method;
      public uint textureHash;
      public bool textureHashNeeded = false;
      public bool unmanageable = false;

      public ApplicationLauncher.AppScenes defaultScenes;
      public ApplicationLauncher.AppScenes requiredScenes;
      public Texture2D modIcon;
    }

    private List<ModDescriptor> descriptors = new List<ModDescriptor> ();
    private List<ModDescriptor> tempDescriptors;
    private Dictionary <ModDescriptor, ApplicationLauncher.AppScenes> currentScenes = new Dictionary<ModDescriptor, ApplicationLauncher.AppScenes> ();
    private Dictionary <ModDescriptor, ApplicationLauncher.AppScenes> pendingChanges = new Dictionary<ModDescriptor, ApplicationLauncher.AppScenes> ();
    private Dictionary <ModDescriptor, bool> pinnedMods = new Dictionary<ModDescriptor, bool> ();

    private ApplicationLauncherButton appButton = null;
    private Texture2D appButtonTexture;
    private Sprite normalPin;
    private Sprite highlightedPin;
    private Sprite activePin;
    private EventData<GameScenes>.OnEvent updateCallback1 = null;
    private Callback updateCallback2 = null;

    private GameObject mainWindow = null;


        // #KAMP-010 = KSC // #autoLOC_300900 = KSC
        // #KAMP-011 = SPH/VAB // #autoLOC_6002119 = SPH #autoLOC_6002108 = VAB
        // #KAMP-012 = FLT // #autoLOC_900586 = Flight
        // #KAMP-013 = MAP // #autoLOC_308130 = Map View
        // #KAMP-014 = TRS // #autoLOC_6001648 = Tracking Station #autoLOC_6002222 = Tracking
        // #KAMP-015 = ALL // #autoLOC_900712 = All

        private List<KeyValuePair<ApplicationLauncher.AppScenes, string>> appScenesNames = new List<KeyValuePair<ApplicationLauncher.AppScenes, string>>() {
      new KeyValuePair<ApplicationLauncher.AppScenes, string> (ApplicationLauncher.AppScenes.SPACECENTER, Localizer.Format("#autoLOC_300900")), // KSC
      new KeyValuePair<ApplicationLauncher.AppScenes, string> ((ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB), Localizer.Format("#autoLOC_6002119") + "/" + Localizer.Format("#autoLOC_6002108")), // "SPH/VAB"
      new KeyValuePair<ApplicationLauncher.AppScenes, string> (ApplicationLauncher.AppScenes.FLIGHT, Localizer.Format("#autoLOC_900586")), // "FLT"
      new KeyValuePair<ApplicationLauncher.AppScenes, string> (ApplicationLauncher.AppScenes.MAPVIEW, Localizer.Format("#autoLOC_308130")), // "MAP"
      new KeyValuePair<ApplicationLauncher.AppScenes, string> (ApplicationLauncher.AppScenes.TRACKSTATION, Localizer.Format("#autoLOC_6002222")), // "TRS"
      new KeyValuePair<ApplicationLauncher.AppScenes, string> ( (ApplicationLauncher.AppScenes)63, Localizer.Format("#autoLOC_900712")), // "ALL"

    //private List< KeyValuePair <ApplicationLauncher.AppScenes, string> > appScenesNames = new List< KeyValuePair <ApplicationLauncher.AppScenes, string> >  () {
    //  new KeyValuePair<ApplicationLauncher.AppScenes, string> (ApplicationLauncher.AppScenes.SPACECENTER, "KSC" ),
    //  new KeyValuePair<ApplicationLauncher.AppScenes, string> ((ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB), "SPH/VAB" ),
    //  new KeyValuePair<ApplicationLauncher.AppScenes, string> (ApplicationLauncher.AppScenes.FLIGHT,  "FLT" ),
    //  new KeyValuePair<ApplicationLauncher.AppScenes, string> (ApplicationLauncher.AppScenes.MAPVIEW, "MAP" ),
    //  new KeyValuePair<ApplicationLauncher.AppScenes, string> (ApplicationLauncher.AppScenes.TRACKSTATION, "TRS" ),
    //  new KeyValuePair<ApplicationLauncher.AppScenes, string> ( (ApplicationLauncher.AppScenes)63, "ALL" )
    };

    static internal AdjustableModPanel Instance { get; private set; } = null;

    #region ToolbarControl

    private bool ToolbarControllerAvailable = true;
    private Type ToolbarControllerType = null;
    private System.Reflection.MethodInfo ToolbarControllerMethod = null;

    public bool IsButtonToolbarController (ApplicationLauncherButton button, out string name, out string id) {
      if (!ToolbarControllerAvailable) {
        name = null;
        id = null;
        return false;
      }
      if (ToolbarControllerType == null) {
        ToolbarControllerType = AssemblyLoader.loadedAssemblies
          .Select (a => a.assembly.GetExportedTypes ())
          .SelectMany (t => t)
          .FirstOrDefault (t => t.FullName == "ToolbarControl_NS.ToolbarControl");
        if (ToolbarControllerType == null) {
          Debug.Log ("[Adjustable Mod Panel] INFO: ToolbarControl mod not found");
          ToolbarControllerAvailable = false;
          name = null;
          id = null;
          return false;
        }
      }
      if (ToolbarControllerMethod == null) {
        ToolbarControllerMethod = ToolbarControllerType.GetMethod ("IsStockButtonManaged", 
          System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
          null,
          new Type[] { typeof(ApplicationLauncherButton), typeof(string).MakeByRefType() , typeof (string).MakeByRefType(), typeof (string).MakeByRefType() },
          null);
        if (ToolbarControllerMethod == null) {
          Debug.Log ("[Adjustable Mod Panel] INFO: ToolbarControl mod found, but the method is absent");
          ToolbarControllerAvailable = false;
          name = null;
          id = null;
          return false;
        }
        Debug.Log ("[Adjustable Mod Panel] INFO: Found ToolbarControl mod, will check mods from now on");
      }
      System.Object[] parameters = new System.Object[]{button, null, null, null};
      bool rez = false;
      try {
        rez = (bool)ToolbarControllerMethod.Invoke (null, parameters);
      } catch (Exception e) {
        Debug.Log ("[Adjustable Mod Panel] WARNING: Exception is caught while calling ToolbarControl: " + e.ToString());
      }
      name = (string)parameters[1];
      id = (string)parameters[2];
      return rez;
    }

    #endregion

    #region Main window

    private class ToggleParameters : MonoBehaviour {
      public ModDescriptor currentModKey;
      public ApplicationLauncher.AppScenes toggleModScenes;
    }

    private void OpenMainWindow () {
      if (mainWindow != null) {
        Destroy (mainWindow);
        mainWindow = null;
      }
      if (appButton?.toggleButton?.CurrentState == KSP.UI.UIRadioButton.State.False)
        return;
      UISkinDef skin = UISkinManager.defaultSkin;

      // main window
      mainWindow = new GameObject ("AdjustableModPanelWindow");
      mainWindow.AddComponent<CanvasRenderer> ();
      var transform = mainWindow.AddComponent<RectTransform> ();
      var image = mainWindow.AddComponent<UnityEngine.UI.Image> ();
      image.sprite = skin.window.normal.background;
      image.type = UnityEngine.UI.Image.Type.Sliced;
      // put in the center of the screen
      transform.anchorMin = new Vector2 (0.5f, 0.5f);
      transform.anchorMax = new Vector2 (0.5f, 0.5f);
      transform.pivot = new Vector2 (0.5f, 0.5f);
      var vert = mainWindow.AddComponent<UnityEngine.UI.VerticalLayoutGroup> ();
      vert.spacing = 3;
      vert.padding = new RectOffset (5, 5, 5, 5);
      var horzfitter = mainWindow.AddComponent<UnityEngine.UI.ContentSizeFitter> ();
      horzfitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
      horzfitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

      // header
      var line = new GameObject();
      line.AddComponent<RectTransform> ();
      var horlayout = line.AddComponent<UnityEngine.UI.HorizontalLayoutGroup> ();
      horlayout.spacing = 5;
      // header spacing
      var elem = new GameObject();
      elem.AddComponent<RectTransform> ();
      var layout = elem.AddComponent<UnityEngine.UI.LayoutElement> ();
      layout.preferredHeight = 30;
      layout.preferredWidth = 255 + 55;
      elem.transform.SetParent (line.transform);
      // header elements
      foreach (var scene in appScenesNames) {
        elem = new GameObject ();
        elem.AddComponent<CanvasRenderer> ();
        elem.AddComponent<RectTransform> ();
        layout = elem.AddComponent<UnityEngine.UI.LayoutElement> ();
        var text = elem.AddComponent<TMPro.TextMeshProUGUI> ();
        text.text = scene.Value;
        text.font = UISkinManager.TMPFont;
        text.fontSize = 14;
        text.color = Color.white;
        text.fontStyle = TMPro.FontStyles.Bold;
        text.alignment = TMPro.TextAlignmentOptions.Center;
        layout.preferredHeight = 30;
        layout.preferredWidth = 70;
        elem.transform.SetParent (line.transform);
      }
      line.transform.SetParent (mainWindow.transform);

      int num = 0;
      foreach (var mod in descriptors) {
        if (!mod.unmanageable && mod.defaultScenes != ApplicationLauncher.AppScenes.NEVER && mod.modIcon != null)
          num++;
      }

      Transform contentTransform = null;
      if (num < 11) {
        contentTransform = mainWindow.transform;
      } else {
        // scroller
        var scrollobj = new GameObject ();
        transform = scrollobj.AddComponent<RectTransform> ();
        layout = scrollobj.AddComponent<UnityEngine.UI.LayoutElement> ();
        layout.preferredHeight = 500;
        layout.preferredWidth = 630;
        scrollobj.transform.SetParent (mainWindow.transform);
        var scroll = scrollobj.AddComponent<UnityEngine.UI.ScrollRect> ();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.scrollSensitivity = 10f;
        var viewport = new GameObject ();
        viewport.AddComponent<CanvasRenderer> ();
        transform = viewport.AddComponent<RectTransform> ();
        viewport.AddComponent<UnityEngine.UI.Image> ();
        viewport.AddComponent<UnityEngine.UI.Mask> ().showMaskGraphic = false;
        viewport.transform.SetParent (scrollobj.transform);
        transform.anchorMin = Vector2.zero;
        transform.anchorMax = Vector2.one;
        transform.pivot = new Vector2 (0.5f, 0.5f);
        transform.sizeDelta = Vector2.zero;
        scroll.viewport = transform;
        var content = new GameObject();
        transform = content.AddComponent<RectTransform> ();
        content.AddComponent<UnityEngine.UI.VerticalLayoutGroup> ();
        var fitter = content.AddComponent<UnityEngine.UI.ContentSizeFitter> ();
        fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        content.transform.SetParent (viewport.transform);
        scroll.content = transform;
        transform.anchorMin = new Vector2 (0f, 1f);
        transform.anchorMax = new Vector2 (0f, 1f);
        transform.pivot = new Vector2 (0f, 1f);
        transform.sizeDelta = Vector2.zero;
        // scrollbar
        var scrollbar = new GameObject();
        scrollbar.AddComponent<CanvasRenderer> ();
        transform = scrollbar.AddComponent<RectTransform> ();
        image = scrollbar.AddComponent<UnityEngine.UI.Image> ();
        image.sprite = skin.verticalScrollbar.normal.background;
        image.type = UnityEngine.UI.Image.Type.Sliced;
        var scrollbarcomp = scrollbar.AddComponent<UnityEngine.UI.Scrollbar> ();
        scrollbarcomp.direction = UnityEngine.UI.Scrollbar.Direction.BottomToTop;
        scrollbar.transform.SetParent (scrollobj.transform);
        transform.anchorMin = new Vector2 (1f, 0f);
        transform.anchorMax = Vector2.one;
        transform.pivot = Vector2.one;
        transform.sizeDelta = new Vector2 (20, 0);
        // scrollbar handle
        var handle = new GameObject();
        handle.AddComponent<CanvasRenderer> ();
        transform = handle.AddComponent<RectTransform> ();
        image = handle.AddComponent<UnityEngine.UI.Image> ();
        image.sprite = skin.verticalScrollbarThumb.normal.background;
        image.type = UnityEngine.UI.Image.Type.Sliced;
        scrollbarcomp.targetGraphic = image;
        scrollbarcomp.transition = UnityEngine.UI.Selectable.Transition.SpriteSwap;
        var spritestate = scrollbarcomp.spriteState;
        spritestate.highlightedSprite = skin.verticalScrollbarThumb.highlight.background;
        spritestate.pressedSprite = skin.verticalScrollbarThumb.active.background;
        spritestate.disabledSprite = skin.verticalScrollbarThumb.disabled.background;
        scrollbarcomp.spriteState = spritestate;
        handle.transform.SetParent (scrollbar.transform);
        scrollbarcomp.handleRect = transform;
        scroll.verticalScrollbar = scrollbarcomp;
        contentTransform = content.transform;
        // reset config to fix some weirdness
        scrollbar.SetActive (false);
        scrollbar.SetActive (true);
        scrollobj.SetActive (false);
        scrollobj.SetActive (true);
        transform.offsetMin = Vector2.zero;
        transform.offsetMax = Vector2.zero;
      }

      foreach (var mod in descriptors) {
        if (mod.unmanageable || mod.modIcon == null || mod.defaultScenes == ApplicationLauncher.AppScenes.NEVER)
          continue;
        // mod lines
        line = new GameObject ();
        line.AddComponent<RectTransform> ();
        horlayout = line.AddComponent<UnityEngine.UI.HorizontalLayoutGroup> ();
        horlayout.spacing = 5;
        // pinned toggle element
        elem = new GameObject ();
        elem.AddComponent<RectTransform> ();
        layout = elem.AddComponent<UnityEngine.UI.LayoutElement> ();
        layout.preferredHeight = 50;
        layout.preferredWidth = 50;
        if (!GetType().Module.Name.StartsWith(mod.module)) {
          // toggle
          var toggle = new GameObject ();
          toggle.AddComponent<CanvasRenderer> ();
          transform = toggle.AddComponent<RectTransform> ();
          transform.anchorMin = new Vector2 (0.5f, 0.5f);
          transform.anchorMax = new Vector2 (0.5f, 0.5f);
          transform.pivot = new Vector2 (0.5f, 0.5f);
          transform.sizeDelta = new Vector2 (50, 50);
          image = toggle.AddComponent<UnityEngine.UI.Image> ();
          image.sprite = normalPin;
          image.type = UnityEngine.UI.Image.Type.Sliced;
          var togglecomp = toggle.AddComponent<UnityEngine.UI.Toggle> ();
          togglecomp.targetGraphic = image;
          togglecomp.image.sprite = normalPin;
          togglecomp.image.type = UnityEngine.UI.Image.Type.Sliced;
          togglecomp.transition = UnityEngine.UI.Selectable.Transition.SpriteSwap;
          var spritestatet = togglecomp.spriteState;
          spritestatet.highlightedSprite = highlightedPin;
          spritestatet.pressedSprite = activePin;
          spritestatet.disabledSprite = normalPin;
          togglecomp.spriteState = spritestatet;
          // checkbox for toggle
          var check = new GameObject ();
          check.AddComponent<CanvasRenderer> ();
          transform = check.AddComponent<RectTransform> ();
          check.transform.SetParent (toggle.transform);
          transform.anchorMin = new Vector2 (0f, 0f);
          transform.anchorMax = new Vector2 (1f, 1f);
          transform.pivot = new Vector2 (0.5f, 0.5f);
          transform.sizeDelta = Vector2.zero;
          image = check.AddComponent<UnityEngine.UI.Image> ();
          togglecomp.graphic = image;
          image.sprite = activePin;
          image.type = UnityEngine.UI.Image.Type.Sliced;
          // toggle setup
          togglecomp.interactable = true;
          togglecomp.isOn = pinnedMods[mod];
          togglecomp.onValueChanged.AddListener ((value) => {
            pinnedMods[mod] = value;
            Debug.Log ("[Adjustable Mod Panel] New pinned value for " + mod.module + ": " + value);
            forceUpdateCount += 1;
          });
          var navigation = togglecomp.navigation;
          navigation.mode = UnityEngine.UI.Navigation.Mode.None;
          togglecomp.navigation = navigation;
          toggle.transform.SetParent (elem.transform);
        }
        elem.transform.SetParent (line.transform);
        // mod icon
        elem = new GameObject ();
        elem.AddComponent<CanvasRenderer> ();
        elem.AddComponent<RectTransform> ();
        var rimage = elem.AddComponent<UnityEngine.UI.RawImage> ();
        rimage.texture = mod.modIcon;
        layout = elem.AddComponent<UnityEngine.UI.LayoutElement> ();
        layout.preferredHeight = 50;
        layout.preferredWidth = 50;
        elem.transform.SetParent (line.transform);
        // mod name
        elem = new GameObject ();
        elem.AddComponent<CanvasRenderer> ();
        elem.AddComponent<RectTransform> ();
        layout = elem.AddComponent<UnityEngine.UI.LayoutElement> ();
        var text = elem.AddComponent<TMPro.TextMeshProUGUI> ();
        text.text = mod.module;
        text.font = UISkinManager.TMPFont;
        text.fontSize = 14;
        text.color = Color.white;
        text.fontStyle = TMPro.FontStyles.Bold;
        text.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
        layout.preferredHeight = 50;
        layout.preferredWidth = 200;
        elem.transform.SetParent (line.transform);
        foreach (var scene in appScenesNames) {
          // mod line elements
          elem = new GameObject ();
          elem.AddComponent<RectTransform> ();
          layout = elem.AddComponent<UnityEngine.UI.LayoutElement> ();
          layout.preferredHeight = 50;
          layout.preferredWidth = 70;
          // toggle
          var toggle = new GameObject ();
          toggle.AddComponent<CanvasRenderer> ();
          transform = toggle.AddComponent<RectTransform> ();
          transform.anchorMin = new Vector2 (0.5f, 0.5f);
          transform.anchorMax = new Vector2 (0.5f, 0.5f);
          transform.pivot = new Vector2 (0.5f, 0.5f);
          transform.sizeDelta = new Vector2 (50, 50);
          image = toggle.AddComponent<UnityEngine.UI.Image> ();
          image.sprite = skin.toggle.normal.background;
          image.type = UnityEngine.UI.Image.Type.Sliced;
          var togglecomp = toggle.AddComponent<UnityEngine.UI.Toggle> ();
          togglecomp.targetGraphic = image;
          togglecomp.image.sprite = skin.toggle.normal.background;
          togglecomp.image.type = UnityEngine.UI.Image.Type.Sliced;
          togglecomp.transition = UnityEngine.UI.Selectable.Transition.SpriteSwap;
          var spritestate = togglecomp.spriteState;
          spritestate.highlightedSprite = skin.toggle.highlight.background;
          spritestate.pressedSprite = skin.toggle.active.background;
          spritestate.disabledSprite = skin.toggle.disabled.background;
          togglecomp.spriteState = spritestate;
          // checkbox for toggle
          var check = new GameObject ();
          check.AddComponent<CanvasRenderer> ();
          transform = check.AddComponent<RectTransform> ();
          check.transform.SetParent (toggle.transform);
          transform.anchorMin = new Vector2 (0f, 0f);
          transform.anchorMax = new Vector2 (1f, 1f);
          transform.pivot = new Vector2 (0.5f, 0.5f);
          transform.sizeDelta = Vector2.zero;
          image = check.AddComponent<UnityEngine.UI.Image> ();
          togglecomp.graphic = image;
          image.sprite = skin.toggle.active.background;
          image.type = UnityEngine.UI.Image.Type.Sliced;
          // parameters
          var parameters = toggle.AddComponent<ToggleParameters> ();
          parameters.currentModKey = mod;
          parameters.toggleModScenes = scene.Key;
          // checkbox state
          bool allowed = (mod.defaultScenes & scene.Key) != ApplicationLauncher.AppScenes.NEVER;
          ApplicationLauncher.AppScenes curvalue = currentScenes[mod];
          if (pendingChanges.ContainsKey(mod)) {
            curvalue = pendingChanges[mod];
          }
          bool enabled = (curvalue & (scene.Key & (~mod.requiredScenes))) != ApplicationLauncher.AppScenes.NEVER;
          togglecomp.interactable = allowed;
          togglecomp.isOn = allowed & enabled;
          if (!allowed) {
            togglecomp.GetComponent<UnityEngine.UI.Image> ().color = new Color (0f, 0f, 0f, 0.25f);
          }
          // special case for "always on"
          if ((parameters.toggleModScenes & (~mod.requiredScenes)) == ApplicationLauncher.AppScenes.NEVER) {
            image.color = Color.gray;
            togglecomp.isOn = true;
            // just keep it always on
            togglecomp.onValueChanged.AddListener ((value) => {
              if (!value) {
                togglecomp.isOn = true;
              }
            });
          } else {
            // checkbox callback
            togglecomp.onValueChanged.AddListener ((value) => {
              ApplicationLauncher.AppScenes currentvalue = currentScenes[mod];
              if (pendingChanges.ContainsKey (mod)) {
                currentvalue = pendingChanges[mod];
              }
              if (value != ((currentvalue & (parameters.toggleModScenes & (~mod.requiredScenes))) != ApplicationLauncher.AppScenes.NEVER)) {
                if (value) {
                  pendingChanges[mod] = currentvalue | (parameters.toggleModScenes & mod.defaultScenes);
                } else {
                  pendingChanges[mod] = (currentvalue & (~parameters.toggleModScenes)) | mod.requiredScenes;
                }
                Debug.Log ("[Adjustable Mod Panel] New value for " + parameters.currentModKey.module + ": " + pendingChanges[mod]);
                forceUpdateCount += 1;
                UpdateToggles ();
              }
            });
          }
          var navigation = togglecomp.navigation;
          navigation.mode = UnityEngine.UI.Navigation.Mode.None;
          togglecomp.navigation = navigation;
          toggle.transform.SetParent (elem.transform);
          elem.transform.SetParent (line.transform);
        }
        line.transform.SetParent (contentTransform);
      }
      // input locks
      mainWindow.transform.SetParent (MainCanvasUtil.MainCanvas.transform);
      var trigger = mainWindow.AddComponent<UnityEngine.EventSystems.EventTrigger> ();
      var onMouseEnter = new UnityEngine.EventSystems.EventTrigger.Entry() {
        eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter
      };
      onMouseEnter.callback.AddListener ((data) => InputLockManager.SetControlLock (ControlTypes.ALLBUTCAMERAS, "adjustablemodpanellock"));
      trigger.triggers.Add (onMouseEnter);
      var onMouseExit = new UnityEngine.EventSystems.EventTrigger.Entry () {
        eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit
      };
      onMouseExit.callback.AddListener ((data) => InputLockManager.RemoveControlLock ("adjustablemodpanellock"));
      trigger.triggers.Add (onMouseExit);
      mainWindow.SetActive (true);
    }

    private void CloseMainWindow () {
      if (mainWindow != null) {
        Destroy (mainWindow);
        mainWindow = null;
      }
      InputLockManager.RemoveControlLock ("adjustablemodpanellock");
    }

    private void UpdateToggles () {
      foreach (var toggle in mainWindow.GetComponentsInChildren<UnityEngine.UI.Toggle> (true)) {
        var pars = toggle.GetComponent<ToggleParameters> ();
        if (pars == null)
          continue;
        if ((pars.toggleModScenes & (~pars.currentModKey.requiredScenes)) == ApplicationLauncher.AppScenes.NEVER) {
          toggle.isOn = true;
          continue;
        }
        ApplicationLauncher.AppScenes currentvalue = currentScenes[pars.currentModKey];
        if (pendingChanges.ContainsKey (pars.currentModKey)) {
          currentvalue = pendingChanges[pars.currentModKey];
        }
        if (toggle.isOn != ((currentvalue & (pars.toggleModScenes & (~pars.currentModKey.requiredScenes))) != ApplicationLauncher.AppScenes.NEVER)) {
          toggle.isOn = ((currentvalue & (pars.toggleModScenes & (~pars.currentModKey.requiredScenes))) != ApplicationLauncher.AppScenes.NEVER);
        }
      }
    }

    #endregion

    public void Awake () {
      DontDestroyOnLoad (this);
      updateCallback1 = (scene) => this.forceUpdateCount += 2;
      updateCallback2 = () => this.forceUpdateCount += 3;

      GameEvents.onGUIApplicationLauncherReady.Add (OnGUIApplicationLauncherReady);
      GameEvents.onGUIApplicationLauncherUnreadifying.Add (OnGUIApplicationLauncherUnreadifying);
      GameEvents.onLevelWasLoadedGUIReady.Add (updateCallback1);
      GameEvents.onHideUI.Add (CloseMainWindow);
      GameEvents.onShowUI.Add (OpenMainWindow);

      MapView.OnEnterMapView = (Callback)Delegate.Combine (MapView.OnEnterMapView, updateCallback2);
      MapView.OnExitMapView = (Callback)Delegate.Combine (MapView.OnExitMapView, updateCallback2);

      LoadConfig ();
      Instance = this;

      appButtonTexture = new Texture2D (64, 64);
      appButtonTexture.LoadImage (ModPanelIcons.PanelIcon);

      var texture = new Texture2D (50, 50);
      texture.LoadImage (ModPanelIcons.NormalPin);
      normalPin = Sprite.Create (texture, new Rect (0, 0, texture.width, texture.height), Vector2.one / 2);
      texture = new Texture2D (50, 50);
      texture.LoadImage (ModPanelIcons.HighlightedPin);
      highlightedPin = Sprite.Create (texture, new Rect (0, 0, texture.width, texture.height), Vector2.one / 2);
      texture = new Texture2D (50, 50);
      texture.LoadImage (ModPanelIcons.ActivePin);
      activePin = Sprite.Create (texture, new Rect (0, 0, texture.width, texture.height), Vector2.one / 2);
    }

    public void OnDestroy () {
      GameEvents.onGUIApplicationLauncherReady.Remove (OnGUIApplicationLauncherReady);
      GameEvents.onGUIApplicationLauncherUnreadifying.Remove (OnGUIApplicationLauncherUnreadifying);
      GameEvents.onLevelWasLoadedGUIReady.Remove (updateCallback1);
      GameEvents.onHideUI.Remove (CloseMainWindow);
      GameEvents.onShowUI.Remove (OpenMainWindow);

      MapView.OnEnterMapView = (Callback)Delegate.Remove (MapView.OnEnterMapView, updateCallback2);
      MapView.OnExitMapView = (Callback)Delegate.Remove (MapView.OnExitMapView, updateCallback2);
      SaveConfig ();
    }

    public void OnGUIApplicationLauncherReady () {
      ApplicationLauncher.Instance.prefab_verticalTopDown.GetGameObject ().AddOrGetComponent<ModPanelComponent> ();
      ApplicationLauncher.Instance.prefab_horizontalRightLeft.GetGameObject ().AddOrGetComponent<ModPanelComponent> ();
      if (ApplicationLauncher.Ready && appButton == null) {
        appButton = ApplicationLauncher.Instance.AddModApplication (OpenMainWindow, CloseMainWindow, null, null,
          AppButtonEnable, AppButtonDisable, ApplicationLauncher.AppScenes.ALWAYS & (~ApplicationLauncher.AppScenes.MAINMENU), appButtonTexture);
        // this mod should be always on in KSC
        appButton.container.Data = ApplicationLauncher.AppScenes.SPACECENTER;
      }
      forceUpdateCount += 3;
    }

    private void OnGUIApplicationLauncherUnreadifying (GameScenes scene) {
      appButton?.SetFalse ();
    }

    public void AppButtonDisable () {
      appButton?.SetFalse ();
    }

    public void AppButtonEnable () {
      // not needed, really
    }

    internal ApplicationLauncher.AppScenes GetModScenes (string module, string method, ApplicationLauncher.AppScenes currentScenes, uint hash) {
      foreach (var mod in descriptors) {
        if (mod.module == module && mod.method == method) {
          if (!mod.textureHashNeeded || (mod.textureHash == hash)) {
            if (mod.unmanageable) {
              return currentScenes;
            }
            // lets check that the scenes didn't change while we were frolicking about
            if (currentScenes != this.currentScenes[mod] && currentScenes != mod.defaultScenes) {
              Debug.Log ("[Adjustable Mod Panel] WARNING: mod " + module + "+" + method + " changed visibility. This is unexpected, ignoring it.");
              mod.unmanageable = true;
              return currentScenes;
            }
            if (pendingChanges.ContainsKey (mod)) {
              this.currentScenes[mod] = pendingChanges[mod];
              pendingChanges.Remove (mod);
            }
            return this.currentScenes[mod];
          }
        }
      }
      Debug.Log ("[Adjustable Mod Panel] WARNING: mod " + module + "+" + method + " was not found.");
      return currentScenes;
    }

    internal bool GetModPinned (string module, string method, ApplicationLauncher.AppScenes currentScenes, uint hash) {
      foreach (var mod in descriptors) {
        if (mod.module == module && mod.method == method) {
          if (!mod.textureHashNeeded || (mod.textureHash == hash)) {
            if (mod.unmanageable) {
              return false;
            }
            return pinnedMods[mod];
          }
        }
      }
      return false;
    }

          internal bool IsAppButton (ApplicationLauncherButton appButton) {
      if (appButton == this.appButton)
        return true;
      return false;
    }

    private int forceUpdateCount = 0;
    internal bool ForceUpdate {
      get {
        if (forceUpdateCount > 0) {
          forceUpdateCount--;
          return true;
        }
        return false;
      }
    }

    internal void StartRecordingMods () {
      tempDescriptors = new List<ModDescriptor> ();
    }

    internal void RecordMod (string module, string method, ApplicationLauncher.AppScenes scenes, uint hash, ApplicationLauncher.AppScenes alwaysOn,Texture2D texture) {
      if (tempDescriptors == null) {
        Debug.Log ("[Adjustable Mod Panel] WARNING: mod is recorded in unexpected place.");
        return;
      }
      tempDescriptors.Add (new ModDescriptor () {
        module = module,
        method = method,
        textureHash = hash,
        textureHashNeeded = false,
        unmanageable = false,
        defaultScenes = scenes,
        requiredScenes = alwaysOn,
        modIcon = texture
      });
    }

    internal void EndRecordingMods () {
      if (tempDescriptors == null) {
        Debug.Log ("[Adjustable Mod Panel] WARNING: mod is recorded in unexpected place.");
        return;
      }
      if (tempDescriptors.Count == 0) {
        tempDescriptors = null;
        return;
      }

      var easy = new List<ModDescriptor> ();
      var hard = new List<ModDescriptor> ();
      var unmaintainable = new List<ModDescriptor> ();
      for (int i = 0; i < tempDescriptors.Count; i++) {
        var a = tempDescriptors[i];
        for (int j = i + 1; j < tempDescriptors.Count; j++) {
          var b = tempDescriptors[j];
          if (a.module == b.module && a.method == b.method) {
            a.textureHashNeeded = b.textureHashNeeded = true;
            if (a.textureHash == b.textureHash) {
              Debug.Log ("[Adjustable Mod Panel] WARNING: " + a.module + " defines several app buttons that are exactly the same. We will ignore them from now on.");
              a.unmanageable = true;
            }
          }
        }
        if (a.textureHashNeeded == false) {
          easy.Add (a);
        } else if (a.unmanageable == false) {
          hard.Add (a);
        } else {
          unmaintainable.Add (a);
        }
      }
      foreach (var mod in easy) {
        bool add = true;
        foreach (var modref in descriptors) {
          if (mod.module == modref.module && mod.method == modref.method) {
            if (modref.textureHashNeeded == false) {
              // we assume this is the mod, and end with it
              add = false;
              FillModIfNeeded (modref, mod);
              break;
            }
            if (modref.textureHashNeeded == true) {
              // there used to be several mods with this signature
              // throw it into the "hard" pile
              mod.textureHashNeeded = true;
              hard.Add (mod);
              add = false;
              break;
            }
          }
        }
        if (add) {
          //new mod
          Debug.Log ("[Adjustable Mod Panel] Record a new mod: module " + mod.module + ", method " + mod.method);
          descriptors.Add (mod);
          currentScenes[mod] = mod.defaultScenes;
          pinnedMods[mod] = false;
        }
      }
      foreach (var mod in hard) {
        bool add = true;
        foreach (var modref in descriptors) {
          if (mod.module == modref.module && mod.method == modref.method) {
            // whatever was before, now we consider the texture as a part of a descriptor
            modref.textureHashNeeded = true;
            if (mod.textureHash == modref.textureHash) {
              // mod found
              FillModIfNeeded (modref, mod);
              add = false;
              break;
            }
          }
        }
        if (add) {
          //new mod
          Debug.Log ("[Adjustable Mod Panel] Record a new mod: module " + mod.module + ", method " + mod.method + ", hash " + mod.textureHash);
          Debug.Log ("[Adjustable Mod Panel] WARNING: Since there are several mods with such signature, the icon would be used as a descriptor");
          descriptors.Add (mod);
          currentScenes[mod] = mod.defaultScenes;
          pinnedMods[mod] = false;
        }
      }
      foreach (var mod in unmaintainable) {
        bool add = true;
        foreach (var modref in descriptors) {
          if (mod.module == modref.module && mod.method == modref.method && mod.textureHash == modref.textureHash) {
            modref.unmanageable = true;
            add = false;
          }
        }
        if (add) {
          descriptors.Add (mod);
        }
      }
      tempDescriptors = null;
    } 

    void FillModIfNeeded (ModDescriptor to, ModDescriptor from) {
      if (to.modIcon != null)
        return;
      Debug.Log ("[Adjustable Mod Panel] Encountered a known mod: module " + to.module + ", method " + to.method + 
        (to.textureHashNeeded? (", hash " + to.textureHash.ToString()) : ""));
      to.modIcon = from.modIcon;
      to.requiredScenes = from.requiredScenes;
      to.defaultScenes = from.defaultScenes;
    }

    #region Config

    internal void SaveConfig () {
      Debug.Log ("[Adjustable Mod Panel] Saving settings.");
      KSP.IO.PluginConfiguration config = KSP.IO.PluginConfiguration.CreateForType<AdjustableModPanel> (null);
      int i = 1;
      foreach (var mod in descriptors) {
        if (mod.unmanageable)
          continue;
        // artifact from an old config
        if (mod.textureHashNeeded && mod.textureHash == 0u)
          continue;
        config["module" + i.ToString ()] = mod.module;
        config["method" + i.ToString ()] = mod.method;
        config["hashNeeded" + i.ToString ()] = mod.textureHashNeeded;
        if (mod.textureHashNeeded)
          config["hash" + i.ToString ()] = (long)mod.textureHash;
        config["scenes" + i.ToString ()] = (int)currentScenes[mod];
        config["pinned" + i.ToString ()] = pinnedMods[mod];
        i++;
      }
      config["count"] = i-1;
      config.save ();
    }

    private void LoadConfig () {
      Debug.Log ("[Adjustable Mod Panel] Loading settings.");
      KSP.IO.PluginConfiguration config = KSP.IO.PluginConfiguration.CreateForType<AdjustableModPanel> (null);
      config.load ();
      try {
        int count = config.GetValue<int> ("count");
        for (int i = 1; i <= count; i++) {
          string key = config.GetValue<string> ("mod" + i.ToString (), "");
          ModDescriptor desc = null;
          if (key != "") {
            var vals = key.Split('+');
            desc = new ModDescriptor () {
              module = vals[0],
              method = vals[1],
              textureHash = 0,
              textureHashNeeded = false,
              unmanageable = false,
              modIcon = null,
              defaultScenes = ApplicationLauncher.AppScenes.NEVER,
              requiredScenes = ApplicationLauncher.AppScenes.NEVER
            };
          } else {
            desc = new ModDescriptor () {
              module = config.GetValue<string> ("module" + i.ToString ()),
              method = config.GetValue<string> ("method" + i.ToString ()),
              textureHashNeeded = config.GetValue<bool> ("hashNeeded" + i.ToString ()),
              textureHash = (uint)config.GetValue<long> ("hash" + i.ToString (), 0u),
              unmanageable = false,
              modIcon = null,
              defaultScenes = ApplicationLauncher.AppScenes.NEVER,
              requiredScenes = ApplicationLauncher.AppScenes.NEVER
            };
          }
          ApplicationLauncher.AppScenes value = (ApplicationLauncher.AppScenes)config.GetValue<int> ("scenes" + i.ToString ());
          descriptors.Add (desc);
          currentScenes[desc] = value;
          pinnedMods[desc] = config.GetValue<bool> ("pinned" + i.ToString (), false);
        }
      } catch (System.Exception e) {
        Debug.Log ("[Adjustable Mod Panel] There was an error reading config: " + e);
      }
    }

    #endregion

  }
}
