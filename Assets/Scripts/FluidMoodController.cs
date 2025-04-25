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
    public Light Light;
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
                new { role = "user", content = $"根据心情\"{mood}\"返回流体模拟的参数，严格使用以下格式：lightcolor: [R, G, B, A]& color: [R, G, B, A]& smoothness: [平滑值]& viscosity: [粘滞性]& polarty: [极性]& vorticity: [涡旋值]& lightintensity: [光照强度]，例如 lightcolor: [1.0, 0.84, 0.95, 1.0]& color: [0.2, 0.4, 0.6, 1.0]& smoothness: 0.8& viscosity: 0.05& polarity: 0.5& vorticity: 0.5& lightintensity: 1.1。R, G, B, A 必须是 0.0 到 1.0 的浮点数，color 必须包含完整的四个值，参数之间用&加空格（& ）分隔，lightintensity后不要加句号，确保输出完整且格式正确。" }
            },
            max_tokens = 200 // 保持 200，确保足够生成完整响应
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
                Debug.Log("解析的 responseText: " + responseText); // 调试：记录 responseText
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
        Color lightColor = Color.white;
        float smoothness = 0.88f; // 默认平滑值
        float vorticity = 0;
        float polarity = 0.2f;
        float viscosity = 0.02f;
        float lightintensity = 1.0f;
        // 按 ", " 分割 color 和 smoothness 参数
        string[] parameters = responseText.Split(new[] { "& " }, StringSplitOptions.RemoveEmptyEntries);
        Debug.Log("分割后的参数: " + string.Join(" | ", parameters)); // 调试：记录分割结果

        foreach (var param in parameters)
        {
            // 清理参数，去除多余的空格和句号
            string cleanedParam = param.Trim().TrimEnd('.');
            string[] kv = cleanedParam.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            if (kv.Length == 2)
            {
                string key = kv[0].Trim().ToLower();
                string value = kv[1].Trim();
                if (key == "color")
                {
                    // 提取 [ 和 ] 之间的 RGBA 值
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
                else if (key == "smoothness")
                {
                    if (!float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsedSmoothness))
                    {
                        Debug.LogError($"无法解析平滑值: {value}");
                    }
                    else
                    {
                        smoothness = parsedSmoothness;
                    }
                }
                else if (key == "polarity")
                {
                    if (!float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsedPolarity))
                    {
                        Debug.LogError($"无法解析极性值: {value}");
                    }
                    else
                    {
                        vorticity = parsedPolarity;
                    }
                }
                else if(key== "viscosity")
                {
                    if (!float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsedViscosity))
                    {
                        Debug.LogError($"无法解析粘滞性值: {value}");
                    }
                    else
                    {
                        viscosity = parsedViscosity;
                    }
                }
                else if (key == "vorticity")
                {
                    if (!float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsedVorticity))
                    {
                        Debug.LogError($"无法解析涡旋值: {value}");
                    }
                    else
                    {
                        vorticity = parsedVorticity;
                    }
                }
                else if (key == "lightintensity")
                {
                    if (!float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsedLightintensity))
                    {
                        Debug.LogError($"无法解析光照强度: {value}");
                    }
                    else
                    {
                        lightintensity = parsedLightintensity;
                    }
                }
                else if (key == "lightcolor")
                {
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
                                lightColor = new Color(r, g, b, a);
                            }
                            catch (FormatException ex)
                            {
                                Debug.LogError($"无法解析light RGBA 值: {value}, 错误: {ex.Message}");
                            }
                        }
                        else
                        {
                            Debug.LogError($"lightRGBA 格式错误，期望 4 个值，实际得到 {rgbaValues.Length} 个值: {value}");
                        }
                    }
                    else
                    {
                        Debug.LogError($"无法找到light RGBA 数组的完整方括号: {value}");
                    }
                }
            }
            else
            {
                // 处理孤立值（如 "0.9"）
                Debug.LogWarning($"参数格式错误: {cleanedParam}，期望 key:value 格式，实际得到 {kv.Length} 部分");
                // 尝试作为 smoothness 处理（备用逻辑）
                if (float.TryParse(cleanedParam, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsedSmoothness))
                {
                    smoothness = parsedSmoothness;
                    Debug.Log($"将孤立值 {cleanedParam} 解析为 smoothness: {smoothness}");
                }
            }
        }
        ApplyFluidParameters(lightColor,fluidColor, smoothness, polarity, vorticity, viscosity,lightintensity);
    }

    private void ApplyFluidParameters(Color lightcolor,Color color, float smoothness, float polarity,float vorticity,float viscosity,float lightintensity)
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
                blueprintCopy.vorticity = vorticity;
                blueprintCopy.polarity = polarity;
                blueprintCopy.viscosity = viscosity;
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
        if (Light != null)
        {
            var directionalLight = Light.GetComponent<Light>();
            directionalLight.intensity=lightintensity;
            directionalLight.color = lightcolor;
        }
    }
}