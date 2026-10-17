using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

public class Interactions : MonoBehaviour
{
    AudioSource source;

    public AudioClip clip;

    int score;
    public Text scoretxt;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnTriggerEnter2D(Collider2D collision)
    {
        
       
        
        if (collision.tag == "Star")
        {
            Debug.Log("Player entered the trigger area.");
            // Add your interaction logic here
            Destroy(collision.gameObject); // Example: Destroy the star object
            score++;
            scoretxt.text = score.ToString();
            source.PlayOneShot(clip);

        }
    }
    void Start()
    {
        source = GetComponent<AudioSource>();
        score = 0;
    }

}
