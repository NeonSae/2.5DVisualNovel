
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;
    [Header("Background Music")]
    [SerializeField] private AudioClip startingMusic;
    [SerializeField] private bool playMusicOnStart = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip, volume);
    }


    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null || musicSource == null)
            return;

        // Don't restart the same track if it's already playing.
        if (musicSource.clip == clip &&
            musicSource.isPlaying &&
            musicSource.loop == loop)
        {
            return;
        }

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }


    public void StopMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }
    private void Start()
    {
        if (playMusicOnStart && startingMusic != null)
        {
            PlayMusic(startingMusic, true);
        }
    }
}
