using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    float Hmove;
    Rigidbody2D rb;
    public float speed , Jforce;

    SpriteRenderer spr;
    Animator anim;

    bool jumping , isGrounded;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>(); 
        jumping = false;
        isGrounded = true;
    }

    // Update is called once per frame
    void Update()
    {
        Hmove = Input.GetAxis("Horizontal");
        fliPx();

    
        Debug.Log(Mathf.Abs(Hmove));
        anim.SetFloat("walk", Mathf.Abs(Hmove));

        if (Input.GetKeyDown(KeyCode.Space)  && isGrounded)
        {
            jumping = true;

        }
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = new Vector2( Hmove * speed, rb.linearVelocity.y);
        if (rb.linearVelocity.y==0) 
        {
            isGrounded = true;
        }
        if (jumping)
        {
            rb.AddForce(transform.up * Jforce);
            isGrounded = false;
            jumping = false;
            
        }
    }

    void fliPx()
    {
        if (Input.GetKey(KeyCode.A))
        {
            spr.flipX = true;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            spr.flipX = false;
        }
    }
}
