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

    // The music volume set in the Inspector, before the player's setting is applied.
    private float musicBaseVolume = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (musicSource != null)
            musicBaseVolume = musicSource.volume;

        ApplyVolumes();
        GameSettings.VolumeChanged += ApplyVolumes;
    }

    private void OnDestroy()
    {
        GameSettings.VolumeChanged -= ApplyVolumes;
    }

    private void ApplyVolumes()
    {
        if (musicSource != null)
            musicSource.volume = musicBaseVolume * GameSettings.MusicVolume;
    }

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip, volume * GameSettings.SfxVolume);
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

    // ---------- Pause / resume (used by dialogue lines) ----------

    private bool musicPaused;
    private bool resumeOnNextDoor;

    public void PauseMusic()
    {
        if (musicSource == null || !musicSource.isPlaying)
            return;

        musicSource.Pause();
        musicPaused = true;
        resumeOnNextDoor = false;
    }

    public void ResumeMusic()
    {
        if (musicSource == null || !musicPaused)
            return;

        musicSource.UnPause();
        musicPaused = false;
        resumeOnNextDoor = false;
    }

    // The music comes back the next time the player goes through a door.
    public void ResumeMusicOnNextDoor()
    {
        if (musicPaused)
            resumeOnNextDoor = true;
    }

    // Called by the RoomTransitionManager when the player changes room.
    public void OnPlayerChangedRoom()
    {
        if (resumeOnNextDoor)
            ResumeMusic();
    }
    private void Start()
    {
        if (playMusicOnStart && startingMusic != null)
        {
            PlayMusic(startingMusic, true);
        }
    }
}
