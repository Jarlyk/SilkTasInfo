using Assembly_CSharp.TasInfo.mm.Source;
using UnityEngine;
using System.Collections;

// ReSharper disable All
class patch_CameraController : CameraController {
    private void Awake() {
        Application.onBeforeRender += OnApplicationBeforeRender;
        StartCoroutine(OnWaitForEndOfFrame());
    }
    
    private void OnApplicationBeforeRender() {
        TasInfo.OnPreRender();
    }

    private IEnumerator OnWaitForEndOfFrame() {
        while (true) {
            yield return new WaitForEndOfFrame();
            TasInfo.OnPostRender();
        }
    }
}