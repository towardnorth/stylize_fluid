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
                new { role = "user",content = $"根据心情\"{mood}\"返回流体模拟的参数，请用以下格式：color: [颜色], smoothness: [平滑值]，例如 color: red, smoothness: 0.8。" }
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
        if (request.result == UnityWebRequest.Result.Success)
        {
            // 输出原始响应内容，用于调试
            Debug.Log("API 响应内容: " + request.downloadHandler.text);

            // 使用 ApiResponse 解析 JSON
            ApiResponse response = JsonConvert.DeserializeObject<ApiResponse>(request.downloadHandler.text);

            // 检查 response 和 choices 是否为空
            if (response?.choices != null && response.choices.Count > 0)
            {
                string responseText = response.choices[0].message.content;
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
                        Debug.LogError($"无法解析颜色值: {value}");
                    }
                }
                else if (key == "smoothness")
                {
                    if (!float.TryParse(value, out float parsedSmoothness))
                    {
                        Debug.LogError($"无法解析速度值: {value}");
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
            
            ObiFluidEmitterBlueprint obiFluidEmitterBlueprint=ObiEmitter.blueprint as ObiFluidEmitterBlueprint;

            if (fluidSurfaceMesher != null && obiFluidEmitterBlueprint != null)
            {
                ObiSolver solver = ObiEmitter.GetComponentInParent<ObiSolver>();

                // 1. 创建 Blueprint 的运行时副本（可选，视需求而定）
                ObiFluidEmitterBlueprint blueprintCopy = Instantiate(obiFluidEmitterBlueprint);
                if (solver == null)
                {
                    Debug.LogError("ObiSolver 未找到，请确保 ObiEmitter 已绑定到 ObiSolver");
                    return;
                }
                // 2. 修改 Blueprint 的 smoothing 属性
                blueprintCopy.smoothing = smoothness;
                
                // 3. 同步调用 Generate() 重新生成 Blueprint 数据
                blueprintCopy.GenerateImmediate();
                
                // 4. 重新挂载 Blueprint 到 ObiEmitter
                ObiEmitter.emitterBlueprint = blueprintCopy;
                
                ObiEmitter.LoadBlueprint(solver);
                
                // 5. 获取并更新 ObiFluidRenderingPass
                ObiFluidRenderingPass fluidPass = fluidSurfaceMesher.pass as ObiFluidRenderingPass;
                if (fluidPass != null)
                {
                    // 设置颜色和 smoothness
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
