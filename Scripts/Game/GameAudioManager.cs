using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameAudioManager : MonoBehaviour
{
    public static GameAudioManager Instance { get; private set; }

    [Header("MASTER VOLUME")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.25f;

    [Header("CANDY PICKUP")]
    public AudioClip[] candyPickupClips;
    [Range(0f, 1f)] public float candyVolume = 0.6f;
    public Vector2 candyPitchRange = new Vector2(0.92f, 1.08f);

    [Header("OBJECT DESTRUCTION")]
    [Tooltip(
        "Default sound used by every destructible unless that object " +
        "assigns its own override clip."
    )]
    public AudioClip defaultDestructionClip;
    [Range(0f, 1f)] public float destructionVolume = 0.75f;
    public Vector2 destructionPitchRange = new Vector2(0.95f, 1.05f);

    [Header("OBJECT WOBBLE")]
    [Tooltip("Short warning sound played when a nearby object begins wobbling for the human player.")]
    public AudioClip wobbleClip;
    [Range(0f, 1f)] public float wobbleVolume = 0.45f;
    public Vector2 wobblePitchRange = new Vector2(0.92f, 1.08f);
    [Min(0f)] public float wobbleCooldown = 0.18f;
    [Min(0.1f)] public float wobbleMinimumDistance = 2f;
    [Min(1f)] public float wobbleMaximumDistance = 18f;

    [Header("SUCTION")]
    public AudioClip suctionLoop;
    [Range(0f, 1f)] public float suctionVolume = 0.35f;

    [Header("TORNADO ELIMINATION")]
    public AudioClip eliminationImpact;
    public AudioClip eliminationVacuum;
    [Range(0f, 1f)] public float eliminationVolume = 1f;

    [Header("GROWTH MILESTONE")]
    public AudioClip growthMilestone;
    [Range(0f, 1f)] public float growthVolume = 0.8f;

    [Tooltip("How much the player's visible tornado scale must increase before the growth sting plays again.")]
    [Min(0.01f)]
    public float growthMilestoneStep = 0.25f;

    [Header("PLAYER DEFEATED")]
    public AudioClip defeatSting;
    [Range(0f, 1f)] public float defeatVolume = 1f;

    [Header("VICTORY")]
    public AudioClip victorySting;
    [Range(0f, 1f)] public float victoryVolume = 1f;

    [Header("BACKGROUND MUSIC")]
    public AudioClip gameplayMusic;
    public bool playMusicOnStart = true;

    private AudioSource sfxSource;
    private AudioSource suctionSource;
    private AudioSource musicSource;
    private AudioSource[] wobbleSources;

    private float nextGrowthMilestone = -1f;
    private float nextWobbleTime;
    private int nextWobbleSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        sfxSource = CreateSource("SFX");
        suctionSource = CreateSource("Suction");
        musicSource = CreateSource("Music");

        wobbleSources = new AudioSource[4];

        for (int i = 0; i < wobbleSources.Length; i++)
        {
            wobbleSources[i] = CreateSpatialSource(
                "Wobble " + (i + 1)
            );
        }

        suctionSource.loop = true;
        musicSource.loop = true;
    }

    private void Start()
    {
        if (playMusicOnStart)
        {
            PlayMusic();
        }
    }

    private AudioSource CreateSource(string sourceName)
    {
        GameObject obj = new GameObject(sourceName);

        obj.transform.SetParent(transform);

        AudioSource source =
            obj.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.spatialBlend = 0f;

        return source;
    }

    private AudioSource CreateSpatialSource(string sourceName)
    {
        AudioSource source = CreateSource(sourceName);
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.dopplerLevel = 0f;
        source.minDistance = Mathf.Max(0.1f, wobbleMinimumDistance);
        source.maxDistance = Mathf.Max(
            source.minDistance,
            wobbleMaximumDistance
        );
        return source;
    }

    // --------------------------------------------------
    // CANDY
    // --------------------------------------------------

    public void PlayCandyPickup()
    {
        PlayRandom(
            candyPickupClips,
            candyVolume,
            candyPitchRange
        );
    }

    // --------------------------------------------------
    // OBJECT DESTRUCTION
    // --------------------------------------------------

    public void PlayDestruction(
        AudioClip overrideClip = null)
    {
        PlayWithPitch(
            overrideClip != null
                ? overrideClip
                : defaultDestructionClip,
            destructionVolume,
            destructionPitchRange
        );
    }

    public void PlayWobble(Vector3 worldPosition)
    {
        if (Time.unscaledTime < nextWobbleTime ||
            wobbleClip == null ||
            wobbleSources == null ||
            wobbleSources.Length == 0)
        {
            return;
        }

        nextWobbleTime =
            Time.unscaledTime + Mathf.Max(0f, wobbleCooldown);

        AudioSource source = GetAvailableWobbleSource();

        source.transform.position = worldPosition;
        source.clip = wobbleClip;
        source.volume = wobbleVolume * sfxVolume * masterVolume;
        source.pitch = Random.Range(
            wobblePitchRange.x,
            wobblePitchRange.y
        );
        source.minDistance = Mathf.Max(0.1f, wobbleMinimumDistance);
        source.maxDistance = Mathf.Max(
            source.minDistance,
            wobbleMaximumDistance
        );
        source.Play();
    }

    private AudioSource GetAvailableWobbleSource()
    {
        for (int i = 0; i < wobbleSources.Length; i++)
        {
            int index = (nextWobbleSource + i) % wobbleSources.Length;

            if (!wobbleSources[index].isPlaying)
            {
                nextWobbleSource = (index + 1) % wobbleSources.Length;
                return wobbleSources[index];
            }
        }

        AudioSource source = wobbleSources[nextWobbleSource];
        nextWobbleSource = (nextWobbleSource + 1) % wobbleSources.Length;
        source.Stop();
        return source;
    }

    // --------------------------------------------------
    // SUCTION
    // --------------------------------------------------

    public void StartSuction()
    {
        if (suctionSource == null ||
            suctionLoop == null ||
            suctionSource.isPlaying)
        {
            return;
        }

        suctionSource.clip =
            suctionLoop;

        suctionSource.volume =
            suctionVolume *
            sfxVolume *
            masterVolume;

        suctionSource.pitch = 1f;

        suctionSource.Play();
    }

    public void StopSuction()
    {
        if (suctionSource != null &&
            suctionSource.isPlaying)
        {
            suctionSource.Stop();
        }
    }

    // --------------------------------------------------
    // ELIMINATION
    // --------------------------------------------------

    public void PlayElimination()
    {
        PlayOneShot(
            eliminationImpact,
            eliminationVolume
        );

        PlayOneShot(
            eliminationVacuum,
            eliminationVolume * 0.8f
        );
    }

    // --------------------------------------------------
    // GROWTH
    // --------------------------------------------------

    public void PlayGrowthMilestone()
    {
        PlayOneShot(
            growthMilestone,
            growthVolume
        );
    }

    public void ReportPlayerGrowth(
        float currentVisualScale)
    {
        float step =
            Mathf.Max(
                0.01f,
                growthMilestoneStep
            );

        /*
         * First report establishes the next target.
         * We do NOT play a sound immediately.
         */
        if (nextGrowthMilestone < 0f)
        {
            nextGrowthMilestone =
                currentVisualScale +
                step;

            return;
        }

        if (currentVisualScale <
            nextGrowthMilestone)
        {
            return;
        }

        PlayGrowthMilestone();

        /*
         * If one large pickup jumps through several
         * thresholds, only play the sound once and
         * move the target forward.
         */
        while (currentVisualScale >=
               nextGrowthMilestone)
        {
            nextGrowthMilestone +=
                step;
        }
    }

    public void ResetGrowthMilestones(
        float currentVisualScale = 0f)
    {
        nextGrowthMilestone =
            Mathf.Max(
                0f,
                currentVisualScale
            ) +
            Mathf.Max(
                0.01f,
                growthMilestoneStep
            );
    }

    // --------------------------------------------------
    // PLAYER DEFEATED
    // --------------------------------------------------

    public void PlayDefeat()
    {
        PlayOneShot(
            defeatSting,
            defeatVolume
        );
    }

    // --------------------------------------------------
    // VICTORY
    // --------------------------------------------------

    public void PlayVictory()
    {
        PlayOneShot(
            victorySting,
            victoryVolume
        );
    }

    // --------------------------------------------------
    // MUSIC
    // --------------------------------------------------

    public void PlayMusic()
    {
        if (musicSource == null ||
            gameplayMusic == null)
        {
            return;
        }

        if (musicSource.isPlaying)
        {
            return;
        }

        musicSource.clip =
            gameplayMusic;

        musicSource.volume =
            musicVolume *
            masterVolume;

        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    // --------------------------------------------------
    // INTERNAL HELPERS
    // --------------------------------------------------

    private void PlayRandom(
        AudioClip[] clips,
        float volume,
        Vector2 pitchRange)
    {
        if (clips == null ||
            clips.Length == 0 ||
            sfxSource == null)
        {
            return;
        }

        AudioClip clip =
            clips[
                Random.Range(
                    0,
                    clips.Length
                )
            ];

        if (clip == null ||
            sfxSource == null)
        {
            return;
        }

        sfxSource.pitch =
            Random.Range(
                pitchRange.x,
                pitchRange.y
            );

        sfxSource.PlayOneShot(
            clip,
            volume *
            sfxVolume *
            masterVolume
        );
    }

    private void PlayWithPitch(
        AudioClip clip,
        float volume,
        Vector2 pitchRange)
    {
        if (clip == null ||
            sfxSource == null)
        {
            return;
        }

        sfxSource.pitch =
            Random.Range(
                pitchRange.x,
                pitchRange.y
            );

        sfxSource.PlayOneShot(
            clip,
            volume *
            sfxVolume *
            masterVolume
        );
    }

    private void PlayOneShot(
        AudioClip clip,
        float volume)
    {
        if (clip == null ||
            sfxSource == null)
        {
            return;
        }

        sfxSource.pitch = 1f;

        sfxSource.PlayOneShot(
            clip,
            volume *
            sfxVolume *
            masterVolume
        );
    }

    // --------------------------------------------------
    // GLOBAL VOLUME CONTROLS
    // --------------------------------------------------

    public void SetMasterVolume(
        float value)
    {
        masterVolume =
            Mathf.Clamp01(value);

        RefreshContinuousVolumes();
    }

    public void SetSFXVolume(
        float value)
    {
        sfxVolume =
            Mathf.Clamp01(value);

        RefreshContinuousVolumes();
    }

    public void SetMusicVolume(
        float value)
    {
        musicVolume =
            Mathf.Clamp01(value);

        RefreshContinuousVolumes();
    }

    private void RefreshContinuousVolumes()
    {
        if (musicSource != null)
        {
            musicSource.volume =
                musicVolume *
                masterVolume;
        }

        if (suctionSource != null)
        {
            suctionSource.volume =
                suctionVolume *
                sfxVolume *
                masterVolume;
        }
    }

    private void OnDisable()
    {
        StopSuction();
    }

    private void OnDestroy()
    {
        StopSuction();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }
}