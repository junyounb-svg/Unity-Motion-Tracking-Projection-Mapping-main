using UnityEngine;

public class EmergencyAlarm : MonoBehaviour
{
    [Header("Light References")]
    [SerializeField] private Light[] lights;
    [SerializeField] private bool findLightsInChildren = true;
    
    [Header("Flicker Settings")]
    [SerializeField] private float flickerInterval = 0.1f;
    [SerializeField] private bool useRedEmergencyColor = true;
    [SerializeField] private Color emergencyColor = Color.red;
    
    [Header("Sound Settings")]
    [SerializeField] private AudioSource alarmAudioSource;
    [SerializeField] private AudioClip alarmSound;
    [SerializeField] private bool playOnStart = true;
    
    [Header("Proximity Transition (Door Reveal)")]
    [SerializeField] private bool useProximityTransition = true;
    [SerializeField] private Transform doorTransform;
    [SerializeField] private string doorObjectName = "Cube (4)";
    [SerializeField] private Transform playerTransform;
    [SerializeField] private string playerObjectName = "Bag.2_White";
    [SerializeField] private float proximityRadius = 3f;
    [SerializeField] private Color clearColor = Color.green;
    
    private float[] originalIntensities;
    private Color[] originalColors;
    private float flickerTimer;
    private bool lightsOn = true;
    private float transitionFactor; // 0 = full emergency, 1 = all clear (green, no flicker, no sound)
    
    void Start()
    {
        // Find lights if not assigned
        if (lights == null || lights.Length == 0)
        {
            if (findLightsInChildren)
            {
                lights = GetComponentsInChildren<Light>(true);
            }
            else
            {
                Light singleLight = GetComponent<Light>();
                if (singleLight != null)
                {
                    lights = new Light[] { singleLight };
                }
            }
            
            if (lights == null || lights.Length == 0)
            {
                Debug.LogWarning("EmergencyAlarm: No lights found. Assign lights manually or ensure this object has Light components.");
            }
        }
        
        // Store original light states
        if (lights != null && lights.Length > 0)
        {
            originalIntensities = new float[lights.Length];
            originalColors = new Color[lights.Length];
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null)
                {
                    originalIntensities[i] = lights[i].intensity;
                    originalColors[i] = lights[i].color;
                }
            }
        }
        
        // Setup audio
        if (alarmAudioSource == null)
        {
            alarmAudioSource = GetComponent<AudioSource>();
            if (alarmAudioSource == null)
            {
                alarmAudioSource = gameObject.AddComponent<AudioSource>();
                alarmAudioSource.playOnAwake = false;
                alarmAudioSource.loop = true;
            }
        }
        
        if (alarmSound != null)
        {
            alarmAudioSource.clip = alarmSound;
            alarmAudioSource.loop = true;
            if (playOnStart)
            {
                alarmAudioSource.Play();
            }
        }
        else
        {
            Debug.LogWarning("EmergencyAlarm: No alarm sound assigned. Assign an AudioClip to enable sound.");
        }
        
        // Find door and player for proximity transition
        if (useProximityTransition)
        {
            if (doorTransform == null)
            {
                GameObject doorObj = GameObject.Find(doorObjectName);
                if (doorObj != null) doorTransform = doorObj.transform;
                else Debug.LogWarning("EmergencyAlarm: Could not find door '" + doorObjectName + "'. Assign manually.");
            }
            if (playerTransform == null)
            {
                GameObject playerObj = GameObject.Find(playerObjectName);
                if (playerObj != null) playerTransform = playerObj.transform;
                else Debug.LogWarning("EmergencyAlarm: Could not find player '" + playerObjectName + "'. Assign manually.");
            }
        }
    }
    
    void Update()
    {
        // Calculate proximity-based transition (0 = full emergency, 1 = all clear)
        if (useProximityTransition && doorTransform != null && playerTransform != null)
        {
            float distance = Vector3.Distance(doorTransform.position, playerTransform.position);
            transitionFactor = distance <= proximityRadius ? (1f - distance / proximityRadius) : 0f;
        }
        else
        {
            transitionFactor = 0f;
        }
        
        // Fade/stop alarm sound based on transition
        if (alarmAudioSource != null && alarmSound != null)
        {
            alarmAudioSource.volume = Mathf.Clamp01(1f - transitionFactor);
            if (transitionFactor >= 0.99f && alarmAudioSource.isPlaying)
            {
                alarmAudioSource.Stop();
            }
            else if (transitionFactor < 0.99f && playOnStart && !alarmAudioSource.isPlaying)
            {
                alarmAudioSource.Play();
            }
        }
        
        if (lights == null || lights.Length == 0)
            return;
        
        // When transitioned (no flicker): solid color from red->green
        if (transitionFactor > 0.6f)
        {
            Color targetColor = Color.Lerp(emergencyColor, clearColor, transitionFactor);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] == null) continue;
                lights[i].enabled = true;
                lights[i].intensity = originalIntensities[i];
                lights[i].color = targetColor;
            }
            return;
        }
        
        // Flicker mode: scale speed based on transition (slower as we approach green)
        float effectiveFlickerInterval = Mathf.Lerp(flickerInterval, flickerInterval * 4f, transitionFactor);
        flickerTimer += Time.deltaTime;
        
        if (flickerTimer >= effectiveFlickerInterval)
        {
            flickerTimer = 0f;
            lightsOn = !lightsOn;
            
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] == null) continue;
                Color lightColor = Color.Lerp(useRedEmergencyColor ? emergencyColor : originalColors[i], clearColor, transitionFactor);
                
                if (lightsOn)
                {
                    lights[i].enabled = true;
                    lights[i].intensity = originalIntensities[i];
                    lights[i].color = lightColor;
                }
                else
                {
                    lights[i].enabled = false;
                }
            }
        }
    }
    
    public void PlayAlarm()
    {
        if (alarmAudioSource != null && alarmSound != null)
        {
            alarmAudioSource.Play();
        }
    }
    
    public void StopAlarm()
    {
        if (alarmAudioSource != null)
        {
            alarmAudioSource.Stop();
        }
        
        // Restore lights
        if (lights != null && originalIntensities != null && originalColors != null)
        {
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null && i < originalIntensities.Length && i < originalColors.Length)
                {
                    lights[i].enabled = true;
                    lights[i].intensity = originalIntensities[i];
                    lights[i].color = originalColors[i];
                }
            }
        }
    }
}
