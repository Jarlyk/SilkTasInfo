using Assembly_CSharp.TasInfo.mm.Source;
using UnityEngine;

// ReSharper disable All
class patch_CameraController : CameraController {
    private void Awake() {
        Application.onBeforeRender += OnApplicationBeforeRender;
    }
    
    private void OnApplicationBeforeRender() {
        TasInfo.OnPreRender();
    }

    private void OnPostRender() {
        TasInfo.OnPostRender();
    }
}