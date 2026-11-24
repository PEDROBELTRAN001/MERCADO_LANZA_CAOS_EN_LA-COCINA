using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

// Audio 2D compartido por ambas cámaras. Cada categoría pasa por su propia ganancia.
public class MarketAudio : MonoBehaviour
{
    public enum Channel { Radio, Background, Effects }
    public enum RadioPhase { Idle, Transition, Loading, Song }
    public const float SongDuration = 20f;
    public static MarketAudio Instance { get; private set; }
    public static string MusicFolder => Path.Combine(Application.streamingAssetsPath, "RadioMusic");
    public RadioPhase Phase { get; private set; }
    public string CurrentSongName { get; private set; } = "";
    public float SegmentStart { get; private set; }
    public float SecondsRemaining => Mathf.Max(0f, remaining);
    public AudioSource RadioSource { get; private set; }
    public AudioSource BackgroundSource { get; private set; }
    public AudioSource MissionSource { get; private set; }
    public AudioSource PickupSource { get; private set; }
    public AudioSource LossSource { get; private set; }
    private readonly float[] volumes = new float[3];
    private readonly bool[] enabledChannels = new bool[3];
    private readonly List<string> songs = new List<string>();
    private AudioClip transition, loadedSong;
    private GameManager game;
    private OrderSystem orders;
    private Coroutine radioCycle;
    private UnityWebRequest songRequest;
    private string previousSong;
    private float remaining;
    private bool paused;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Attach()
    {
        var hud = FindAnyObjectByType<GameHUD>();
        if (hud == null) return;
        if (hud.GetComponent<MarketAudio>() == null) hud.gameObject.AddComponent<MarketAudio>();
        if (hud.GetComponent<MarketPauseMenu>() == null) hud.gameObject.AddComponent<MarketPauseMenu>();
    }
    private void Awake()
    {
        Instance = this;
        float[] defaults = { .55f, .35f, .8f };
        for (int i = 0; i < 3; i++)
        {
            volumes[i] = Mathf.Clamp01(PlayerPrefs.GetFloat("Mercado.Audio.Volume." + i, defaults[i]));
            enabledChannels[i] = PlayerPrefs.GetInt("Mercado.Audio.Enabled." + i, 1) != 0;
        }
        RadioSource = Source("Radio", 128);
        BackgroundSource = Source("Ambiente del mercado", 160);
        MissionSource = Source("Campana de misión", 40);
        PickupSource = Source("Recogida de potenciador", 32);
        LossSource = Source("Derrota", 16);
        BackgroundSource.clip = Resources.Load<AudioClip>("Audio/mercado"); BackgroundSource.loop = true;
        MissionSource.clip = Resources.Load<AudioClip>("Audio/campana");
        PickupSource.clip = Resources.Load<AudioClip>("Audio/jump");
        LossSource.clip = Resources.Load<AudioClip>("Audio/error");
        transition = Resources.Load<AudioClip>("Audio/radio_transicion");
        ApplyVolumes();
    }
    private AudioSource Source(string title, int priority)
    {
        var root = new GameObject(title); root.transform.SetParent(transform, false);
        var source = root.AddComponent<AudioSource>();
        source.playOnAwake = false; source.loop = false; source.spatialBlend = 0f;
        source.priority = priority; source.dopplerLevel = 0f;
        return source;
    }
    private IEnumerator Start()
    {
        while (GameManager.Instance == null || OrderSystem.Instance == null) yield return null;
        game = GameManager.Instance; orders = OrderSystem.Instance;
        game.OnGameStarted += StartRound; game.OnGameStateChanged += StateChanged;
        game.OnGameOver += EndRound; orders.OnNewOrder += NewMission;
        if (BackgroundSource.clip != null) Play(BackgroundSource);
        if (game.CurrentState == GameManager.GameState.Playing) StartRound();
    }
    private bool ShouldPause => Time.timeScale <= 0f || (game != null && game.CurrentState == GameManager.GameState.Paused);
    private void Update()
    {
        bool next = ShouldPause;
        if (next == paused) return;
        paused = next;
        foreach (var source in Sources())
            if (paused) source.Pause(); else source.UnPause();
    }
    private AudioSource[] Sources() => new[] { RadioSource, BackgroundSource, MissionSource, PickupSource, LossSource };
    private void Play(AudioSource source)
    {
        source.Play();
        if (ShouldPause) source.Pause();
    }
    public float Volume(Channel channel) => volumes[(int)channel];
    public bool IsEnabled(Channel channel) => enabledChannels[(int)channel];
    public void SetVolume(Channel channel, float value)
    {
        int i = (int)channel; value = Mathf.Clamp01(value);
        if (Mathf.Approximately(volumes[i], value)) return;
        volumes[i] = value; PlayerPrefs.SetFloat("Mercado.Audio.Volume." + i, value); ApplyVolumes();
    }
    public void SetEnabled(Channel channel, bool value)
    {
        int i = (int)channel;
        if (enabledChannels[i] == value) return;
        enabledChannels[i] = value; PlayerPrefs.SetInt("Mercado.Audio.Enabled." + i, value ? 1 : 0); ApplyVolumes();
    }
    private float Gain(Channel channel) => IsEnabled(channel) ? Volume(channel) : 0f;
    private void ApplyVolumes()
    {
        RadioSource.volume = Gain(Channel.Radio); BackgroundSource.volume = Gain(Channel.Background);
        MissionSource.volume = PickupSource.volume = LossSource.volume = Gain(Channel.Effects);
    }
    private void NewMission(Order order)
    {
        if (PlayerController.GameplayActive && MissionSource.clip != null) MissionSource.PlayOneShot(MissionSource.clip);
    }
    public void PlayPickup()
    {
        if (PlayerController.GameplayActive && PickupSource.clip != null) PickupSource.PlayOneShot(PickupSource.clip);
    }
    private void StateChanged(GameManager.GameState state)
    {
        if (state == GameManager.GameState.Ready || state == GameManager.GameState.MainMenu) StopRadio();
    }
    private void StartRound()
    {
        StopRadio(); MissionSource.Stop(); PickupSource.Stop(); LossSource.Stop();
        RefreshSongs(); previousSong = null;
        radioCycle = StartCoroutine(RadioLoop());
    }
    private void RefreshSongs()
    {
        songs.Clear();
        if (!Directory.Exists(MusicFolder)) return;
        foreach (string file in Directory.GetFiles(MusicFolder))
            if (string.Equals(Path.GetExtension(file), ".mp3", StringComparison.OrdinalIgnoreCase)) songs.Add(file);
    }
    private IEnumerator RadioLoop()
    {
        while (true)
        {
            Phase = RadioPhase.Transition; CurrentSongName = "Sintonizando la radio…"; SegmentStart = 0f;
            if (transition != null)
            {
                RadioSource.clip = transition; RadioSource.loop = false; Play(RadioSource);
                yield return WaitSegment(transition.length);
                RadioSource.Stop();
            }
            RefreshSongs();
            if (songs.Count == 0)
            {
                Phase = RadioPhase.Idle; CurrentSongName = "Añade MP3 a RadioMusic";
                yield return WaitSegment(2f); continue;
            }
            int index = UnityEngine.Random.Range(0, songs.Count);
            if (songs.Count > 1 && songs[index] == previousSong)
                index = (index + UnityEngine.Random.Range(1, songs.Count)) % songs.Count;
            string file = songs[index]; previousSong = file;
            Phase = RadioPhase.Loading; CurrentSongName = Path.GetFileNameWithoutExtension(file);
            songRequest = UnityWebRequestMultimedia.GetAudioClip(new Uri(file).AbsoluteUri, AudioType.MPEG);
            yield return songRequest.SendWebRequest();
            bool success = songRequest.result == UnityWebRequest.Result.Success;
            if (success) loadedSong = DownloadHandlerAudioClip.GetContent(songRequest);
            songRequest.Dispose(); songRequest = null;
            if (!success || loadedSong == null || loadedSong.length <= 0f)
            {
                ReleaseSong(); yield return WaitSegment(1f); continue;
            }
            Phase = RadioPhase.Song;
            SegmentStart = UnityEngine.Random.Range(0f, Mathf.Max(0f, loadedSong.length - SongDuration));
            RadioSource.clip = loadedSong; RadioSource.loop = loadedSong.length < SongDuration;
            RadioSource.time = SegmentStart; Play(RadioSource);
            yield return WaitSegment(SongDuration);
            RadioSource.Stop(); RadioSource.clip = null; ReleaseSong();
        }
    }
    private IEnumerator WaitSegment(float duration)
    {
        remaining = duration;
        while (remaining > 0f)
        {
            yield return null;
            if (game != null && game.CurrentState == GameManager.GameState.Playing && !ShouldPause)
                remaining -= Time.unscaledDeltaTime;
        }
    }
    private void ReleaseSong() { if (loadedSong != null) Destroy(loadedSong); loadedSong = null; }
    private void StopRadio()
    {
        if (radioCycle != null) StopCoroutine(radioCycle); radioCycle = null;
        if (songRequest != null) { songRequest.Abort(); songRequest.Dispose(); songRequest = null; }
        if (RadioSource != null) { RadioSource.Stop(); RadioSource.clip = null; }
        ReleaseSong();
        remaining = 0f; Phase = RadioPhase.Idle; CurrentSongName = "";
    }
    private void EndRound()
    {
        StopRadio(); MissionSource.Stop(); PickupSource.Stop();
        if (!game.WorkersWon && LossSource.clip != null) Play(LossSource);
    }
    private void OnDestroy()
    {
        StopRadio(); PlayerPrefs.Save();
        if (game != null) { game.OnGameStarted -= StartRound; game.OnGameStateChanged -= StateChanged; game.OnGameOver -= EndRound; }
        if (orders != null) orders.OnNewOrder -= NewMission;
        if (Instance == this) Instance = null;
    }
}
