using UnityEngine;

public class GlobalAudioManager : MonoBehaviour
{
    public static GlobalAudioManager Instance;
    private AudioSource audioSource;


    void Start()
    {
        if (audioSource != null)
        {
            Debug.Log("AUDIO SANITY CHECK: Is Playing? " + audioSource.isPlaying);
            Debug.Log("AUDIO SANITY CHECK: Is Muted? " + audioSource.mute);
            Debug.Log("AUDIO SANITY CHECK: Volume level? " + audioSource.volume);
        }
    }
    
    void Awake()
    {
        // Singleton pattern: keeps this audio object alive across ALL scenes
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            audioSource = GetComponent<AudioSource>();
        }
        else
        {
            Destroy(gameObject); // Delete duplicates if we return to Main Menu
        }
    }

    public void ToggleMute()
    {
        if (audioSource != null)
        {
            audioSource.mute = !audioSource.mute;
        }
    }

    public bool IsMuted()
    {
        return audioSource != null && audioSource.mute;
    }
}