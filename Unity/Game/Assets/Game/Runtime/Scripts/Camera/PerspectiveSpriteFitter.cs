
using UnityEngine;


[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public sealed class PerspectiveSpriteFitter : MonoBehaviour
{
    public enum FitMode
    {
        Contain, 
        Cover,
        FitWidth,
        FitHeight,
        FitInParent /// test 
    }

    [Header("Target")]
    [SerializeField] private SpriteRenderer targetSprite;
    [SerializeField] private FitMode fitMode = FitMode.FitInParent;

    [Header("Options")]
    [Range(0f, 0.45f)]
    [SerializeField] private float padding;

    [Min(0f)]
    [SerializeField] private float extraDistance;

    [Header("Fit In Parent Alignment")]
    [Tooltip("-1 = extra space below, +1 = above")]
    [Range(-1f, 1f)]
    [SerializeField] private float verticalAlignment;

    [Tooltip("-1 = extra space left, +1 = right")]
    [Range(-1f, 1f)]
    [SerializeField] private float horizontalAlignment;

    [Header("Update")]
    [SerializeField] private bool updateContinuously = true;

    private Camera _camera;
    private bool _needsUpdate = true;

    private void OnEnable()
    {
        _camera = GetComponent<Camera>();
        _needsUpdate = true;
        Fit();
    }

    private void OnValidate()
    {
        _needsUpdate = true;
    }

    private void LateUpdate()
    {
        if (!updateContinuously && !_needsUpdate)
            return;

        Fit();
        _needsUpdate = false;
    }

    [ContextMenu("Fit Sprite Now")]
    public void Fit()
    {
        if (targetSprite == null || targetSprite.sprite == null)
            return;

        if (_camera == null)
            _camera = GetComponent<Camera>();

        if (_camera.orthographic || _camera.aspect <= 0f)
            return;

        Bounds bounds = targetSprite.localBounds;
        Transform target = targetSprite.transform;

        Vector3 center = target.TransformPoint(bounds.center);
        Quaternion invRotation = Quaternion.Inverse(transform.rotation);

        float halfFov = _camera.fieldOfView * Mathf.Deg2Rad * 0.5f;
        float usableArea = 1f - padding * 2f;

        float slopeY = Mathf.Tan(halfFov) * usableArea;
        float slopeX = slopeY * _camera.aspect;

        float widthDistance = 0f;
        float heightDistance = 0f;
        float nearDistance = 0f;

        // Measure the sprite's four corners in camera space.
        for (int i = 0; i < 4; i++)
        {
            Vector3 p = GetCorner(i, bounds, target, center, invRotation);

            widthDistance = Mathf.Max(
                widthDistance,
                Mathf.Abs(p.x) / slopeX - p.z
            );

            heightDistance = Mathf.Max(
                heightDistance,
                Mathf.Abs(p.y) / slopeY - p.z
            );

            nearDistance = Mathf.Max(
                nearDistance,
                _camera.nearClipPlane + 0.01f - p.z
            );
        }

        float distance = fitMode switch
        {
            FitMode.Cover => Mathf.Min(widthDistance, heightDistance),
            FitMode.FitWidth => widthDistance,
            FitMode.FitHeight => heightDistance,
            _ => Mathf.Max(widthDistance, heightDistance)
        };

        distance = Mathf.Max(distance, nearDistance, 0.01f);
        distance += extraDistance;

        Vector2 offset = Vector2.zero;

        // FitInParent and Contain keep the whole sprite visible.
        // Compute available slack to distribute the extra space.
        if (fitMode == FitMode.FitInParent ||
            fitMode == FitMode.Contain)
        {
            float minX = float.NegativeInfinity;
            float maxX = float.PositiveInfinity;
            float minY = float.NegativeInfinity;
            float maxY = float.PositiveInfinity;

            for (int i = 0; i < 4; i++)
            {
                Vector3 p = GetCorner(
                    i, bounds, target, center, invRotation);

                float depth = distance + p.z;
                float halfWidth = depth * slopeX;
                float halfHeight = depth * slopeY;

                minX = Mathf.Max(minX, p.x - halfWidth);
                maxX = Mathf.Min(maxX, p.x + halfWidth);

                minY = Mathf.Max(minY, p.y - halfHeight);
                maxY = Mathf.Min(maxY, p.y + halfHeight);
            }

            if (minX <= maxX)
            {
                offset.x = Mathf.Lerp(
                    minX, maxX,
                    (horizontalAlignment + 1f) * 0.5f
                );
            }

            if (minY <= maxY)
            {
                offset.y = Mathf.Lerp(
                    minY, maxY,
                    (verticalAlignment + 1f) * 0.5f
                );
            }
        }

        // Keep camera orientation unchanged.
        transform.position =
            center
            - transform.forward * distance
            + transform.right * offset.x
            + transform.up * offset.y;
    }

    private static Vector3 GetCorner(
        int index,
        Bounds bounds,
        Transform target,
        Vector3 center,
        Quaternion invRotation)
    {
        Vector3 localCorner = new Vector3(
            (index & 1) == 0 ? bounds.min.x : bounds.max.x,
            (index & 2) == 0 ? bounds.min.y : bounds.max.y,
            bounds.center.z
        );

        Vector3 worldCorner = target.TransformPoint(localCorner);
        return invRotation * (worldCorner - center);
    }
}
