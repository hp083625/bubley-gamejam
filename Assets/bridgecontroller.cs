using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class bridgecontroller : MonoBehaviour
{
    [SerializeField] private GameObject[] gameObjects; // Array to store the game objects
    [SerializeField] private int activeObjectCount = 3; // Number of objects that should be active at once
    [SerializeField] private float toggleInterval = 2f; // Time interval between toggles in seconds

    private HashSet<int> activeIndices = new HashSet<int>();

    private void Start()
    {
        // Validate the active object count
        activeObjectCount = Mathf.Clamp(activeObjectCount, 0, gameObjects.Length);
        
        // Initially deactivate all objects
        foreach (GameObject obj in gameObjects)
        {
            if (obj != null)
                obj.SetActive(false);
        }

        // Start the toggle routine
        StartCoroutine(ToggleObjectsRoutine());
    }

    private IEnumerator ToggleObjectsRoutine()
    {
        while (true)
        {
            UpdateActiveObjects();
            yield return new WaitForSeconds(toggleInterval);
        }
    }

    private void UpdateActiveObjects()
    {
        // Deactivate all objects first
        for (int i = 0; i < gameObjects.Length; i++)
        {
            if (gameObjects[i] != null)
                gameObjects[i].SetActive(false);
        }

        // Clear the active indices
        activeIndices.Clear();

        // Randomly activate the required number of objects
        while (activeIndices.Count < activeObjectCount)
        {
            int randomIndex = Random.Range(0, gameObjects.Length);
            if (!activeIndices.Contains(randomIndex) && gameObjects[randomIndex] != null)
            {
                activeIndices.Add(randomIndex);
                gameObjects[randomIndex].SetActive(true);
            }
        }
    }
}
