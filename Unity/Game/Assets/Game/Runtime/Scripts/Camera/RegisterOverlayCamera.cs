using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Camera))]
public sealed class RegisterOverlayCamera : MonoBehaviour
{
    [SerializeField] private string baseCameraTag = "MainCamera";

    private Camera _overlayCamera;
    private Camera _baseCamera;

    private void Start()
    {
        _overlayCamera = GetComponent<Camera>();

        var baseObject = GameObject.FindWithTag(baseCameraTag);
        if (baseObject == null ||
            !baseObject.TryGetComponent(out _baseCamera))
        {
            Debug.LogWarning("Base Camera not found.");
            return;
        }

        var baseData = _baseCamera.GetUniversalAdditionalCameraData();
        var overlayData = _overlayCamera.GetUniversalAdditionalCameraData();

        overlayData.renderType = CameraRenderType.Overlay;

        if (!baseData.cameraStack.Contains(_overlayCamera))
            baseData.cameraStack.Add(_overlayCamera);
    }

    private void OnDisable()
    {
        if (_baseCamera == null || _overlayCamera == null)
            return;

        var data = _baseCamera.GetUniversalAdditionalCameraData();
        data.cameraStack.Remove(_overlayCamera);
    }
}