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
                new { role = "user", content = $"��������\"{mood}\"��������ģ��Ĳ������ϸ�ʹ�����¸�ʽ��lightcolor: [R, G, B, A]& color: [R, G, B, A]& smoothness: [ƽ��ֵ]& viscosity: [ճ����]& polarty: [����]& vorticity: [����ֵ]& lightintensity: [����ǿ��]������ lightcolor: [1.0, 0.84, 0.95, 1.0]& color: [0.2, 0.4, 0.6, 1.0]& smoothness: 0.8& viscosity: 0.05& polarity: 0.5& vorticity: 0.5& lightintensity: 1.1��R, G, B, A ������ 0.0 �� 1.0 �ĸ�������color ��������������ĸ�ֵ������֮����&�ӿո�& ���ָ���lightintensity��Ҫ�Ӿ�ţ�ȷ����������Ҹ�ʽ��ȷ��" }
            },
            max_tokens = 200 // ���� 200��ȷ���㹻����������Ӧ
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
                Debug.Log("������ responseText: " + responseText); // ���ԣ���¼ responseText
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
        Color lightColor = Color.white;
        float smoothness = 0.88f; // Ĭ��ƽ��ֵ
        float vorticity = 0;
        float polarity = 0.2f;
        float viscosity = 0.02f;
        float lightintensity = 1.0f;
        // �� ", " �ָ� color �� smoothness ����
        string[] parameters = responseText.Split(new[] { "& " }, StringSplitOptions.RemoveEmptyEntries);
        Debug.Log("�ָ��Ĳ���: " + string.Join(" | ", parameters)); // ���ԣ���¼�ָ���

        foreach (var param in parameters)
        {
            // ���������ȥ������Ŀո�;��
            string cleanedParam = param.Trim().TrimEnd('.');
            string[] kv = cleanedParam.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            if (kv.Length == 2)
            {
                string key = kv[0].Trim().ToLower();
                string value = kv[1].Trim();
                if (key == "color")
                {
                    // ��ȡ [ �� ] ֮��� RGBA ֵ
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
                else if (key == "smoothness")
                {
                    if (!float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsedSmoothness))
                    {
                        Debug.LogError($"�޷�����ƽ��ֵ: {value}");
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
                        Debug.LogError($"�޷���������ֵ: {value}");
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
                        Debug.LogError($"�޷�����ճ����ֵ: {value}");
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
                        Debug.LogError($"�޷���������ֵ: {value}");
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
                        Debug.LogError($"�޷���������ǿ��: {value}");
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
                                Debug.LogError($"�޷�����light RGBA ֵ: {value}, ����: {ex.Message}");
                            }
                        }
                        else
                        {
                            Debug.LogError($"lightRGBA ��ʽ�������� 4 ��ֵ��ʵ�ʵõ� {rgbaValues.Length} ��ֵ: {value}");
                        }
                    }
                    else
                    {
                        Debug.LogError($"�޷��ҵ�light RGBA ���������������: {value}");
                    }
                }
            }
            else
            {
                // �������ֵ���� "0.9"��
                Debug.LogWarning($"������ʽ����: {cleanedParam}������ key:value ��ʽ��ʵ�ʵõ� {kv.Length} ����");
                // ������Ϊ smoothness ���������߼���
                if (float.TryParse(cleanedParam, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsedSmoothness))
                {
                    smoothness = parsedSmoothness;
                    Debug.Log($"������ֵ {cleanedParam} ����Ϊ smoothness: {smoothness}");
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
                    Debug.LogError("ObiSolver δ�ҵ�����ȷ�� ObiEmitter �Ѱ󶨵� ObiSolver");
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
        if (Light != null)
        {
            var directionalLight = Light.GetComponent<Light>();
            directionalLight.intensity=lightintensity;
            directionalLight.color = lightcolor;
        }
    }
}