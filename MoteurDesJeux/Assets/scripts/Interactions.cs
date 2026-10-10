using JetBrains.Annotations;
using UnityEngine;

public class Interactions : MonoBehaviour
{
    AudioSource source;

    public AudioClip clip;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnTriggerEnter2D(Collider2D collision)
    {
        
       
        
        if (collision.tag == "Star")
        {
            Debug.Log("Player entered the trigger area.");
            // Add your interaction logic here
            Destroy(collision.gameObject); // Example: Destroy the star object
            source.PlayOneShot(clip);

        }
    }
    void Start()
    {
        source = GetComponent<AudioSource>();
    }
}
