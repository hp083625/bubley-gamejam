using UnityEngine;
using System;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
#if UNITY_EDITOR || !ENABLE_IL2CPP
using Newtonsoft.Json;
#endif

public class GroqService : MonoBehaviour
{
    private const string GROQ_API_URL = "https://api.groq.com/openai/v1/chat/completions";
    private string apiKey;
    private readonly HttpClient client = new HttpClient();
    private List<Message> messageHistory = new List<Message>();
    private const int MAX_HISTORY = 10;

    [System.Serializable]
    private class Message
    {
        public string role;
        public string content;
        public string tool_call_id;
        public string name;
        public ToolCall[] tool_calls;

        // Constructor for regular messages
        public Message(string role, string content)
        {
            this.role = role;
            this.content = content;
        }

        // Constructor for function responses
        public Message(string role, string content, string toolCallId, string name)
        {
            this.role = role;
            this.content = content;
            this.tool_call_id = toolCallId;
            this.name = name;
        }

        // Default constructor for JSON deserialization
        public Message() { }
    }

    [System.Serializable]
    private class Tool
    {
        public string type = "function";
        public Function function;
    }

    [System.Serializable]
    private class Function
    {
        public string name;
        public string description;
        public object parameters;
    }

    [System.Serializable]
    private class ToolCall
    {
        public string id;
        public string type;
        public FunctionCall function;
    }

    [System.Serializable]
    private class FunctionCall
    {
        public string name;
        public string arguments;
    }

    [System.Serializable]
    private class ChatRequest
    {
        public string model = "llama-3.3-70b-versatile";
        public List<Message> messages;
        public List<Tool> tools;
        public string tool_choice = "auto";
        public float temperature = 0.7f;
        public int max_tokens = 1024;
    }

    [System.Serializable]
    private class ChatResponse
    {
        public Choice[] choices;
    }

    [System.Serializable]
    private class Choice
    {
        public Message message;
        public string finish_reason;
    }

    void Start()
    {
        apiKey = "";
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("[GroqService] API key is missing!");
            enabled = false;
            return;
        }

        Debug.Log("[GroqService] Initializing with API key length: " + apiKey.Length);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        // Add system message to history
        messageHistory.Add(new Message("system", 
            "You are a friendly AI assistant in a game. You're eager to help and follow the player. " +
            "Use the toggle_follow function when the player asks you to follow. " +
            "Keep responses short (under 50 characters) and cheerful. " +
            "Show enthusiasm for all requests and interactions."
        ));
        Debug.Log("[GroqService] System message added to history");
    }

    public void ClearHistory()
    {
        messageHistory.Clear();
        // Re-add system message
        messageHistory.Add(new Message("system", 
            "You are a friendly AI assistant in a game. You're eager to help and follow the player. " +
            "Use the toggle_follow function when the player asks you to follow. " +
            "Keep responses short (under 50 characters) and cheerful. " +
            "Show enthusiasm for all requests and interactions."
        ));
        Debug.Log("[GroqService] History cleared and system message re-added");
    }

    private string SerializeRequest(ChatRequest request)
    {
        try
        {
#if UNITY_EDITOR || !ENABLE_IL2CPP
            return JsonConvert.SerializeObject(request, new JsonSerializerSettings 
            { 
                NullValueHandling = NullValueHandling.Ignore 
            });
#else
            return JsonUtility.ToJson(request);
#endif
        }
        catch (Exception e)
        {
            Debug.LogError($"[GroqService] Error serializing request: {e.Message}");
            return JsonUtility.ToJson(request);
        }
    }

    private ChatResponse DeserializeResponse(string json)
    {
        try
        {
#if UNITY_EDITOR || !ENABLE_IL2CPP
            return JsonConvert.DeserializeObject<ChatResponse>(json);
#else
            return JsonUtility.FromJson<ChatResponse>(json);
#endif
        }
        catch (Exception e)
        {
            Debug.LogError($"[GroqService] Error deserializing response: {e.Message}");
            return JsonUtility.FromJson<ChatResponse>(json);
        }
    }

    public async Task<string> SendMessage(string userMessage, AIAgent agent)
    {
        Debug.Log($"[GroqService] Sending message: {userMessage}");
        try
        {
            var tools = new List<Tool>
            {
                new Tool
                {
                    function = new Function
                    {
                        name = "toggle_follow",
                        description = "Toggle the AI's follow mode on/off",
                        parameters = new { type = "object", properties = new { } }
                    }
                }
            };

            // Add user message to history
            messageHistory.Add(new Message("user", userMessage));
            Debug.Log($"[GroqService] Added user message to history. Total messages: {messageHistory.Count}");

            // Trim history if too long
            while (messageHistory.Count > MAX_HISTORY)
            {
                if (messageHistory[1].role != "system")
                {
                    messageHistory.RemoveAt(1);
                }
            }

            var request = new ChatRequest
            {
                messages = messageHistory,
                tools = tools,
                tool_choice = "auto",
                temperature = 1f,
                max_tokens = 1024
            };

            var jsonRequest = JsonConvert.SerializeObject(request, new JsonSerializerSettings 
            { 
                NullValueHandling = NullValueHandling.Ignore 
            });
            Debug.Log($"[GroqService] Sending request to Groq API: {jsonRequest}");

            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            Debug.Log("[GroqService] Making API call...");

            var response = await client.PostAsync(GROQ_API_URL, content);
            var jsonResponse = await response.Content.ReadAsStringAsync();
            
            Debug.Log($"[GroqService] Received response: {jsonResponse}");
            Debug.Log($"[GroqService] Response status code: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                Debug.LogError($"[GroqService] API call failed with status {response.StatusCode}: {jsonResponse}");
                return "Sorry, I'm having trouble connecting to my brain.";
            }

            var chatResponse = DeserializeResponse(jsonResponse);
            Debug.Log($"[GroqService] Deserialized response. Has choices: {chatResponse?.choices != null}");

            if (chatResponse?.choices != null && chatResponse.choices.Length > 0)
            {
                var choice = chatResponse.choices[0];
                var message = choice.message;
                Debug.Log($"[GroqService] Got message from choice: {message?.content}");

                // Add assistant's response to history
                messageHistory.Add(message);

                // Check if there are any tool calls
                if (message.tool_calls != null && message.tool_calls.Length > 0)
                {
                    Debug.Log("[GroqService] Processing tool calls...");
                    foreach (var toolCall in message.tool_calls)
                    {
                        string functionName = toolCall.function.name;
                        Debug.Log($"[GroqService] Tool call: {functionName}");
                        
                        // Handle both formats: <function=toggle_follow> and toggle_follow
                        if (functionName.Contains("toggle_follow") || 
                            functionName.Contains("=toggle_follow"))
                        {
                            Debug.Log("[GroqService] Toggling follow mode");
                            agent.ToggleFollowMode();
                            
                            // Find and disable ChatUI
                            var chatUI = FindObjectOfType<ChatUI>();
                            if (chatUI != null)
                            {
                                chatUI.HideChat();
                            }
                        }
                    }
                }

                return message.content;
            }

            Debug.LogWarning("[GroqService] No valid response in choices");
            return "I didn't understand that.";
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GroqService] Error in SendMessage: {e.Message}\nStack trace: {e.StackTrace}");
            return "Sorry, I'm having trouble responding right now.";
        }
    }

    // Get the last N messages for display
    public List<(string role, string content)> GetLastMessages(int count)
    {
        var messages = new List<(string role, string content)>();
        int start = Mathf.Max(1, messageHistory.Count - count); // Skip system message
        
        for (int i = start; i < messageHistory.Count; i++)
        {
            var msg = messageHistory[i];
            if (msg.role != "system") // Skip system messages
            {
                messages.Add((msg.role, msg.content));
            }
        }
        
        Debug.Log($"[GroqService] Retrieved last {messages.Count} messages");
        return messages;
    }
}
