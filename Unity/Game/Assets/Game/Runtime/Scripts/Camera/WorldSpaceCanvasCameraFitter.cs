
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public sealed class WorldSpaceCanvasCameraFitter : MonoBehaviour
{
    public enum FitMode
    {
        Contain,
        Cover,
        FitWidth,
        FitHeight
    }

    [SerializeField] private RectTransform canvasRoot;
    [SerializeField] private FitMode fitMode = FitMode.Contain;

    [Range(0f, 0.4f)]
    [SerializeField] private float padding = 0f;

    [SerializeField] private bool updateContinuously = true;

    private Camera _camera;

    private void OnEnable()
    {
        _camera = GetComponent<Camera>();
        Fit();
    }

    private void LateUpdate()
    {
        if (updateContinuously)
            Fit();
    }

    [ContextMenu("Fit Canvas Now")]
    public void Fit()
    {
        if (canvasRoot == null)
            return;

        if (_camera == null)
            _camera = GetComponent<Camera>();

        if (!_camera.orthographic || _camera.aspect <= 0f)
            return;

        // Canvas rectangle expressed in camera-oriented space.
        Vector3[] corners = new Vector3[4];
        canvasRoot.GetWorldCorners(corners);

        Quaternion inverseRotation =
            Quaternion.Inverse(transform.rotation);

        Vector3 center = canvasRoot.TransformPoint(
            canvasRoot.rect.center
        );

        float halfWidth = 0f;
        float halfHeight = 0f;

        foreach (Vector3 corner in corners)
        {
            Vector3 local =
                inverseRotation * (corner - center);

            halfWidth = Mathf.Max(
                halfWidth, Mathf.Abs(local.x));

            halfHeight = Mathf.Max(
                halfHeight, Mathf.Abs(local.y));
        }

        float heightSize = halfHeight;
        float widthSize = halfWidth / _camera.aspect;

        float size = fitMode switch
        {
            FitMode.Cover => Mathf.Min(
                heightSize, widthSize),

            FitMode.FitWidth => widthSize,

            FitMode.FitHeight => heightSize,

            _ => Mathf.Max(heightSize, widthSize)
        };

        _camera.orthographicSize =
            Mathf.Max(0.001f, size / (1f - padding));

        // Center the UI without modifying camera rotation.
        Vector3 delta = center - transform.position;

        transform.position +=
            Vector3.ProjectOnPlane(delta, transform.forward);
    }
}
