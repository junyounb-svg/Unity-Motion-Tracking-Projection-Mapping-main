using UnityEngine;

/// <summary>
/// Third-person camera that follows a motion-tracked object (e.g. Bag.2_White).
/// The camera moves side-to-side, forward/back, and up/down to follow the target,
/// maintaining a consistent proximity distance. The camera does NOT rotate.
/// Attach this script to your Main Camera.
/// </summary>
public class CameraMovement : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Name of the motion-tracked object in the hierarchy (e.g. Bag.2_White).")]
    [SerializeField] private string targetObjectName = "Bag.2_White";
    [Tooltip("Assign in Inspector to avoid relying on Find, or leave empty to find by name.")]
    [SerializeField] private Transform target;

    [Header("Camera Follow Settings")]
    [Tooltip("Offset from target. In this project, higher Z = behind the object. (0, 1.3, 4) = 1.3 up, 4 units behind.")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.3f, 4f);
    [Tooltip("Maximum distance the camera can be from the target (proximity limit).")]
    [SerializeField] private float maxDistanceFromTarget = 3f;
    [Tooltip("How quickly the camera follows the target (higher = faster response).")]
    [SerializeField] private float followSpeed = 8f;

    [Header("Default on Play")]
    [Tooltip("Camera field of view when the scene starts playing.")]
    [SerializeField] private float defaultFieldOfView = 45.92966f;

    void Start()
    {
        // Find target first so we can start the camera in the right place
        if (target == null && !string.IsNullOrEmpty(targetObjectName))
        {
            GameObject go = GameObject.Find(targetObjectName);
            if (go != null)
            {
                target = go.transform;
                Debug.Log("CameraMovement: Found target '" + targetObjectName + "'.");
            }
            else
            {
                Debug.LogWarning("CameraMovement: Could not find '" + targetObjectName + "'. Assign the target in the Inspector or check the object name in the hierarchy.");
            }
        }

        // Start camera in follow position relative to target (so object is in view from frame 1)
        if (target != null)
        {
            Vector3 desiredPosition = target.position + offset;
            Vector3 directionToCamera = desiredPosition - target.position;
            float distanceToCamera = directionToCamera.magnitude;
            if (distanceToCamera > maxDistanceFromTarget)
                desiredPosition = target.position + directionToCamera.normalized * maxDistanceFromTarget;
            transform.position = desiredPosition;
        }

        Camera cam = GetComponent<Camera>();
        if (cam != null)
            cam.fieldOfView = defaultFieldOfView;
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        // Desired position: target + offset (camera follows in all directions, no rotation)
        Vector3 desiredPosition = target.position + offset;

        // Keep camera within max proximity of target
        Vector3 directionToCamera = desiredPosition - target.position;
        float distanceToCamera = directionToCamera.magnitude;
        if (distanceToCamera > maxDistanceFromTarget)
        {
            desiredPosition = target.position + directionToCamera.normalized * maxDistanceFromTarget;
        }

        // Move only position; rotation is never changed
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
    }

    void OnDrawGizmosSelected()
    {
        Transform t = target != null ? target : (Application.isPlaying ? target : null);
        if (t != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(t.position, maxDistanceFromTarget);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(t.position, transform.position);
        }
    }
}
