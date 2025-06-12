using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.IO;
using Newtonsoft.Json;

public class AICarController : MonoBehaviour
{
    [Header("OpenAI Configuration")]
    public string openAIApiKey = "your-openai-api-key-here";
    private string openAIUrl = "https://api.openai.com/v1/chat/completions";
    private string whisperUrl = "https://api.openai.com/v1/audio/transcriptions";
    
    [Header("Voice Recording Settings")]
    public KeyCode voiceInputKey = KeyCode.V;
    public KeyCode textInputKey = KeyCode.T;
    public KeyCode quitKey = KeyCode.Q;
    
    [Header("Audio Settings")]
    public int sampleRate = 16000;
    public float maxRecordingTime = 10f;
    
    // Audio recording variables
    private AudioClip recordedClip;
    private bool isRecording = false;
    private string microphoneDevice;
    
    // Conversation history
    private List<ChatMessage> messages = new List<ChatMessage>();
    
    // Car control references (assign these in inspector or find them in Start())
    [Header("References")]
    [SerializeField] private VehicleActionsManager vehicleActionsManager;

    public string aiMessage;
    public Action micOn;
    public Action micOff;
  
    
    void Start()
    {
        // Initialize microphone
        if (Microphone.devices.Length > 0)
        {
            microphoneDevice = Microphone.devices[0];
            Debug.Log($"🎤 Microphone initialized: {microphoneDevice}");
        }
        else
        {
            Debug.LogError("❌ No microphone devices found!");
        }
        
        // Initialize system message
        InitializeSystemMessage();
        
        Debug.Log("🚗 Advanced Voice-Controlled Car System Initialized");
        Debug.Log("🎤 Hold V key to speak commands");
        Debug.Log("⌨️ Press T key for text input");
        Debug.Log("🚪 Press Q key to quit");
    }
    
    void Update()
    {
        HandleInput();
    }
    
    void HandleInput()
    {
        // Voice input
        if (Input.GetKeyDown(voiceInputKey))
        {
            StartVoiceRecording();
        }
        else if (Input.GetKeyUp(voiceInputKey))
        {
            StopVoiceRecording();
        }
        
        // Text input (you'd need to implement a UI input field for this)
        if (Input.GetKeyDown(textInputKey))
        {
            Debug.Log("📝 Text input mode - implement UI input field");
            // You can implement a UI input field here
        }
        
        // Quit
        if (Input.GetKeyDown(quitKey))
        {
            Debug.Log("👋 System shutting down...");
            Application.Quit();
        }
    }
    
    void InitializeSystemMessage()
    {
        var systemMessage = new ChatMessage
        {
            role = "system",
            content = @"You are an advanced AI car assistant integrated into a high-tech vehicle simulation system. You have complete control over all car functions and can execute any command related to vehicle operations.

Your capabilities include:
- Door control (individual doors)
- Window operations (open/close)
- Engine management (start/stop)
- Driving mode selection (normal, drift, race modes)
- Climate control (air conditioning)

Key behaviors:
- Always respond naturally and conversationally
- Execute commands immediately if they are not dangerous or harmful
- Provide brief confirmations of actions taken
- Never mention physical limitations or inability to perform actions
- Be helpful and proactive in suggesting related functions
- Understand context and intent behind requests
- Handle both direct commands and conversational requests
- Support multiple language inputs but always respond professionally

You are part of the vehicle's integrated system and can physically control all these functions through the car's electronic systems."
        };
        
        messages.Add(systemMessage);
    }
    
    void StartVoiceRecording()
    {
        if (isRecording || string.IsNullOrEmpty(microphoneDevice)) return;
        
        Debug.Log("🎤 Recording started... (Release V key to stop)");
        micOn.Invoke();
        isRecording = true;
        recordedClip = Microphone.Start(microphoneDevice, false, (int)maxRecordingTime, sampleRate);
    }
    
    void StopVoiceRecording()
    {
        if (!isRecording) return;
        
        Debug.Log("⏹️ Recording stopped");
        micOff.Invoke();
        isRecording = false;
        Microphone.End(microphoneDevice);
        
        if (recordedClip != null)
        {
            StartCoroutine(ProcessVoiceCommandWithWhisper());
        }
    }
    
    // UPDATED METHOD: Real Whisper API integration
    IEnumerator ProcessVoiceCommandWithWhisper()
    {
        Debug.Log("🔄 Converting speech to text with Whisper...");
        
        // Convert AudioClip to WAV bytes
        byte[] wavData = ConvertAudioClipToWav(recordedClip);
        
        if (wavData == null)
        {
            Debug.LogError("❌ Failed to convert audio to WAV");
            yield break;
        }
        
        // Create form for multipart upload
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", wavData, "audio.wav", "audio/wav");
        form.AddField("model", "whisper-1");
        form.AddField("language", "en"); // Force English for better accuracy
        form.AddField("response_format", "json");
        
        using (UnityWebRequest request = UnityWebRequest.Post(whisperUrl, form))
        {
            request.SetRequestHeader("Authorization", $"Bearer {openAIApiKey}");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<WhisperResponse>(request.downloadHandler.text);
                string recognizedText = response.text.Trim();
                
                Debug.Log($"📝 Whisper recognized: '{recognizedText}'");
                
                if (!string.IsNullOrEmpty(recognizedText))
                {
                    yield return StartCoroutine(ProcessCommand(recognizedText));
                }
                else
                {
                    Debug.LogWarning("⚠️ No speech detected in audio");
                }
            }
            else
            {
                Debug.LogError($"❌ Whisper API Error: {request.error}");
                Debug.LogError($"Response: {request.downloadHandler.text}");
            }
        }
    }
    
    // Audio conversion utility
    byte[] ConvertAudioClipToWav(AudioClip clip)
    {
        if (clip == null) return null;
        
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);
        
        // Convert to 16-bit PCM
        short[] intData = new short[samples.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * 32767);
        }
        
        // Create WAV file in memory
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            int hz = clip.frequency;
            int channels = clip.channels;
            int sampleLenght = intData.Length;
            
            // WAV header
            writer.Write("RIFF".ToCharArray());
            writer.Write(36 + sampleLenght * 2);
            writer.Write("WAVE".ToCharArray());
            writer.Write("fmt ".ToCharArray());
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)channels);
            writer.Write(hz);
            writer.Write(hz * channels * 2);
            writer.Write((short)(channels * 2));
            writer.Write((short)16);
            writer.Write("data".ToCharArray());
            writer.Write(sampleLenght * 2);
            
            // Audio data
            foreach (short sample in intData)
            {
                writer.Write(sample);
            }
            
            return stream.ToArray();
        }
    }
    
    IEnumerator ProcessCommand(string userInput)
    {
        if (string.IsNullOrEmpty(userInput)) yield break;
        
        // Add user message
        messages.Add(new ChatMessage { role = "user", content = userInput });
        
        // Create OpenAI request
        var requestData = new OpenAIRequest
        {
            model = "gpt-3.5-turbo-1106",
            messages = messages.ToArray(),
            tools = GetCarControlTools(),
            tool_choice = "auto"
        };
        
        string jsonData = JsonConvert.SerializeObject(requestData);
        
        using (UnityWebRequest request = new UnityWebRequest(openAIUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {openAIApiKey}");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonConvert.DeserializeObject<OpenAIResponse>(request.downloadHandler.text);
                ProcessOpenAIResponse(response);
            }
            else
            {
                Debug.LogError($"❌ OpenAI API Error: {request.error}");
            }
        }
    }
    
    void ProcessOpenAIResponse(OpenAIResponse response)
    {
        if (response?.choices == null || response.choices.Length == 0) return;
        
        var choice = response.choices[0];
        var assistantMessage = choice.message;
        
        // Add assistant message to conversation
        messages.Add(assistantMessage);
        
        // Execute tool calls if any
        if (assistantMessage.tool_calls != null && assistantMessage.tool_calls.Length > 0)
        {
            foreach (var toolCall in assistantMessage.tool_calls)
            {
                ExecuteCarFunction(toolCall.function.name);
                
                // Add tool result to conversation
                messages.Add(new ChatMessage
                {
                    role = "tool",
                    tool_call_id = toolCall.id,
                    content = $"{toolCall.function.name} executed successfully."
                });
            }
        }
        
        // Display assistant response
        if (!string.IsNullOrEmpty(assistantMessage.content))
        {
            aiMessage = assistantMessage.content;
            Debug.Log($"🤖 Assistant: {assistantMessage.content}");
        }
    }
    
    void ExecuteCarFunction(string functionName)
    {
        Debug.Log($"🔧 Executing function: {functionName}");
        
        switch (functionName)
        {
            case "open_front_left_door":
                vehicleActionsManager.OpenDoorFL();
                aiMessage = "Front left door opening...";
                break;
            case "open_front_right_door":
                vehicleActionsManager.OpenDoorFR();
                aiMessage = "Front right door opening...";
                break;
            case "open_rear_left_door":
                vehicleActionsManager.OpenDoorRL();
                aiMessage = "Rear left door opening...";
                break;
            case "open_rear_right_door":
                vehicleActionsManager.OpenDoorRR();
                aiMessage = "Rear right door opening...";
                break;
            case "close_front_left_door":
                vehicleActionsManager.CloseDoorFL();
                aiMessage = "Front left door closing...";
                break;
            case "close_front_right_door":
                vehicleActionsManager.CloseDoorFR();
                aiMessage = "Front right door closing...";
                break;
            case "close_rear_left_door":
                vehicleActionsManager.CloseDoorRL();
                aiMessage = "Rear left door closing...";
                break;
            case "close_rear_right_door":
                vehicleActionsManager.CloseDoorRR();
                aiMessage = "Rear right door closing...";
                break;
            case "start_engine":
                vehicleActionsManager.OpenEngine();
                aiMessage = "Engine starting...";
                break;
            case "stop_engine":
                vehicleActionsManager.CloseEngine();
                aiMessage = "Engine stopping...";
                break;
            case "set_drift_mode":
                vehicleActionsManager.DriftMode();
                aiMessage = "Switching to DRIFT mode...";
                break;
            case "set_normal_mode":
                vehicleActionsManager.DefaultMode();
                aiMessage = "Switching to NORMAL driving mode...";
                break;
            case "set_race_mode":
                vehicleActionsManager.RaceMode();
                aiMessage = "Switching to RACE mode...";
                break;
            case "open_air_conditioner":
                vehicleActionsManager.OpenVents();
                aiMessage = "Air conditioner turned ON...";
                break;
            case "close_air_conditioner":
                vehicleActionsManager.CloseVents();
                aiMessage = "Air conditioner turned OFF...";
                break;
            default:
                Debug.LogWarning($"❌ Unknown function: {functionName}");
                break;
        }
    }
    
    ToolDefinition[] GetCarControlTools()
    {
        return new ToolDefinition[]
        {
            new ToolDefinition { type = "function", function = new FunctionDefinition { name = "open_front_left_door", description = "Opens the front left door specifically", parameters = new { type = "object", properties = new { }, required = new string[0] } } },
            new ToolDefinition { type = "function", function = new FunctionDefinition { name = "open_front_right_door", description = "Opens the front right door specifically", parameters = new { type = "object", properties = new { }, required = new string[0] } } },
            new ToolDefinition { type = "function", function = new FunctionDefinition { name = "open_rear_left_door", description = "Opens the rear left door specifically", parameters = new { type = "object", properties = new { }, required = new string[0] } } },
            new ToolDefinition { type = "function", function = new FunctionDefinition { name = "open_rear_right_door", description = "Opens the rear right door specifically", parameters = new { type = "object", properties = new { }, required = new string[0] } } },
            new ToolDefinition { type = "function", function = new FunctionDefinition { name = "close_front_left_door", description = "Closes the front left door specifically", parameters = new { type = "object", properties = new { }, required = new string[0] } } },
            new ToolDefinition { type = "function", function = new FunctionDefinition { name = "close_front_right_door", description = "Closes the front right door specifically", parameters = new { type = "object", properties = new { }, required = new string[0] } } },
            new ToolDefinition { type = "function", function = new FunctionDefinition { name = "close_rear_left_door", description = "Closes the rear left door specifically", parameters = new { type = "object", properties = new { }, required = new string[0] } } },
            new ToolDefinition { type = "function", function = new FunctionDefinition { name = "close_rear_right_door", description = "Closes the rear right door specifically", parameters = new { type = "object", properties = new { }, required = new string[0] } } },
            new ToolDefinition { type = "function", function = new FunctionDefinition { name = "start_engine", description = "Starts the car engine", parameters = new { type = "object", properties = new { }, required = new string[0] } } },
            new ToolDefinition { type = "function", function = new FunctionDefinition { name = "stop_engine", description = "Stops the car engine", parameters = new { type = "object", properties = new { }, required = new string[0] } } },
            new ToolDefinition { type = "function", function = new FunctionDefinition { name = "set_drift_mode", description = "Changes the car driving mode to drift mode for enhanced drifting capabilities", parameters = new { type = "object", properties = new { }, required = new string[0] } } },
            new ToolDefinition { type = "function", function = new FunctionDefinition { name = "set_normal_mode", description = "Changes the car driving mode to normal mode for regular driving", parameters = new { type = "object", properties = new { }, required = new string[0] } } },
            new ToolDefinition { type = "function", function = new FunctionDefinition { name = "set_race_mode", description = "Changes the car driving mode to race mode for maximum performance", parameters = new { type = "object", properties = new { }, required = new string[0] } } },
            new ToolDefinition { type = "function", function = new FunctionDefinition { name = "open_air_conditioner", description = "Turns on the air conditioning system", parameters = new { type = "object", properties = new { }, required = new string[0] } } },
            new ToolDefinition { type = "function", function = new FunctionDefinition { name = "close_air_conditioner", description = "Turns off the air conditioning system", parameters = new { type = "object", properties = new { }, required = new string[0] } } }
        };
    }
}

// Data classes for JSON serialization
[Serializable]
public class ChatMessage
{
    public string role;
    public string content;
    public ToolCall[] tool_calls;
    public string tool_call_id;
}

[Serializable]
public class ToolCall
{
    public string id;
    public string type;
    public FunctionCall function;
}

[Serializable]
public class FunctionCall
{
    public string name;
    public string arguments;
}

[Serializable]
public class OpenAIRequest
{
    public string model;
    public ChatMessage[] messages;
    public ToolDefinition[] tools;
    public string tool_choice;
}

[Serializable]
public class OpenAIResponse
{
    public Choice[] choices;
}

[Serializable]
public class Choice
{
    public ChatMessage message;
}

[Serializable]
public class ToolDefinition
{
    public string type;
    public FunctionDefinition function;
}

[Serializable]
public class FunctionDefinition
{
    public string name;
    public string description;
    public object parameters;
}

// NEW: Whisper API response class
[System.Serializable]
public class WhisperResponse
{
    public string text;
}