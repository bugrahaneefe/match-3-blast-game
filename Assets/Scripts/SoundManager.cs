using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    public AudioSource audioSource;  // ✅ General AudioSource
    public AudioClip cubeExplodeSound;  // ✅ Explosion sound (can add more later)

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);  // Prevent duplicates
            return;
        }

        DontDestroyOnLoad(gameObject);  // Keep sound manager across scenes
        audioSource = GetComponent<AudioSource>();
    }

    public void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public void PlayCubeExplode()
    {
        PlaySound(cubeExplodeSound);
    }
}
