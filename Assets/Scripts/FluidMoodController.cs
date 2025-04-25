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
                new { role = "user", content = $"根据心情\"{mood}\"返回流体模拟的参数，严格使用以下格式：color: [R, G, B, A], smoothness: [平滑值]，例如 color: [0.2, 0.4, 0.6, 1.0], smoothness: 0.8。R, G, B, A 必须是 0.0 到 1.0 的浮点数，color 必须包含完整的四个值，参数之间用逗号加空格（, ）分隔，smoothness 后不要加句号，确保输出完整且格式正确。" }
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
            Debug.Log("API 响应内容: " + request.downloadHandler.text);
            ApiResponse response = JsonConvert.DeserializeObject<ApiResponse>(request.downloadHandler.text);
            if (response?.choices != null && response.choices.Count > 0)
            {
                string responseText = response.choices[0].message.content;
                Debug.Log("解析的 responseText: " + responseText);
                ParseMoodParameters(responseText);
            }
            else
            {
                Debug.LogError("API 响应中没有 choices 或 choices 为空。响应内容: " + request.downloadHandler.text);
            }
        }
        else
        {
            Debug.LogError("请求失败: " + request.error);
        }
    }

    private void ParseMoodParameters(string responseText)
    {
        if (string.IsNullOrEmpty(responseText))
        {
            Debug.LogError("responseText 为空或未定义，无法解析参数。");
            return;
        }
        Color fluidColor = Color.white; // 默认颜色
        float smoothness = 0.88f; // 默认平滑值

        // 按 ", smoothness:" 分割，分为 color 和 smoothness 部分
        string[] parts = responseText.Split(new[] { ", smoothness:" }, StringSplitOptions.RemoveEmptyEntries);
        Debug.Log("按 ', smoothness:' 分割后的部分: " + string.Join(" | ", parts));

        if (parts.Length != 2)
        {
            Debug.LogError($"期望 2 个部分（color 和 smoothness），实际得到 {parts.Length} 个部分: {responseText}");
            return;
        }

        // 解析 color 部分
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
                        Debug.LogError($"无法解析 RGBA 值: {value}, 错误: {ex.Message}");
                    }
                }
                else
                {
                    Debug.LogError($"RGBA 格式错误，期望 4 个值，实际得到 {rgbaValues.Length} 个值: {value}");
                }
            }
            else
            {
                Debug.LogError($"无法找到 RGBA 数组的完整方括号: {value}");
            }
        }
        else
        {
            Debug.LogError($"color 参数格式错误: {colorPart}");
        }

        // 解析 smoothness 部分
        string smoothnessPart = parts[1].Trim();
        if (!float.TryParse(smoothnessPart, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsedSmoothness))
        {
            Debug.LogError($"无法解析平滑值: {smoothnessPart}");
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
                    Debug.LogError("ObiSolver 未找到，请确保 ObiEmitter 已绑定到 ObiSolver");
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
                    Debug.LogWarning("ObiFluidRenderingPass 未找到，无法设置颜色和 smoothness");
                }
            }
            else
            {
                Debug.LogWarning("ObiFluidSurfaceMesher 或 ObiFluidEmitterBlueprint 未找到");
            }
        }
        else
        {
            Debug.LogError("ObiEmitter 为空，请检查是否已正确赋值");
        }
    }
}