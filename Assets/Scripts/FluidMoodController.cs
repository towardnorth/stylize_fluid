using Obi;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;


public class Message
{
    public string role { get; set; }
    public string content { get; set; }
}

public class Choice
{
    public int index { get; set; }
    public Message message { get; set; }
}

public class ApiResponse
{
    public List<Choice> choices { get; set; }
}

public class FluidMoodController : MonoBehaviour
{
    public ObiEmitter ObiEmitter;
    private const string apiUrl= "https://api.openai-proxy.org/v1/chat/completions";
    private const string apiKey= "sk-m8EWphBNxbRE1D3NxamRq5AFH8F3tAegx8x0s3gbUEJwQ5LL";

    public void RequestFluidParams(string mood)
    {
        StartCoroutine(GetFluidParameters(mood));
    }

    private IEnumerator GetFluidParameters(string mood)
    {
        System.Net.ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, SslPolicyErrors) => true;
        var requestData = new
        {
            model = "gpt-3.5-turbo",
            messages = new[]
            {
                new { role = "user",content = $"��������\"{mood}\"��������ģ��Ĳ������������¸�ʽ��color: [��ɫ], smoothness: [ƽ��ֵ]������ color: red, smoothness: 0.8��" }
            },
            max_tokens = 50
        };
        string jsonData = JsonConvert.SerializeObject(requestData);
        UnityWebRequest request =new UnityWebRequest(apiUrl,"POST");
        byte[] bodyRaw =System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler =new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.ConnectionError ||
        request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"Error: {request.error}");
            Debug.LogError($"Response Code: {request.responseCode}");
            Debug.LogError($"Response Text: {request.downloadHandler.text}");
        }
        else
        {
            Debug.Log("Success: " + request.downloadHandler.text);
        }
    }
    
    private void ParseMoodParameters(string responseText)
    {
        if (string.IsNullOrEmpty(responseText))
        {
            Debug.LogError("responseText Ϊ�ջ�δ���壬�޷�����������");
            return;
        }
        Color fluidColor= Color.white;
        float smoothness = 0.88f;

        string[] parameters=responseText.Split(',');

        foreach (var param in parameters)
        {
            string[] kv = param.Split(':');
            if (kv.Length == 2)
            {
                string key = kv[0].Trim().ToLower();
                string value = kv[1].Trim();
                if (key == "color")
                {
                    if (!ColorUtility.TryParseHtmlString(value, out fluidColor))
                    {
                        Debug.LogError($"�޷�������ɫֵ: {value}");
                    }
                }
                else if (key == "smoothness")
                {
                    if (!float.TryParse(value, out float parsedSmoothness))
                    {
                        Debug.LogError($"�޷������ٶ�ֵ: {value}");
                    }
                    else
                    {
                        smoothness = parsedSmoothness;
                    }
                }
            }

        }
        ApplyFluidParameters(fluidColor, smoothness);
    }
    private void ApplyFluidParameters(Color color, float smoothness)
    {
        if (ObiEmitter != null)
        {
            var fluidSurfaceMesher=ObiEmitter.GetComponent<ObiFluidSurfaceMesher>();
            if(fluidSurfaceMesher != null)
            {
                fluidSurfaceMesher.GetType().GetProperty("turbidity");
                fluidSurfaceMesher.GetType().GetProperty("smoothness");
            }
            else
            {
                Debug.LogError("ObiFluidSurfaceMesher δ�ҵ���ȷ��������ӵ� ObiEmitter ��");
            }
        }
        else
        {
            Debug.LogError("ObiEmitter Ϊ�գ������Ƿ�����ȷ��ֵ");
        }
    }
}
