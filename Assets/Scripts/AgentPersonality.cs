using UnityEngine;

[CreateAssetMenu(fileName = "New Agent Personality", menuName = "AI/Agent Personality")]
public class AgentPersonality : ScriptableObject
{
    [Header("Basic Settings")]
    public string personalityName = "Default";
    
    [Header("Appearance")]
    public Color bubbleColor = Color.white;
    
    [Header("Personality")]
    [TextArea(3, 5)]
    public string greeting = "Hello there!";
    
    [TextArea(5, 8)]
    [Tooltip("The base personality and behavior description")]
    public string personalityDescription = "A helpful AI assistant.";
    
    [Header("Behavior Settings")]
    [Range(0f, 1f)]
    public float helpfulness = 0.7f;
    [Range(0f, 1f)]
    public float enthusiasm = 0.7f;
    
    public string GetSystemPrompt()
    {
        string enthusiasm_level = enthusiasm > 0.7f ? "very enthusiastic" : 
                                enthusiasm > 0.4f ? "moderately enthusiastic" : 
                                "reluctant";
        
        string helpfulness_level = helpfulness > 0.7f ? "eager to help" : 
                                 helpfulness > 0.4f ? "willing to help" : 
                                 "hesitant to help";
        
        return $"You are {enthusiasm_level} and {helpfulness_level} AI assistant in a game. " +
               $"{personalityDescription} " +
               "Use the toggle_follow function when the player asks you to follow. " +
               "Use the move_to_object function when asked to go to a specific object. " +
               "Keep responses short (under 50 characters) and match your personality. " +
               $"Your first greeting should be: {greeting}";
    }
}
