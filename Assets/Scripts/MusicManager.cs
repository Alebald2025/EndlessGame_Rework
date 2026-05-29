using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Música")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip gameplayMusic;

    [Header("Configuración de reproducción")]
    [SerializeField] [Range(0f, 1f)] private float volume = 0.60f;
    [SerializeField] private float crossfadeDuration = 1f;

    private AudioSource audioSource;
    private Coroutine fadeCoroutine;

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

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.volume = volume;
    }

    public void PlayMenuMusic()
    {
        PlayMusic(menuMusic);
    }

    public void PlayGameplayMusic()
    {
        PlayMusic(gameplayMusic);
    }

    private void PlayMusic(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("[MusicManager] No se asignó la pista de audio.");
            return;
        }

        if (audioSource.clip == clip && audioSource.isPlaying)
            return;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeToClip(clip));
    }

    private IEnumerator FadeToClip(AudioClip clip)
    {
        float startVolume = audioSource.volume;

        if (audioSource.isPlaying)
        {
            float elapsed = 0f;
            while (elapsed < crossfadeDuration)
            {
                elapsed += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / crossfadeDuration);
                yield return null;
            }

            audioSource.Stop();
        }

        audioSource.clip = clip;
        audioSource.Play();
        audioSource.volume = 0f;

        float fadeInElapsed = 0f;
        while (fadeInElapsed < crossfadeDuration)
        {
            fadeInElapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, volume, fadeInElapsed / crossfadeDuration);
            yield return null;
        }

        audioSource.volume = volume;
        fadeCoroutine = null;
    }
}
