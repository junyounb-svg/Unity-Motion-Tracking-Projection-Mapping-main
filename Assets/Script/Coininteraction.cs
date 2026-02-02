using UnityEngine;

public class CoinCollectible : MonoBehaviour
{
    [Header("Player Reference")]
    [SerializeField] private string playerObjectName = "Bag.2_White";
    [SerializeField] private Transform playerTransform;
    
    [Header("Shine Settings")]
    [SerializeField] private float maxShineDistance = 10f;
    [SerializeField] private float minShineDistance = 1f;
    [SerializeField] private float minEmissionIntensity = 0.1f;
    [SerializeField] private float maxEmissionIntensity = 2f;
    [SerializeField] private Color emissionColor = Color.yellow;
    
    [Header("Collection Settings")]
    [SerializeField] private float collectionRadius = 0.5f;
    [SerializeField] private bool useDistanceBasedCollection = true; // Primary method - works without colliders
    [SerializeField] private bool useTrigger = true; // Fallback method - requires colliders on both objects
    [SerializeField] private bool destroyOnCollection = true;
    [SerializeField] private GameObject collectionEffectPrefab;
    
    private Renderer coinRenderer;
    private Material coinMaterial;
    private bool isCollected = false;
    private float currentDistance;
    
    void Start()
    {
        // Get the Renderer component
        coinRenderer = GetComponent<Renderer>();
        if (coinRenderer == null)
        {
            coinRenderer = GetComponentInChildren<Renderer>();
        }
        
        if (coinRenderer == null)
        {
            Debug.LogError("CoinCollectible: No Renderer found on " + gameObject.name);
            enabled = false;
            return;
        }
        
        // Get or create material instance
        coinMaterial = coinRenderer.material;
        
        // Enable emission on the material
        if (coinMaterial.HasProperty("_EmissionColor"))
        {
            coinMaterial.EnableKeyword("_EMISSION");
        }
        
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
                Debug.LogWarning("CoinCollectible: Could not find player object named '" + playerObjectName + "'. Please assign it manually.");
            }
        }
        
        // Setup collider for collection detection (only if using trigger/collision method)
        if (!useDistanceBasedCollection)
        {
            SetupCollider();
        }
    }
    
    void SetupCollider()
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            col = GetComponentInChildren<Collider>();
        }
        
        if (col == null)
        {
            // Add a sphere collider if none exists
            SphereCollider sphereCol = gameObject.AddComponent<SphereCollider>();
            sphereCol.radius = collectionRadius;
            sphereCol.isTrigger = useTrigger;
            Debug.Log("CoinCollectible: Added SphereCollider with radius " + collectionRadius);
        }
        else
        {
            // Ensure the collider is set as trigger if needed
            if (useTrigger && !col.isTrigger)
            {
                col.isTrigger = true;
                Debug.Log("CoinCollectible: Set collider as trigger");
            }
            
            // Adjust radius if it's a sphere collider
            if (col is SphereCollider)
            {
                (col as SphereCollider).radius = collectionRadius;
            }
        }
    }
    
    void Update()
    {
        if (isCollected || playerTransform == null)
            return;
        
        // Calculate distance to player
        currentDistance = Vector3.Distance(transform.position, playerTransform.position);
        
        // Update shine intensity based on distance
        UpdateShineIntensity();
        
        // Check for collection using distance-based method (primary)
        if (useDistanceBasedCollection)
        {
            if (currentDistance <= collectionRadius)
            {
                CollectCoin();
            }
        }
    }
    
    void UpdateShineIntensity()
    {
        if (coinMaterial == null || !coinMaterial.HasProperty("_EmissionColor"))
            return;
        
        // Calculate normalized distance (0 = closest, 1 = farthest)
        float normalizedDistance = Mathf.Clamp01((currentDistance - minShineDistance) / (maxShineDistance - minShineDistance));
        
        // Reverse it so closer = brighter (0 = brightest, 1 = dimmest)
        float intensityFactor = 1f - normalizedDistance;
        
        // Calculate emission intensity
        float emissionIntensity = Mathf.Lerp(minEmissionIntensity, maxEmissionIntensity, intensityFactor);
        
        // Apply emission color with intensity
        Color finalEmissionColor = emissionColor * emissionIntensity;
        coinMaterial.SetColor("_EmissionColor", finalEmissionColor);
        
        // Also update the global illumination if using HDRP/URP
        if (coinMaterial.HasProperty("_EmissionIntensity"))
        {
            coinMaterial.SetFloat("_EmissionIntensity", emissionIntensity);
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Only use trigger if distance-based collection is disabled
        if (!useDistanceBasedCollection && useTrigger && !isCollected)
        {
            CheckCollection(other.gameObject);
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // Only use collision if distance-based collection is disabled
        if (!useDistanceBasedCollection && !useTrigger && !isCollected)
        {
            CheckCollection(collision.gameObject);
        }
    }
    
    void CheckCollection(GameObject other)
    {
        // Check if the colliding object is the player
        if (other.name == playerObjectName || other.transform == playerTransform || 
            other.transform.IsChildOf(playerTransform))
        {
            CollectCoin();
        }
    }
    
    void CollectCoin()
    {
        if (isCollected)
            return;
        
        isCollected = true;
        
        Debug.Log("Coin collected by " + playerObjectName + "! Distance was: " + currentDistance.ToString("F2"));
        
        // Spawn collection effect if assigned
        if (collectionEffectPrefab != null)
        {
            Instantiate(collectionEffectPrefab, transform.position, transform.rotation);
        }
        
        // Destroy or disable the coin
        if (destroyOnCollection)
        {
            Destroy(gameObject);
        }
        else
        {
            // Just disable the renderer and collider
            if (coinRenderer != null)
                coinRenderer.enabled = false;
            
            Collider col = GetComponent<Collider>();
            if (col != null)
                col.enabled = false;
            
            // Disable this script
            enabled = false;
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw collection radius
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, collectionRadius);
        
        // Draw shine distance range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, minShineDistance);
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, maxShineDistance);
    }
}
