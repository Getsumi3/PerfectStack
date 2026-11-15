using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovingBlock : MonoBehaviour
{
    [Header("Movement")] 
    public float speed = 3f;
    public float leftLimit = -1.5f;
    public float rightLimit = 1.5f;

    [Header("Falling")] 
    public float fallGravity = 2f;

    
    public StackManager manager;
    
    private Rigidbody2D rb;
    private bool isDropped;
    private bool movingRight = true;

    private bool gameplayEnabled = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
    }

    private void FixedUpdate()
    {
        if (!gameplayEnabled) return;
        if(isDropped) return;
        
        float direction = movingRight ? 1f : -1f;
        
        Vector2 nextPosition = rb.position + Vector2.right * direction * speed * Time.fixedDeltaTime;
        
        if(nextPosition.x >= rightLimit)
        {
            nextPosition.x = rightLimit;
            movingRight = false;
        }
        else if(nextPosition.x <= leftLimit)
        {
            nextPosition.x = leftLimit;
            movingRight = true;
        }
        
        rb.MovePosition(nextPosition);
        
        if (isDropped && manager != null &&
            Camera.main != null &&
            rb.position.y < Camera.main.transform.position.y - 8f)
        {
            manager.GameOver();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!gameplayEnabled) return;
        if(isDropped) return;

        if (Input.GetMouseButtonDown(0)) DropBlock();
        
        if(Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) DropBlock();
    }

    void DropBlock()
    {
        isDropped = true;
        rb.gravityScale = fallGravity;
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isDropped) return;

        if (manager != null)
        {
            manager.LandBlock(this);
        }
        else
        {
            Debug.LogError("StackManager is missing!");
        }
    }
    
    public void SetGameplayEnabled(bool enabled)
    {
        gameplayEnabled = enabled;
    }
    
    public void SetMovingDirection(bool moveRight)
    {
        movingRight = moveRight;
    }
}