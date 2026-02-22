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

    private string basePrompt = @"Unity projesinde bir ai arabaya verilen görevler doğrultusunda yalnızca belirlenen komutları verebilen bir botsun. Elinde sadece bazı komut setleri var sadece onları kullanabiliyorsun.

""move(int gas)"" aracı ileri, geri hareket haline geçirmek için kullanılır gas -1(geri) 0(dur) ve 1(ileri) alabilir.(sleep komutu ile birlikte kullanarak hareketi belirli bir süre devam ettirebilirsin.)
""turn(int direction)"" aracı anlık belirli bir açıda döndürmeye yarar -180 ve 180 derece alır. Sağa döndürmek için pozitif derece vermelisin. Sol için negatif derece vermelisin.
""sleep(float time)"" bekleme komutudur time değişkeni saniye cinsinden değer alır (örn: 0.5, 1.5, 2).
""message(string text)"" console.log gibi çalışır herhangi bir şekilde kullanıcıyı bildirmek için kullanabilirsin.
""stop(bool result)"" görevi başarılı bir şekilde yapıp yapamadığını belirtir.

Belirlediğim komutlar dışında herhangi birşey kullanamazsın. Herhangi açıklama veya yorum satırı istemiyorum sadece komutlar olsun. (Yorum satırları yerine message() fonksiyonunu kullanabilirsin. Gerçekten başarılı değilsen varsayım yapma stop değişkenini buna göre kullan.

Görevin: ";

    [SerializeField] private TMP_InputField userInputField;
    [SerializeField] private AICarController aiCarController;
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

    // JSON için özel karakterleri escape et
    string escapedPrompt = userPrompt
        .Replace("\\", "\\\\")
        .Replace("\"", "\\\"")
        .Replace("\n", "\\n")
        .Replace("\r", "\\r")
        .Replace("\t", "\\t");

    string jsonData = "{\"contents\":[{\"parts\":[{\"text\":\"" + escapedPrompt + "\"}]}]}";
    
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
            
            // Send response to AI car controller
            if (aiCarController != null)
            {
                aiCarController.PlayResponse(request.downloadHandler.text);
            }
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
            StartCoroutine(SendRequestToGemini(basePrompt + userPrompt));
        }
    }
}


