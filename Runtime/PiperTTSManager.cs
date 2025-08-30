using UnityEngine;
using Unity.InferenceEngine; // Обновлено для версии 2.3.0
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace prograymm.custompipertts
{
    public class PiperTTSManager : MonoBehaviour
    {
        [SerializeField] private ModelAsset modelAsset;
        [SerializeField] private TextAsset phonemeMapJson;
        [SerializeField] private string voice = "en"; // По умолчанию en для en_US-amy-low
        [SerializeField] private int sampleRate = 16000; // en_US-amy-low использует 16000 Hz
        [SerializeField] private float noiseScale = 0.667f;
        [SerializeField] private float lengthScale = 1.0f;
        [SerializeField] private float noiseW = 0.8f;

        private Model model;
        private Worker worker; // Sentis использует Worker (ранее IWorker в старых версиях)
        private Dictionary<string, int> phonemeIdMap;

        void Awake()
        {
            if (modelAsset == null || phonemeMapJson == null)
            {
                Debug.LogError("ModelAsset or PhonemeMapJson not assigned!");
                return;
            }

            try
            {
                model = ModelLoader.Load(modelAsset);
                worker = new Worker(model, BackendType.GPUCompute);
                LoadPhonemeMap();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to initialize Piper model: {e.Message}");
            }
        }

        private void LoadPhonemeMap()
        {
            try
            {
                var json = JsonUtility.FromJson<PhonemeMapJson>(phonemeMapJson.text);
                phonemeIdMap = new Dictionary<string, int>();
                foreach (var kv in json.phoneme_id_map)
                {
                    phonemeIdMap[kv.Key] = kv.Value[0];
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load phoneme map: {e.Message}");
            }
        }

        public async Task<AudioClip> GenerateSpeechAsync(string text, string language = "en")
        {
            if (worker == null || phonemeIdMap == null)
            {
                Debug.LogError("Piper not initialized!");
                return null;
            }

            try
            {
                string phonemes = eSpeakPhonemizer.TextToPhonemes(text, language);
                if (string.IsNullOrEmpty(phonemes))
                {
                    Debug.LogError("Phonemization failed!");
                    return null;
                }

                List<int> inputIds = new List<int>();
                foreach (string p in phonemes.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (phonemeIdMap.TryGetValue(p, out int id))
                    {
                        inputIds.Add(id);
                    }
                }

                using var inputIdsTensor = new Tensor<int>(new TensorShape(1, inputIds.Count), inputIds.ToArray());
                using var lengthsTensor = new Tensor<int>(new TensorShape(1, 1), new int[] { inputIds.Count });
                using var scalesTensor = new Tensor<float>(new TensorShape(1, 3), new float[] { noiseScale, lengthScale, noiseW });
                using var sidTensor = new Tensor<int>(new TensorShape(1), new int[] { 0 });

                worker.Schedule(inputIdsTensor, lengthsTensor, scalesTensor, sidTensor);

                // Ждем завершения выполнения, если необходимо (для синхронного поведения)
                // В асинхронном контексте можно использовать await, но для простоты используем блокирующий вызов
                using var outputTensor = worker.PeekOutput("audio") as Tensor<float>;
                float[] audioData = outputTensor.DownloadToArray();

                AudioClip clip = AudioClip.Create("TTSClip", audioData.Length, 1, sampleRate, false);
                clip.SetData(audioData, 0);
                return clip;
            }
            catch (Exception e)
            {
                Debug.LogError($"Speech generation failed: {e.Message}");
                return null;
            }
        }

        void OnDestroy()
        {
            worker?.Dispose();
        }

        [System.Serializable]
        private class PhonemeMapJson
        {
            public Dictionary<string, int[]> phoneme_id_map;
        }
    }
}