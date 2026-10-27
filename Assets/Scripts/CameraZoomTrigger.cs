using UnityEngine;

public class CameraZoomTrigger : MonoBehaviour
{
    [Header("Vista Cercana")]
    [Tooltip("Tamaño de cámara ortográfica (zoom)")]
    [SerializeField] private float closeZoom = 5.5f;

    [Tooltip("Ángulo X de la cámara (ej. 30 para más frontal, 50 es el original)")]
    [SerializeField] private float frontalAngleX = 30f;

    [Tooltip("Desplazamiento de la cámara mientras estás en esta zona")]
    [SerializeField] private Vector3 closeOffset = new Vector3(0, 9, -11);

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null && player.viewCamera != null)
        {
            player.viewCamera.GetComponent<CameraFollow>()?.SetContextView(closeZoom, frontalAngleX, closeOffset);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null && player.viewCamera != null)
        {
            player.viewCamera.GetComponent<CameraFollow>()?.ResetToDefaultView();
        }
    }
}