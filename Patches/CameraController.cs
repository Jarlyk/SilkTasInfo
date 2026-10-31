using Assembly_CSharp.TasInfo.mm.Source;
using UnityEngine;
using System.Collections;

// ReSharper disable All
class patch_CameraController : CameraController {
    private Coroutine postRender;

    private void Awake() {
        Application.onBeforeRender += OnApplicationBeforeRender;
        StartCoroutine(OnWaitForEndOfFrame());
    }

    private void OnDestroy() {
        Application.onBeforeRender -= OnApplicationBeforeRender;

        if (postRender != null) {
            StopCoroutine(postRender);
            postRender = null;
        }
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