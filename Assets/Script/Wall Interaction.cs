using UnityEngine;

public class SlidingDoor : MonoBehaviour
{
    [Header("Player Reference")]
    [SerializeField] private string playerObjectName = "Bag.2_White";
    [SerializeField] private Transform playerTransform;
    
    [Header("Proximity Settings")]
    [SerializeField] private float proximityRadius = 3f;
    
    [Header("Slide Settings")]
    [SerializeField] private Vector3 slideOffset = Vector3.up * 2f; // How far the door moves when open
    [SerializeField] private float slideSpeed = 2f;
    
    private Vector3 closedPosition;
    private Vector3 openPosition;
    private bool isOpen = false;
    
    void Start()
    {
        // Store the starting position as the closed position
        closedPosition = transform.position;
        openPosition = closedPosition + slideOffset;
        
        // Find player if not assigned
        if (playerTransform == null)
        {
            GameObject playerObject = GameObject.Find(playerObjectName);
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
            }
            else
            {
                Debug.LogWarning("SlidingDoor: Could not find player object named '" + playerObjectName + "'. Please assign it manually.");
            }
        }
    }
    
    void Update()
    {
        if (playerTransform == null)
            return;
        
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        
        // Open when player is within proximity, close when outside
        if (distance <= proximityRadius)
        {
            isOpen = true;
        }
        else
        {
            isOpen = false;
        }
        
        // Smoothly lerp the door position
        Vector3 targetPosition = isOpen ? openPosition : closedPosition;
        transform.position = Vector3.Lerp(transform.position, targetPosition, slideSpeed * Time.deltaTime);
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw proximity radius
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, proximityRadius);
        
        // Draw slide direction
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + slideOffset);
    }
}
