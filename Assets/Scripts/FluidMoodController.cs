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
                new { role = "user", content = $"��������\"{mood}\"��������ģ��Ĳ������ϸ�ʹ�����¸�ʽ��skycolor: [R, G, B, A]& lightcolor: [R, G, B, A]& color: [R, G, B, A]& viscosity: [ճ����]& polarty: [����]& vorticity: [����ֵ]& lightintensity: [����ǿ��]�������ҽ����㼸��ʾ����Ϊ�ο�����mood��contentʱ����Ӧ����Ϊ skycolor: [0, 0.21, 0.6, 1.0]& lightcolor: [1.0, 0.96, 0.84, 1.0]& color: [0.015, 1.0, 0.79, 1.0]& viscosity: 0.05& polarity: 2& vorticity: 1.5& lightintensity: 1.05����mood��excitedʱ����Ӧ����Ϊ skycolor: [1, 0.21, 0.67, 1.0]& lightcolor: [1.0, 0.75, 0.73, 1.0]& color: [1.0, 0.53, 0, 1.0]& viscosity: 0.01& polarity: 0.2& vorticity: 0.6& lightintensity: 1.0����mood��calmʱ����Ӧ����Ϊ skycolor: [0.57, 0.41, 0.73, 1.0]& lightcolor: [1.0, 0.84, 0.95, 1.0]& color: [0, 0.65, 0.87, 1.0]& viscosity: 0.001& polarity: 0.5& vorticity: 0& lightintensity: 1.1����mood��afraidʱ����Ӧ����Ϊ skycolor: [0, 0, 0, 1.0]& lightcolor: [0.39, 0.39, 0.39, 1.0]& color: [0.02, 0.21, 0.38, 1.0]& viscosity: 0.001& polarity: 0.2& vorticity: 0.15& lightintensity: 0.1����mood��happyʱ����Ӧ����Ϊ skycolor: [1.0, 1.0, 0.9, 1.0]& lightcolor: [1.0, 0.8, 0.58, 1.0]& color: [1.0, 0.68, 0, 1.0]& viscosity: 0.001& polarity: 0.4& vorticity: 0.12& lightintensity: 1.21����mood��sadʱ����Ӧ����Ϊ skycolor: [0, 0.1, 0.43, 1.0]& lightcolor: [0.66, 0.66, 0.66, 1.0]& color: [0.02, 0, 0.55, 1.0]& viscosity: 0.01& polarity: 0.3& vorticity: 0.2& lightintensity: 1����mood��depressedʱ����Ӧ����Ϊ skycolor: [0, 0.19, 0.09, 1.0]& lightcolor: [0.3, 0.44, 0.44, 1.0]& color: [0, 0.56, 0.46, 1.0]& viscosity: 0& polarity: 0.5& vorticity: 0.04& lightintensity: 0.9��R, G, B, A ������ 0.0 �� 1.0 �ĸ�������color ��������������ĸ�ֵ������֮����&�ӿո�& ���ָ���lightintensity��Ҫ�Ӿ�ţ�ȷ����������Ҹ�ʽ��ȷ���������ʾ������mood������Ҫ��ȫ���Ҹ���ʾ���������� ��ϸ΢�ı仯��ʾ�������ο���" }
            },
            max_tokens = 400 // ���� 200��ȷ���㹻����������Ӧ
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
        Color skyColor = Color.white;
        
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
        if (Camera != null)
        {
            // ȷ������� Clear Flags ����Ϊ Skybox
            if (Camera.clearFlags != CameraClearFlags.Skybox)
            {
                Debug.LogWarning($"Camera �� Clear Flags ���� Skybox����ǰΪ {Camera.clearFlags}�������Զ�����Ϊ Skybox");
                Camera.clearFlags = CameraClearFlags.Skybox;
            }

            var skybox = Camera.GetComponent<Skybox>();
            if (skybox != null)
            {
                // ��ȡ Scene Skybox ���ʣ�ֱ�Ӵ� Resources ��Ԥ���أ�ȷ��ʹ����ȷ�Ĳ��ʣ�
                Material sceneSkyboxMaterial = Resources.Load<Material>("Scene Skybox");
                if (sceneSkyboxMaterial == null)
                {
                    Debug.LogWarning("δ�ҵ���Ϊ 'Scene Skybox' �Ĳ��ʣ�������һ���µ� Skybox/Procedural ����");
                    sceneSkyboxMaterial = new Material(Shader.Find("Skybox/Procedural"));
                }

                // ȷ������ʹ�� Skybox/Procedural ��ɫ��
                if (sceneSkyboxMaterial.shader.name != "Skybox/Procedural")
                {
                    Debug.LogWarning($"Scene Skybox ���ʵ���ɫ������ Skybox/Procedural����ǰΪ {sceneSkyboxMaterial.shader.name}���������·�����ɫ��");
                    sceneSkyboxMaterial.shader = Shader.Find("Skybox/Procedural");
                }

                // �������Ƿ��� _Tint ����
                if (sceneSkyboxMaterial.HasProperty("_SkyTint"))
                {
                    sceneSkyboxMaterial.SetColor("_SkyTint", skycolor);
                    Debug.Log($"�ѽ� Scene Skybox ���ʵ� Sky Tint ����Ϊ: {skycolor}");
                }
                else
                {
                    Debug.LogWarning($"Scene Skybox ����û�� _Tint ���ԣ���ǰ��ɫ��: {sceneSkyboxMaterial.shader.name}");
                }

                // �����ʷ���� Skybox ����� RenderSettings
                skybox.material = sceneSkyboxMaterial;
                RenderSettings.skybox = sceneSkyboxMaterial;
                Debug.Log("�ѽ� Scene Skybox ���ʷ���� Skybox ����� RenderSettings");
            }
            else
            {
                Debug.LogWarning("Camera ��û�� Skybox ��������Զ���� Skybox ���");
                skybox = Camera.gameObject.AddComponent<Skybox>();
                Material defaultSkyboxMaterial = new Material(Shader.Find("Skybox/Procedural"));
                if (defaultSkyboxMaterial.HasProperty("_Tint"))
                {
                    defaultSkyboxMaterial.SetColor("_Tint", skycolor);
                    Debug.Log($"�ѽ��´����� Skybox �� Sky Tint ����Ϊ: {skycolor}");
                    skybox.material = defaultSkyboxMaterial;
                    RenderSettings.skybox = defaultSkyboxMaterial;
                }
                else
                {
                    Debug.LogWarning($"�´����� Skybox ����û�� _Tint ���ԣ���ǰ��ɫ��: {defaultSkyboxMaterial.shader.name}");
                }
            }
        }
        else
        {
            Debug.LogWarning("Camera Ϊ�գ����� Inspector �и�ֵ");
        }
    }
}