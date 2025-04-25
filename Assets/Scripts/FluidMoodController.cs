using Obi;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System;

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
    private const string apiUrl = "https://api.openai-proxy.org/v1/chat/completions";
    private const string apiKey = "sk-m8EWphBNxbRE1D3NxamRq5AFH8F3tAegx8x0s3gbUEJwQ5LL";

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
                new { role = "user", content = $"��������\"{mood}\"��������ģ��Ĳ������ϸ�ʹ�����¸�ʽ��color: [R, G, B, A], smoothness: [ƽ��ֵ]������ color: [0.2, 0.4, 0.6, 1.0], smoothness: 0.8��R, G, B, A ������ 0.0 �� 1.0 �ĸ�������color ��������������ĸ�ֵ������֮���ö��żӿո�, ���ָ���smoothness ��Ҫ�Ӿ�ţ�ȷ����������Ҹ�ʽ��ȷ��" }
            },
            max_tokens = 200
        };
        string jsonData = JsonConvert.SerializeObject(requestData);
        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("API ��Ӧ����: " + request.downloadHandler.text);
            ApiResponse response = JsonConvert.DeserializeObject<ApiResponse>(request.downloadHandler.text);
            if (response?.choices != null && response.choices.Count > 0)
            {
                string responseText = response.choices[0].message.content;
                Debug.Log("������ responseText: " + responseText);
                ParseMoodParameters(responseText);
            }
            else
            {
                Debug.LogError("API ��Ӧ��û�� choices �� choices Ϊ�ա���Ӧ����: " + request.downloadHandler.text);
            }
        }
        else
        {
            Debug.LogError("����ʧ��: " + request.error);
        }
    }

    private void ParseMoodParameters(string responseText)
    {
        if (string.IsNullOrEmpty(responseText))
        {
            Debug.LogError("responseText Ϊ�ջ�δ���壬�޷�����������");
            return;
        }
        Color fluidColor = Color.white; // Ĭ����ɫ
        float smoothness = 0.88f; // Ĭ��ƽ��ֵ

        // �� ", smoothness:" �ָ��Ϊ color �� smoothness ����
        string[] parts = responseText.Split(new[] { ", smoothness:" }, StringSplitOptions.RemoveEmptyEntries);
        Debug.Log("�� ', smoothness:' �ָ��Ĳ���: " + string.Join(" | ", parts));

        if (parts.Length != 2)
        {
            Debug.LogError($"���� 2 �����֣�color �� smoothness����ʵ�ʵõ� {parts.Length} ������: {responseText}");
            return;
        }

        // ���� color ����
        string colorPart = parts[0].Trim();
        string[] colorKv = colorPart.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
        if (colorKv.Length == 2 && colorKv[0].Trim().ToLower() == "color")
        {
            string value = colorKv[1].Trim();
            int startIndex = value.IndexOf('[');
            int endIndex = value.IndexOf(']');
            if (startIndex >= 0 && endIndex > startIndex)
            {
                string rgbaString = value.Substring(startIndex + 1, endIndex - startIndex - 1);
                string[] rgbaValues = rgbaString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (rgbaValues.Length == 4)
                {
                    try
                    {
                        float r = float.Parse(rgbaValues[0].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                        float g = float.Parse(rgbaValues[1].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                        float b = float.Parse(rgbaValues[2].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                        float a = float.Parse(rgbaValues[3].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                        fluidColor = new Color(r, g, b, a);
                    }
                    catch (FormatException ex)
                    {
                        Debug.LogError($"�޷����� RGBA ֵ: {value}, ����: {ex.Message}");
                    }
                }
                else
                {
                    Debug.LogError($"RGBA ��ʽ�������� 4 ��ֵ��ʵ�ʵõ� {rgbaValues.Length} ��ֵ: {value}");
                }
            }
            else
            {
                Debug.LogError($"�޷��ҵ� RGBA ���������������: {value}");
            }
        }
        else
        {
            Debug.LogError($"color ������ʽ����: {colorPart}");
        }

        // ���� smoothness ����
        string smoothnessPart = parts[1].Trim();
        if (!float.TryParse(smoothnessPart, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsedSmoothness))
        {
            Debug.LogError($"�޷�����ƽ��ֵ: {smoothnessPart}");
        }
        else
        {
            smoothness = parsedSmoothness;
        }

        ApplyFluidParameters(fluidColor, smoothness);
    }

    private void ApplyFluidParameters(Color color, float smoothness)
    {
        if (ObiEmitter != null)
        {
            var fluidSurfaceMesher = ObiEmitter.GetComponent<ObiFluidSurfaceMesher>();
            ObiFluidEmitterBlueprint obiFluidEmitterBlueprint = ObiEmitter.blueprint as ObiFluidEmitterBlueprint;
            if (fluidSurfaceMesher != null && obiFluidEmitterBlueprint != null)
            {
                ObiSolver solver = ObiEmitter.GetComponentInParent<ObiSolver>();
                ObiFluidEmitterBlueprint blueprintCopy = Instantiate(obiFluidEmitterBlueprint);
                if (solver == null)
                {
                    Debug.LogError("ObiSolver δ�ҵ�����ȷ�� ObiEmitter �Ѱ󶨵� ObiSolver");
                    return;
                }
                blueprintCopy.smoothing = smoothness;
                blueprintCopy.GenerateImmediate();
                ObiEmitter.emitterBlueprint = blueprintCopy;
                ObiEmitter.LoadBlueprint(solver);
                ObiFluidRenderingPass fluidPass = fluidSurfaceMesher.pass as ObiFluidRenderingPass;
                if (fluidPass != null)
                {
                    fluidPass.turbidity = color;
                    fluidPass.smoothness = smoothness;
                }
                else
                {
                    Debug.LogWarning("ObiFluidRenderingPass δ�ҵ����޷�������ɫ�� smoothness");
                }
            }
            else
            {
                Debug.LogWarning("ObiFluidSurfaceMesher �� ObiFluidEmitterBlueprint δ�ҵ�");
            }
        }
        else
        {
            Debug.LogError("ObiEmitter Ϊ�գ������Ƿ�����ȷ��ֵ");
        }
    }
}