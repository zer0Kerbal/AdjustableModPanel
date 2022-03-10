/*
Copyright (c) 2017, radistmorse
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

using System.Linq;
using UnityEngine;

namespace AdjustableModPanel {
  class ModPanelComponent : MonoBehaviour {

    private KSP.UI.UIList modlist = null;
    private int mods = -1;

    public void Awake () {
      if (gameObject == null)
        return;
      var layout = GetComponent<KSP.UI.Screens.SimpleLayout> ();
      modlist = layout?.GetModList ();

      if (modlist == null) {
        // no modlist, nothing to do
        Debug.Log ("[Adjustable Mod Panel] ERROR: No mod panel found, exiting now.");
        Destroy (this);
      }
    }

    // this black magic is needed to get the texture from gpu to cpu
    private Texture2D DuplicateTexture (Texture2D source) {
      RenderTexture renderTex = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);

      Graphics.Blit (source, renderTex);
      RenderTexture previous = RenderTexture.active;
      RenderTexture.active = renderTex;
      Texture2D readableText = new Texture2D(source.width, source.height);
      readableText.ReadPixels (new Rect (0, 0, renderTex.width, renderTex.height), 0, 0, false);
      readableText.Apply ();
      RenderTexture.active = previous;
      RenderTexture.ReleaseTemporary (renderTex);
      return readableText;
    }

    public void Update () {
      // not the best way to avoid useless updates, but there are no relevant events
      if (modlist.Count == mods && !AdjustableModPanel.Instance.ForceUpdate)
        return;
      mods = modlist.Count;

      KSP.UI.UIListData<KSP.UI.UIListItem> myButton = null;

      AdjustableModPanel.Instance.StartRecordingMods ();

      System.Collections.Generic.Dictionary<KSP.UI.Screens.ApplicationLauncherButton, uint> cashes = 
        new System.Collections.Generic.Dictionary<KSP.UI.Screens.ApplicationLauncherButton, uint> ();

      foreach (var mod in modlist) {
        var button = mod.listItem.GetComponent<KSP.UI.Screens.ApplicationLauncherButton> ();
        if (AdjustableModPanel.Instance.IsAppButton (button)) {
          myButton = mod;
        }
        var texture = (Texture2D) button.sprite.texture;

        // button without a texture - very unnatural. Better ignore.
        if (texture == null)
          continue;

        // try any non-null callback in order
        var func = new Callback[] { button.onTrue, button.onHover, button.onLeftClick, button.onEnable, button.onFalse, button.onHoverOut, button.onDisable }
          .Select(cb => cb.GetInvocationList().Skip(1).FirstOrDefault()).Where(m => m != null).FirstOrDefault();

        // button with no callbacks. Ignore.
        if (func == null)
          continue;

        var method = func.Method.Name;
        var module = func.Method.Module.Name;
        if (module.EndsWith (".dll"))
          module = module.Substring (0, module.Length - 4);

        if (AdjustableModPanel.Instance.IsButtonToolbarController (button, out string name, out string id)) {
          // button created by ToolbarControl needs special treatment
          module = name;
          method = id;
        }

        uint textureHash = 0;
        unchecked {
          foreach (var byt in DuplicateTexture (texture).GetRawTextureData ()) {
            textureHash = textureHash * 8u + byt;
          }
        }
        cashes[button] = textureHash;

        KSP.UI.Screens.ApplicationLauncher.AppScenes alwaysOn = KSP.UI.Screens.ApplicationLauncher.AppScenes.NEVER;

        if (button.container.Data is KSP.UI.Screens.ApplicationLauncher.AppScenes) {
          alwaysOn = (KSP.UI.Screens.ApplicationLauncher.AppScenes)button.container.Data;
          button.container.Data = null;
        }

        AdjustableModPanel.Instance.RecordMod (module, method, button.VisibleInScenes, textureHash, alwaysOn, texture);
      }

      AdjustableModPanel.Instance.EndRecordingMods ();

      foreach (var mod in modlist) {
        var button = mod.listItem.GetComponent<KSP.UI.Screens.ApplicationLauncherButton> ();
        if (AdjustableModPanel.Instance.IsAppButton (button)) {
          myButton = mod;
        }
        var texture = (Texture2D) button.sprite.texture;

        // button without a texture - very unnatural. Better ignore.
        if (texture == null)
          continue;

        // try any non-null callback in order
        var func = new Callback[] { button.onTrue, button.onHover, button.onLeftClick, button.onEnable, button.onFalse, button.onHoverOut, button.onDisable }
          .Select(cb => cb.GetInvocationList().Skip(1).FirstOrDefault()).Where(m => m != null).FirstOrDefault();

        // button with no callbacks. Ignore.
        if (func == null)
          continue;

        var module = func.Method.Module.Name;
        if (module.EndsWith (".dll"))
          module = module.Substring (0, module.Length - 4);

        var method = func.Method.Name;
        if (AdjustableModPanel.Instance.IsButtonToolbarController (button, out string name, out string id)) {
          // button created by ToolbarControl needs special treatment
          module = name;
          method = id;
        }

        button.VisibleInScenes = AdjustableModPanel.Instance.GetModScenes (module, method, button.VisibleInScenes, cashes[button]);
        // normally, this should be enough. But for some reason some mode buttons remain active,
        //even when they should not. So we change the button state explicitly.
        bool visible = KSP.UI.Screens.ApplicationLauncher.Instance.DetermineVisibility (button);
        button.gameObject.SetActive (visible);

        if (visible && AdjustableModPanel.Instance.GetModPinned (module, method, button.VisibleInScenes, cashes[button])) {
          // move to the top of the mod panel
          mod.listItem.transform.SetAsFirstSibling ();
        }
      }

      // our own button should always be the first
      myButton?.listItem.transform.SetAsFirstSibling ();
    }
  }
}
