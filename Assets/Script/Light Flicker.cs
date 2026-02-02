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
    
    private float[] originalIntensities;
    private Color[] originalColors;
    private float flickerTimer;
    private bool lightsOn = true;
    
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
    }
    
    void Update()
    {
        if (lights == null || lights.Length == 0)
            return;
        
        flickerTimer += Time.deltaTime;
        
        if (flickerTimer >= flickerInterval)
        {
            flickerTimer = 0f;
            lightsOn = !lightsOn;
            
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] == null) continue;
                
                if (lightsOn)
                {
                    lights[i].enabled = true;
                    lights[i].intensity = originalIntensities[i];
                    lights[i].color = useRedEmergencyColor ? emergencyColor : originalColors[i];
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
        if (lights != null)
        {
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null)
                {
                    lights[i].enabled = true;
                    lights[i].intensity = originalIntensities[i];
                    lights[i].color = originalColors[i];
                }
            }
        }
    }
}
