using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine.UIElements;
using System;
using Unity.VisualScripting;
using TMPro;





#if UNITY_EDITOR
using UnityEditor;
#endif

public class GeminiManager : MonoBehaviour
{
    private string apiKey;
    // URL'yi doğrudan buraya daha temiz bir formatta koyalım
    private string baseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent";

    [SerializeField] private TMP_InputField userInputField;
    void Start()
    {
        CheckAndCreateKeyFile();
        if (!string.IsNullOrEmpty(apiKey))
        {
            StartCoroutine(SendRequestToGemini("Unity projemden sana ulaşıyorum, sesimi duyuyor musun?"));
        }
    }

    private void CheckAndCreateKeyFile()
    {
        string resourcesPath = Path.Combine(Application.dataPath, "Resources");
        string fullPath = Path.Combine(resourcesPath, "api_key.txt");

        if (!Directory.Exists(resourcesPath)) Directory.CreateDirectory(resourcesPath);

        if (!File.Exists(fullPath) || string.IsNullOrWhiteSpace(File.ReadAllText(fullPath)))
        {
            if (!File.Exists(fullPath)) File.WriteAllText(fullPath, "");
            Debug.LogError("<b>[Gemini]</b> API Anahtarı eksik! Assets/Resources/api_key.txt dosyasına anahtarı yapıştır.");
        }
        else
        {
            apiKey = File.ReadAllText(fullPath).Trim();
        }
    }

    IEnumerator SendRequestToGemini(string userPrompt)
    {
    // Model: gemini-2.5-flash
    string currentUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

    string jsonData = "{\"contents\":[{\"parts\":[{\"text\":\"" + userPrompt + "\"}]}]}";
    
    using (UnityWebRequest request = new UnityWebRequest(currentUrl, "POST"))
    {
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("<color=green>Başarılı!</color> Yanıt: " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError($"Hata: {request.responseCode} - {request.downloadHandler.text}");
        }
    }
    
    }
    public void OnClickSendPrompt()
    {
        string userPrompt = userInputField.text;
        if (!string.IsNullOrEmpty(userPrompt))
        {
            StartCoroutine(SendRequestToGemini(userPrompt));
        }
    }
}


