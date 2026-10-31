using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Controls and placeholder bell for the separate SoundProgressionTest scene only.</summary>
public class SoundProgressionTestSetup : MonoBehaviour
{
    private GameProgression progression;
    private NoiseMaskingSource cover;
    private AudioSource bell;
    private AudioClip clip;

    void Awake()
    {
        progression = gameObject.AddComponent<GameProgression>();
        gameObject.AddComponent<EnemyThreat>().progression = progression;
        var clock = new GameObject("Prototype Bell Cover");
        clock.transform.position = new Vector3(0f, 1f, -4f);
        bell = clock.AddComponent<AudioSource>();
        bell.playOnAwake = false;
        bell.spatialBlend = 0f;
        bell.volume = 0.5f;
        // Original placeholder bell, not a final production sound asset.
        const int rate = 22050;
        var samples = new float[rate * 4];
        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / (float)rate;
            float pulse = t % 1f;
            samples[i] = 0.3f * Mathf.Min(1f, pulse * 200f) * Mathf.Exp(-4f * pulse) *
                (Mathf.Sin(2f * Mathf.PI * 440f * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * 1174f * t));
        }
        clip = AudioClip.Create("Prototype Four Second Bell", samples.Length, 1, rate, false);
        clip.SetData(samples, 0);
        bell.clip = clip;
        cover = clock.AddComponent<NoiseMaskingSource>();
        cover.radius = 15f;
        cover.hearingRadiusMultiplier = 0.25f;
    }

    void Start()
    {
        StartCoroutine(SoundProgressionChecks.Run());
        Debug.Log("Sound progression lab: F6 = awaken (safe); F7 = danger; F8 = mastery milestone; F9 = four seconds of bell cover. Starts safe.", this);
    }

    void Update()
    {
        if (Keyboard.current == null || Echo.Menu.GameMenu.BlocksPlayerInput || Time.timeScale <= 0f) return;
        if (Keyboard.current.f6Key.wasPressedThisFrame) { progression.Awaken(); Report(); }
        if (Keyboard.current.f7Key.wasPressedThisFrame) { progression.RaiseDanger(); Report(); }
        if (Keyboard.current.f8Key.wasPressedThisFrame) { progression.UnlockSoundTool(); Report(); }
        if (Keyboard.current.f9Key.wasPressedThisFrame) cover.PlayCover();
    }

    private void Report() { Debug.Log($"Progression: {progression.CurrentStage}", this); }

    void OnDestroy()
    {
        if (cover != null) Destroy(cover.gameObject);
        if (clip != null) Destroy(clip);
    }
}
