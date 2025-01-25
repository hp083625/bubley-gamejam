using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshPro))]
public class FaceCamera : MonoBehaviour
{
    [Header("Camera Settings")]
    [Tooltip("The camera the TMP should face. If left empty, the main camera will be used.")]
    public Camera targetCamera;

    [Header("Rotation Settings")]
    [Tooltip("If checked, the TMP will fully match the camera's rotation.")]
    public bool matchCameraRotation = true;

    [Tooltip("If unchecked, the TMP will only rotate around the Y-axis to face the camera.")]
    public bool allowPitchAndRoll = true;

    [Header("Smooth Rotation")]
    [Tooltip("Enable smooth rotation for a more natural facing motion.")]
    public bool enableSmoothRotation = false;

    [Tooltip("Rotation speed when smooth rotation is enabled.")]
    public float rotationSpeed = 10f;

    private TextMeshPro tmp;

    void Start()
    {
        // Assign the main camera if no targetCamera is set
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                Debug.LogError("FaceCamera: No camera assigned and no Main Camera found.");
            }
        }

        // Get the TextMeshPro component
        tmp = GetComponent<TextMeshPro>();
        if (tmp == null)
        {
            Debug.LogError("FaceCamera script requires a TextMeshPro component on the same GameObject.");
        }
    }

    void LateUpdate()
    {
        if (targetCamera == null) return;

        if (enableSmoothRotation)
        {
            // Calculate the desired rotation
            Quaternion targetRotation;

            if (matchCameraRotation)
            {
                targetRotation = targetCamera.transform.rotation;
            }
            else
            {
                // Only rotate around Y-axis
                Vector3 direction = targetCamera.transform.position - transform.position;
                direction.y = 0; // Keep the text upright
                targetRotation = Quaternion.LookRotation(direction);
            }

            // Smoothly interpolate towards the target rotation
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        else
        {
            if (matchCameraRotation)
            {
                // Directly match the camera's rotation
                transform.rotation = targetCamera.transform.rotation;
            }
            else
            {
                // Make the TMP face the camera, only rotating around the Y-axis
                Vector3 direction = targetCamera.transform.position - transform.position;
                direction.y = 0; // Keep the text upright
                if (direction.sqrMagnitude > 0.001f)
                {
                    Quaternion rotation = Quaternion.LookRotation(direction);
                    transform.rotation = rotation;
                }
            }
        }
    }
}
