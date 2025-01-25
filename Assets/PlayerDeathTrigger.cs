using UnityEngine;

public class PlayerDeathTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Trigger entered by: {other.gameObject.name} with tag: {other.tag}");
        
        // Check if the colliding object is the player
        if (other.CompareTag("Player") || other.gameObject.GetComponent<playerbhevior>() != null)
        {
            Debug.Log("Player detected - destroying");
            Destroy(other.gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"Collision with: {collision.gameObject.name} with tag: {collision.gameObject.tag}");
        
        // Check if the colliding object is the player
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.GetComponent<playerbhevior>() != null)
        {
            Debug.Log("Player detected - destroying");
            Destroy(collision.gameObject);
        }
    }
}
