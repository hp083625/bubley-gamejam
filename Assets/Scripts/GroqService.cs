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
    
    // Dictionary to store message history for each agent
    private Dictionary<string, List<Message>> agentMessageHistories = new Dictionary<string, List<Message>>();
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

    [System.Serializable]
    private class ObjectParameters
    {
        public string objectName;
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
    }

    public void InitializeAgent(string agentId, AgentPersonality personality)
    {
        if (!agentMessageHistories.ContainsKey(agentId))
        {
            var history = new List<Message>();
            history.Add(new Message("system", personality.GetSystemPrompt()));
            agentMessageHistories[agentId] = history;
            Debug.Log($"[GroqService] Initialized agent {agentId} with personality {personality.personalityName}");
        }
    }

    public void ClearHistory(string agentId, AgentPersonality personality)
    {
        if (agentMessageHistories.ContainsKey(agentId))
        {
            var history = new List<Message>();
            history.Add(new Message("system", personality.GetSystemPrompt()));
            agentMessageHistories[agentId] = history;
            Debug.Log($"[GroqService] Cleared history for agent {agentId}");
        }
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
        string agentId = agent.gameObject.GetInstanceID().ToString();
        Debug.Log($"[GroqService] Sending message to agent {agentId}: {userMessage}");
        
        // Initialize agent if not already done
        if (!agentMessageHistories.ContainsKey(agentId))
        {
            InitializeAgent(agentId, agent.personality);
        }

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
                },
                new Tool
                {
                    function = new Function
                    {
                        name = "move_to_object",
                        description = "Make the AI move to a specific object in the game",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                objectName = new
                                {
                                    type = "string",
                                    description = "The name of the object to move to"
                                }
                            },
                            required = new[] { "objectName" }
                        }
                    }
                }
            };

            // Add user message to history
            var messageHistory = agentMessageHistories[agentId];
            messageHistory.Add(new Message("user", userMessage));
            Debug.Log($"[GroqService] Added user message to history for agent {agentId}. Total messages: {messageHistory.Count}");

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

            var jsonRequest = SerializeRequest(request);
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
                        
                        if (functionName.Contains("toggle_follow"))
                        {
                            Debug.Log("[GroqService] Toggling follow mode");
                            agent.ToggleFollowMode();
                            
                            var chatUI = FindObjectOfType<ChatUI>();
                            if (chatUI != null)
                            {
                                chatUI.HideChat();
                            }
                        }
                        else if (functionName == "move_to_object")
                        {
                            var parameters = JsonConvert.DeserializeObject<ObjectParameters>(toolCall.function.arguments);
                            if (parameters != null && !string.IsNullOrEmpty(parameters.objectName))
                            {
                                Debug.Log($"[GroqService] Moving to object: {parameters.objectName}");
                                agent.MoveToObject(parameters.objectName);
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
    public List<(string role, string content)> GetLastMessages(string agentId, int count)
    {
        if (agentMessageHistories.ContainsKey(agentId))
        {
            var messages = new List<(string role, string content)>();
            var messageHistory = agentMessageHistories[agentId];
            int start = Mathf.Max(1, messageHistory.Count - count); // Skip system message
            
            for (int i = start; i < messageHistory.Count; i++)
            {
                var msg = messageHistory[i];
                if (msg.role != "system") // Skip system messages
                {
                    messages.Add((msg.role, msg.content));
                }
            }
            
            Debug.Log($"[GroqService] Retrieved last {messages.Count} messages for agent {agentId}");
            return messages;
        }
        else
        {
            Debug.LogError($"[GroqService] Agent {agentId} not found");
            return new List<(string role, string content)>();
        }
    }
}
