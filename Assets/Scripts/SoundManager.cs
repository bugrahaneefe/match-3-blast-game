using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;
    public AudioSource audioSource;
    public AudioClip cubeExplodeSound;
    public AudioClip balloonExplodeSound;
    public AudioClip duckExplodeSound;


    #region Singleton
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        audioSource = GetComponent<AudioSource>();
    }
    #endregion

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

    public void PlayBalloonExplode()
    {
        PlaySound(balloonExplodeSound);
    }

    public void PlayDuckExplode()
    {
        PlaySound(duckExplodeSound);
    }
}
