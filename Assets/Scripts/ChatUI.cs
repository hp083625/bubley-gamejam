using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

public class ChatUI : MonoBehaviour
{
    [Header("UI Components")]
    public TMP_InputField chatInput;
    public float hideDistance = 5f;

    private AIAgent currentAI;
    public static bool IsTyping { get; private set; }
    private bool chatEnabled = false;
    private bool processingMessage = false;

    public bool IsEnabled => chatEnabled;

    void Start()
    {
        if (chatInput == null)
        {
            Debug.LogError("Chat Input field not assigned!");
            enabled = false;
            return;
        }

        DisableChat();
    }

    void Update()
    {
        if (chatEnabled && Input.GetKeyDown(KeyCode.Return) && !string.IsNullOrWhiteSpace(chatInput.text))
        {
            SendMessage(chatInput.text);
        }

        // Check distance
        if (chatEnabled && currentAI != null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                float distance = Vector3.Distance(player.transform.position, currentAI.transform.position);
                if (distance > hideDistance)
                {
                    HideChat();
                }
            }
        }
    }

    private void EnableChat()
    {
        Debug.Log("Enabling chat");
        chatEnabled = true;
        chatInput.gameObject.SetActive(true);
        chatInput.text = "";
        chatInput.ActivateInputField();
        IsTyping = true;
    }

    private void DisableChat()
    {
        Debug.Log("Disabling chat");
        chatEnabled = false;
        chatInput.gameObject.SetActive(false);
        chatInput.DeactivateInputField();
        IsTyping = false;
        currentAI = null;
        processingMessage = false;
    }

    public void ShowChat(AIAgent ai)
    {
        if (!chatEnabled)
        {
            currentAI = ai;
            EnableChat();
        }
    }

    public void HideChat()
    {
        if (chatEnabled)
        {
            DisableChat();
        }
    }

    private async void SendMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message) || currentAI == null || currentAI.groqService == null) 
            return;

        processingMessage = true;

        try 
        {
            // Clear input but keep it active
            chatInput.text = "";
            chatInput.ActivateInputField();

            // Show user message
            currentAI.SetBubbleText($"You: {message}");

            // Get AI response
            string response = await currentAI.groqService.SendMessage(message, currentAI);
            
            // Only show response if chat is still enabled
            if (chatEnabled)
            {
                currentAI.SetBubbleText(response);
                chatInput.ActivateInputField();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error sending message: {e.Message}");
            if (chatEnabled)
            {
                currentAI.SetBubbleText("Sorry, I had trouble responding.");
                chatInput.ActivateInputField();
            }
        }
        finally
        {
            processingMessage = false;
        }
    }
}
