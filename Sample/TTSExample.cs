using UnityEngine;
using CustomPiperTTS;

namespace CustomPiperTTS
{
    public class TTSExample : MonoBehaviour
    {
        private PiperTTSManager ttsManager;
        private AudioSource audioSource;
        [SerializeField] private string text = "Привет, это тестовый голос для игры!";
        [SerializeField] private string language = "ru"; // ru или en

        async void Start()
        {
            ttsManager = GetComponent<PiperTTSManager>();
            audioSource = GetComponent<AudioSource>();
            if (ttsManager == null || audioSource == null)
            {
                Debug.LogError("PiperTTSManager or AudioSource not found!");
                return;
            }

            AudioClip clip = await ttsManager.GenerateSpeechAsync(text, language);
            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
                Debug.Log("Speech generated successfully!");
            }
        }
    }

}
