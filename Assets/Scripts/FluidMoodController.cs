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
    public Camera Camera;
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
                new { role = "user", content = $"根据心情\"{mood}\"返回流体模拟的参数，严格使用以下格式：skycolor: [R, G, B, A]& lightcolor: [R, G, B, A]& color: [R, G, B, A]& viscosity: [粘滞性]& polarty: [极性]& vorticity: [涡旋值]& lightintensity: [光照强度]，下面我将给你几个示例作为参考。当mood是content时，对应参数为 skycolor: [0, 0.21, 0.6, 1.0]& lightcolor: [1.0, 0.96, 0.84, 1.0]& color: [0.015, 1.0, 0.79, 1.0]& viscosity: 0.05& polarity: 2& vorticity: 1.5& lightintensity: 1.05。当mood是excited时，对应参数为 skycolor: [1, 0.21, 0.67, 1.0]& lightcolor: [1.0, 0.75, 0.73, 1.0]& color: [1.0, 0.53, 0, 1.0]& viscosity: 0.01& polarity: 0.2& vorticity: 0.6& lightintensity: 1.0。当mood是calm时，对应参数为 skycolor: [0.57, 0.41, 0.73, 1.0]& lightcolor: [1.0, 0.84, 0.95, 1.0]& color: [0, 0.65, 0.87, 1.0]& viscosity: 0.001& polarity: 0.5& vorticity: 0& lightintensity: 1.1。当mood是afraid时，对应参数为 skycolor: [0, 0, 0, 1.0]& lightcolor: [0.39, 0.39, 0.39, 1.0]& color: [0.02, 0.21, 0.38, 1.0]& viscosity: 0.001& polarity: 0.2& vorticity: 0.15& lightintensity: 0.1。当mood是happy时，对应参数为 skycolor: [1.0, 1.0, 0.9, 1.0]& lightcolor: [1.0, 0.8, 0.58, 1.0]& color: [1.0, 0.68, 0, 1.0]& viscosity: 0.001& polarity: 0.4& vorticity: 0.12& lightintensity: 1.21。当mood是sad时，对应参数为 skycolor: [0, 0.1, 0.43, 1.0]& lightcolor: [0.66, 0.66, 0.66, 1.0]& color: [0.02, 0, 0.55, 1.0]& viscosity: 0.01& polarity: 0.3& vorticity: 0.2& lightintensity: 1。当mood是depressed时，对应参数为 skycolor: [0, 0.19, 0.09, 1.0]& lightcolor: [0.3, 0.44, 0.44, 1.0]& color: [0, 0.56, 0.46, 1.0]& viscosity: 0& polarity: 0.5& vorticity: 0.04& lightintensity: 0.9。R, G, B, A 必须是 0.0 到 1.0 的浮点数，color 必须包含完整的四个值，参数之间用&加空格（& ）分隔，lightintensity后不要加句号，确保输出完整且格式正确。如果输入示例给的mood，不需要完全按我给的示例来，可以 有细微的变化，示例仅作参考。" }
            },
            max_tokens = 400 // 保持 200，确保足够生成完整响应
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
        Color skyColor = Color.white;
        
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
                else if (key == "skycolor")
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
                                skyColor = new Color(r, g, b, a);
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
                
            }
        }
        ApplyFluidParameters(skyColor,lightColor,fluidColor,polarity, vorticity, viscosity,lightintensity);
    }

    private void ApplyFluidParameters(Color skycolor, Color lightcolor,Color color, float polarity,float vorticity,float viscosity,float lightintensity)
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
        if (Camera != null)
        {
            // 确保相机的 Clear Flags 设置为 Skybox
            if (Camera.clearFlags != CameraClearFlags.Skybox)
            {
                Debug.LogWarning($"Camera 的 Clear Flags 不是 Skybox（当前为 {Camera.clearFlags}），已自动设置为 Skybox");
                Camera.clearFlags = CameraClearFlags.Skybox;
            }

            var skybox = Camera.GetComponent<Skybox>();
            if (skybox != null)
            {
                // 获取 Scene Skybox 材质（直接从 Resources 或预加载，确保使用正确的材质）
                Material sceneSkyboxMaterial = Resources.Load<Material>("Scene Skybox");
                if (sceneSkyboxMaterial == null)
                {
                    Debug.LogWarning("未找到名为 'Scene Skybox' 的材质，将创建一个新的 Skybox/Procedural 材质");
                    sceneSkyboxMaterial = new Material(Shader.Find("Skybox/Procedural"));
                }

                // 确保材质使用 Skybox/Procedural 着色器
                if (sceneSkyboxMaterial.shader.name != "Skybox/Procedural")
                {
                    Debug.LogWarning($"Scene Skybox 材质的着色器不是 Skybox/Procedural（当前为 {sceneSkyboxMaterial.shader.name}），将重新分配着色器");
                    sceneSkyboxMaterial.shader = Shader.Find("Skybox/Procedural");
                }

                // 检查材质是否有 _Tint 属性
                if (sceneSkyboxMaterial.HasProperty("_SkyTint"))
                {
                    sceneSkyboxMaterial.SetColor("_SkyTint", skycolor);
                    Debug.Log($"已将 Scene Skybox 材质的 Sky Tint 设置为: {skycolor}");
                }
                else
                {
                    Debug.LogWarning($"Scene Skybox 材质没有 _Tint 属性，当前着色器: {sceneSkyboxMaterial.shader.name}");
                }

                // 将材质分配给 Skybox 组件和 RenderSettings
                skybox.material = sceneSkyboxMaterial;
                RenderSettings.skybox = sceneSkyboxMaterial;
                Debug.Log("已将 Scene Skybox 材质分配给 Skybox 组件和 RenderSettings");
            }
            else
            {
                Debug.LogWarning("Camera 上没有 Skybox 组件，已自动添加 Skybox 组件");
                skybox = Camera.gameObject.AddComponent<Skybox>();
                Material defaultSkyboxMaterial = new Material(Shader.Find("Skybox/Procedural"));
                if (defaultSkyboxMaterial.HasProperty("_Tint"))
                {
                    defaultSkyboxMaterial.SetColor("_Tint", skycolor);
                    Debug.Log($"已将新创建的 Skybox 的 Sky Tint 设置为: {skycolor}");
                    skybox.material = defaultSkyboxMaterial;
                    RenderSettings.skybox = defaultSkyboxMaterial;
                }
                else
                {
                    Debug.LogWarning($"新创建的 Skybox 材质没有 _Tint 属性，当前着色器: {defaultSkyboxMaterial.shader.name}");
                }
            }
        }
        else
        {
            Debug.LogWarning("Camera 为空，请在 Inspector 中赋值");
        }
    }
}