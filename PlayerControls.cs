using Unity.VisualScripting;
using UnityEngine;

public class PlayerControls : MonoBehaviour
{
    //Change movement values. Change in editor.
    [SerializeField] float jumpForce = 0;//ie Newtons. Jumping is handled by Unity's physics engine.
    [SerializeField] float jumpTime = 0;//Seconds. Time to jump peak
    [SerializeField] float climbYSpeed = 0;
    [SerializeField] float climbXSpeed = 0;

    //Not editable values.
    Transform playerTransform;
    Rigidbody2D playerRigidbody;
    Vector2 movementVector;
    GameObject interactee = null;
    SpriteRenderer playerSpriteRenderer;
    public enum playerState//Determines what inputs do / are valid
    {
        freePlay,//Any movement valid
        freeJumping,//Hold up to jump higher, preserve x momentum, fall after set time
        freeFalling,//Can't jump or interact, can climb and slow strafe
        freeInputClimbing,//Climb up/down, slow side ie up vines
        noInputClimbingInitial,//Locked in animation, no input ie small ledge
        noInputClimbingEnding,//2 States to control animation. Still no input
        interacting,//with level obj. No input until animation finishes
        simpleUI,//UI prompt needing one button press, game frozen and no other input
        mapUI,//In map, alternate controls

    }

    public playerState PlayerState;

    public struct playerInput//Used to keep player's inputs in one place during update across the switch, to prevent duplicated code.
    {
        public float xAxis;
        public bool up;
        public bool down;
        public bool interact;
    }

    //Info for player acceleration, allowing some fine control over acceleration/max speed
    [System.Serializable]
    public struct accelerationInfo
    {
        public float force;
        public float timeLength;
    }
    [SerializeField] accelerationInfo[] accelerationTable;
    float accelerationInfoTotalLength;
    float timeRunBegins;
    int currentWindow;
    float[] accelerationWindowLengths;
    bool isRunning = false;

    //Info for jumping
    float xInertia = 0;
    float jumpBeginTime = 0;

    //Info for free climbing
    playerState? climbState = null;
    int climbYDirection = 0;

    //Info for locked climbing
    float interacteeHeight = 0;
    int climbingAnimationElapsedFrames = 0;

    //Info for animation
    enum sprites
    {
        ClimbingFree0,
        ClimbingFree1,
        ClimbingLock0,
        ClimbingLock1,
        Interact0,
        Interact1,
        StandingIdle0,
        StandingIdle1,
        Walk0,
        Walk1,
        Run0,
        Run1
    }
    [SerializeField] Sprite[] spritesArray;
    int currentFrame = 0;
    bool animationFrame = true;//True meaning frame 0 and false frame 1. Every animation has 2 animation frames.

    //Info for interacting
    int interactInitialFrame;

    // Start is called before the first frame update
    void Start()
    {
        playerTransform = gameObject.transform;
        playerRigidbody = gameObject.GetComponent<Rigidbody2D>();
        playerSpriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        currentFrame = 0;

        //Acceleration data calculated from accerlerationTable, to save time later.
        accelerationInfoTotalLength = 0;
        foreach (accelerationInfo window in accelerationTable)
        {
            accelerationInfoTotalLength += window.timeLength;
        }
        accelerationWindowLengths = new float[accelerationTable.Length];
        float previousWindowsTime = 0;
        for (int i = 0; i < accelerationWindowLengths.Length; i++)
        {
            accelerationWindowLengths[i] = accelerationTable[i].timeLength + previousWindowsTime;
            previousWindowsTime += accelerationTable[i].timeLength;
        }
        currentWindow = 0;
    }

    // Update is called once per frame
    void Update()
    {

        //Save player input for later
        playerInput playerInput;
        playerInput.xAxis = Input.GetAxis("Horizontal");
        playerInput.up = Input.GetButton("Up");
        playerInput.down = Input.GetButton("Down");
        playerInput.interact = Input.GetButtonDown("Interact");

        //Frame-by-Frame effect of state
        switch (PlayerState)
        {
            case playerState.freePlay:// -- Free movement -- 

                //Check for interact
                if (playerInput.interact)
                {
                    Interact();
                    break;
                }

                //Fetch current acceleration force from accelerationInfo
                float currentForce = 0;
                if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
                {
                    timeRunBegins = Time.time;
                    currentWindow = 0;
                    isRunning = true;
                }

                else if (isRunning && Time.time - timeRunBegins > accelerationWindowLengths[currentWindow])
                {
                    currentWindow++;

                    if (currentWindow == accelerationTable.Length - 1)
                    {
                        isRunning = false;
                    }
                }

                currentForce = accelerationTable[currentWindow].force;

                //Check if jumping
                if (playerInput.up)
                {
                    //Check if climbing instead
                    climbState = AttemptClimb();
                    if (climbState != null)
                    {
                        PlayerState = climbState ?? PlayerState;
                        break;
                    }

                    PlayerState = playerState.freeJumping;
                    jumpBeginTime = Time.time;
                    if (currentForce == 0)
                    {
                        xInertia = accelerationTable[0].force * 0.5f;
                    }
                    else
                    {
                        xInertia = currentForce;
                    }
                    break;
                }

                //Check if falling
                if (playerRigidbody.velocity.y < -10f)
                {
                    if (currentForce == 0)
                    {
                        xInertia = accelerationTable[0].force * 0.5f;
                    }
                    else
                    {
                        xInertia = currentForce;
                    }
                    PlayerState = playerState.freeFalling;
                    break;
                }

                //Movement
                movementVector = new Vector2 (currentForce * playerInput.xAxis * Time.deltaTime, 0);
                playerRigidbody.AddForce(movementVector);
                break;

            case playerState.freeJumping:// -- Free jumping --
                //Check for jump end
                if (Input.GetKeyUp(KeyCode.UpArrow) || (Time.time - jumpBeginTime > jumpTime))
                {
                    PlayerState = playerState.freeFalling;
                    break;
                }

                //Check for climb
                climbState = AttemptClimb();
                if (climbState != null)
                {
                    PlayerState = climbState ?? PlayerState;
                    break;
                }

                //Movement
                movementVector = new Vector2(playerInput.xAxis * xInertia * Time.deltaTime, jumpForce * Time.deltaTime);
                playerRigidbody.AddForce(movementVector);
                break;

            case playerState.freeFalling:// -- Free falling --
                //Check for landing
                if (Mathf.Approximately(playerRigidbody.velocity.y, 0f))
                {
                    PlayerState = playerState.freePlay;
                    break;
                }

                //Check for climb
                if (Input.GetButton("Up"))
                {
                    climbState = AttemptClimb();
                    if (climbState != null)
                    {
                        PlayerState = climbState ?? PlayerState;
                        break;
                    }
                }

                //Falling handled by Unity's physics engine.
                //Movement
                movementVector = new Vector2(playerInput.xAxis * xInertia * Time.deltaTime, 0);
                playerRigidbody.AddForce(movementVector);
                break;

            case playerState.freeInputClimbing:// -- Free Input Climbing --

                //Check if left climbing surface
                if (interactee == null || interactee.tag != "FreeClimb")
                {
                    playerRigidbody.gravityScale = 5;
                    PlayerState = playerState.freeFalling;
                    break;
                }

                //Check if moved onto a LockClimb ie animation climb object
                if (interactee != null && interactee.tag == "LockClimb")
                {
                    interacteeHeight = interactee.GetComponent<SpriteRenderer>().bounds.size.y;
                    playerRigidbody.constraints = RigidbodyConstraints2D.FreezePositionX;
                    PlayerState = playerState.noInputClimbingInitial;
                    break;

                }

                climbYDirection = 0;
                if (playerInput.up) { climbYDirection += 1; }
                if (playerInput.down) { climbYDirection -= 1; }
                movementVector = new Vector2(climbXSpeed * playerInput.xAxis, climbYDirection * climbYSpeed);
                playerRigidbody.AddForce(movementVector);
                break;

            case playerState.noInputClimbingInitial:// ---No Input Climbing (2 States)---
                //Check reached top. Uses Y positions so changeover happens when character's hands/upperbody is over the top.
                if (playerTransform.position.y > interactee.transform.position.y)
                {
                    //Player is pushed up to avoid clipping issues. Hide with the sprite crouching and dust effects.
                    playerTransform.Translate(0, (interacteeHeight + playerSpriteRenderer.bounds.size.y) / 2f + 0.1f, 0);
                    playerRigidbody.gravityScale = 5;
                    climbingAnimationElapsedFrames = 0;
                    PlayerState = playerState.noInputClimbingEnding;
                    break;
                }

                //Move upward. Movement is scaled on climbing objects height and framerate to always take ~0.5 seconds.
                playerTransform.Translate(0, interacteeHeight * Time.deltaTime, 0);
                break;

            case playerState.noInputClimbingEnding:
                //Check if set time has passed (~0.5 seconds)
                climbingAnimationElapsedFrames++;
                if (climbingAnimationElapsedFrames > 0.5 / Time.deltaTime)
                {
                    PlayerState = playerState.freePlay;
                    playerRigidbody.constraints &= ~RigidbodyConstraints2D.FreezePositionX;

                    break;
                }

                break;

            case playerState.interacting:// ---Interacting---
                //No input, just wait one second and return
                //InteractInitialFrame is set by interact()
                if (currentFrame == interactInitialFrame)
                {
                    PlayerState = playerState.freePlay;
                }
                break;

            default:
                break;
        }

        //Handle animations -----
        //This could be included in previous switch. Seperated for readability.

        //Face current direction
        if (playerInput.xAxis < 0)
        { playerSpriteRenderer.flipX = true; }
        else
        {
            playerSpriteRenderer.flipX = false;
        }

        switch (PlayerState)
        {
            case playerState.freePlay:
                //Extra check for standing v walking v running
                if (Mathf.Approximately(playerRigidbody.velocity.x, 0f))
                {
                    //Is standing
                    if (animationFrame)
                    {
                        playerSpriteRenderer.sprite = spritesArray[(int)sprites.StandingIdle0];
                        break;
                    }
                    playerSpriteRenderer.sprite = spritesArray[(int)sprites.StandingIdle1];
                    break;
                }

                if (currentWindow == 0)
                {
                    //Is walking
                    if (animationFrame)
                    {
                        playerSpriteRenderer.sprite = spritesArray[(int)sprites.Walk0];
                        break;
                    }
                    playerSpriteRenderer.sprite = spritesArray[(int)sprites.Walk1];
                    break;
                }
                //Is running
                if (animationFrame)
                {
                    playerSpriteRenderer.sprite = spritesArray[(int)sprites.Run0];
                    break;
                }
                playerSpriteRenderer.sprite = spritesArray[(int)sprites.Run1];
                break;

            case playerState.freeJumping:
                playerSpriteRenderer.sprite = spritesArray[(int)sprites.Run1];//No unique jump animation ATM
                break;

            case playerState.freeFalling:
                playerSpriteRenderer.sprite = spritesArray[(int)sprites.Run1];//No unique falling animation ATM
                break;

            case playerState.freeInputClimbing:
                if (animationFrame)
                {
                    playerSpriteRenderer.sprite = spritesArray[(int)sprites.ClimbingFree0];
                    break;
                }
                playerSpriteRenderer.sprite = spritesArray[(int)sprites.ClimbingFree1];
                break;

            case playerState.noInputClimbingInitial:
                playerSpriteRenderer.sprite = spritesArray[(int)sprites.ClimbingLock0];
                break;

            case playerState.noInputClimbingEnding:
                playerSpriteRenderer.sprite = spritesArray[(int)sprites.ClimbingLock1];
                break;

            case playerState.interacting:
                if (animationFrame)
                {
                    playerSpriteRenderer.sprite = spritesArray[(int)sprites.Interact0];
                    break;
                }
                playerSpriteRenderer.sprite = spritesArray[(int)sprites.Interact1];
                break;


            default:
                playerSpriteRenderer.sprite = spritesArray[(int)sprites.StandingIdle0];
                break;
        }

    }

    private void FixedUpdate()
    {
        //Animation changes (between animation frames) is done in FixedUpdate, to keep it smooth
        currentFrame++;

        if (currentFrame > 50)
        {
            animationFrame = true;
            currentFrame = 0;
            return;
        }
        if (currentFrame > 25)
        {
            animationFrame = false;
        }

    }

    void EnterState(playerState newState)//Used for effects beyond player (ie UI elements).
    {

    }

    void ExitState(playerState oldState)//As above. Use in conjunction.
    {

    }

    void Interact()
    {
        //Trigger interact effect - call method from what is being interacted with for info / unique mechanics.
        //Trigger state change, appropriate to object being interacted with
        //Can add an 'interacting' state, to have minigames. Update then pulls controls from interact-ee
        if (interactee.CompareTag("Interactable"))
        {
            interactInitialFrame = currentFrame - 1;
            PlayerState = playerState.interacting;
            interactee.GetComponent<InteractableObject>().GenericInteract();
        }

    }

    //Check and state change to climbing states. This is its own method as it is used and identical in freeMovement, Jumping, and Falling.
    playerState? AttemptClimb()
    {
        if (interactee == null) { return null; }
        if (interactee.tag == "FreeClimb")
        {
            playerRigidbody.gravityScale = 0;
            playerRigidbody.velocity = Vector2.zero;
            return playerState.freeInputClimbing;
        }
        if (interactee.tag == "LockClimb")
        {
            interacteeHeight = interactee.GetComponent<SpriteRenderer>().bounds.size.y;
            playerRigidbody.gravityScale = 0;
            playerRigidbody.constraints = RigidbodyConstraints2D.FreezePositionX;
            return playerState.noInputClimbingInitial;
        }

        //No match is found
        Debug.Log("Error: No match found for interactee type in Attempt Climb");
        return null;
    }

    //Track what GameObject the player is currently triggering (standing next to)
    private void OnTriggerEnter2D(Collider2D collision)
    {
        interactee = collision.gameObject;
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        interactee = null;
    }
}
