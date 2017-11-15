﻿using XInputDotNetPure;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using Cinemachine;

public enum SkillState
{
    Ready,
    Charging,
    Dashing,
    Cooldown
}


public enum BrainState
{
    Free,
    Occupied
}


[RequireComponent(typeof(Player))]
public class PlayerController : MonoBehaviour {

    bool playerIndexSet = false;

    public PlayerIndex playerIndex;
    bool isUsingAController = false;
    GamePadState state;
    GamePadState prevState;

    [HideInInspector]public bool canMoveXZ = true;
    [HideInInspector]public bool canJump = true;
    Player player;

    bool isReadyForNextJumpInput = true;
    bool isWaitingForNextRelease = false;
    float chargeFactor = 0.0f;

    [SerializeField] public Stats stats = new Stats(); // tu mens intellisense
    [SerializeField]
    [Range(5, 1000)] float jumpChargeSpeed = 15.0f;

    int selectedEvolution = 0;
    [SerializeField]
    [Range(70, 250)]
    float customGravity; // 90 seems good
    [SerializeField]
    [Range(0, 250)]
    float airForce;

    public bool isFreeFalling = false;
    // TODO: send this value to jumpManager
    bool isGrounded = true;
    public bool canDoubleJump = true;
    bool hasJumpButtonBeenReleased = true;

    public bool isGravityEnabled = true;

    // TMP??
    RaycastHit hitInfo;
    float maxDistanceOffset = 2.0f;

    // Dashing variables
    private BrainState brainState;
    private SkillState dashingState;
    private SkillState strengthState;
    private SkillState platformistState;

    // Dashing variables // Redefine in start()
    public float dashingTimer;
    public float dashingMaxTimer;
    public float dashingCooldownTimer;
    public float dashingCooldownMaxTimer;
    public float dashingVelocity;

    // Camera Dumping Values // Redefine in start()
    public float defaultDumpingValues = 0.2f;
    public float noDumpingValues = 0.0f;

    public ForcedJump forcedJump;

    public bool DEBUG_hasBeenSpawnedFromTool = false;

    // Platformist variables
    float timerRightTriggerPressed = 0.0f;
    bool rightTriggerHasBeenPressed = false;
    bool waitForRightTriggerRelease = false;

    private void Awake()
    {
        stats.Init(this);
        JumpManager jumpManager = GetComponent<JumpManager>();
        if (jumpManager != null)
            customGravity = jumpManager.GetGravity(stats.Get(Stats.StatType.GROUND_SPEED));
    }

    private void Start()
    {
        player = GetComponent<Player>();
        if (player == null)
            Debug.LogWarning("Player component should not be null");

        // Dashing initialisation
        dashingMaxTimer = 0.15f;
        dashingTimer = dashingMaxTimer;
        dashingCooldownMaxTimer = 0.5f;
        dashingCooldownTimer = dashingCooldownMaxTimer;
        dashingVelocity = 100.0f;

        // Initialize dashing state at Cooldown to prevent controller shit
        dashingState = SkillState.Cooldown;
        brainState = BrainState.Free;

        strengthState = SkillState.Ready;

        // Camera Dumping values
        defaultDumpingValues = 0.2f;
        noDumpingValues = 0.0f;

        forcedJump = new ForcedJump();
    }

    void FixedUpdate()
    {
        if (isGravityEnabled)
        {
            // TODO : Vector.down remove the minus ???? lol
            player.Rb.AddForce(-customGravity * Vector3.up, ForceMode.Acceleration);
            if (player.Rb.velocity.y < -10.0f)
            {
                // No Inputs Mode
                isFreeFalling = true;
            }
            else
            {
                isFreeFalling = false;
            }

        }

        if (forcedJump.IsForcedJumpActive)
        {
            forcedJump.AddForcedJumpForce(player.Rb);
            return;
        }

        if (DEBUG_hasBeenSpawnedFromTool)
            return;

        //player.Rb.velocity = new Vector3(player.Rb.velocity.x, -customGravity, player.Rb.velocity.z);
        // TODO: externaliser pour le comportement multi
        if (!playerIndexSet)
            return;

        if (!prevState.IsConnected)
        {
            isUsingAController = false;
            for (int i = 0; i < GameManager.Instance.ActivePlayersAtStart; i++)
            {
                GamePadState testState = GamePad.GetState(playerIndex);

                if (testState.IsConnected)
                {
                    playerIndexSet = true;
                    isUsingAController = true;
                    break;
                }
            }
        }

        if (isUsingAController)
        {
            // TODO: optimize?
            prevState = state;
            state = GamePad.GetState(playerIndex);

            if (GameManager.CurrentState == GameState.Normal)
            {
                if (canMoveXZ)
                    HandleMovementWithController();
                if (canJump)
                    HandleJumpWithController();
                if (GameManager.CurrentGameMode.evolutionMode == EvolutionMode.GrabCollectableAndActivate)
                    HandleEvolutionsWithController();

                // Dash
                DashControllerState();
                // Strength
                if (GetComponent<EvolutionStrength>() != null)
                    StrengthControllerState();
                if (GetComponent<EvolutionPlatformist>() != null)
                    PlatformistControllerState();
            }
            // TODO: Externalize "state" to handle pause in PauseMenu? //  Remi : Can't manage GamePade(IndexPlayer) Instead, copy not working
            if (SceneManager.GetActiveScene() != SceneManager.GetSceneByBuildIndex(0))
                if (prevState.Buttons.Start == ButtonState.Released && state.Buttons.Start == ButtonState.Pressed)
                    GameManager.ChangeState(GameState.Paused);

        }
        else
        {
            // Keyboard
            if (GameManager.CurrentState == GameState.Normal)
            {
                HandleMovementWithKeyBoard();
                HandleJumpWithKeyboard();
            }
            if (SceneManager.GetActiveScene() != SceneManager.GetSceneByBuildIndex(0))
                if (Input.GetKeyDown(KeyCode.Escape))
                    GameManager.ChangeState(GameState.Paused);
        }
    }

    public bool IsGrounded
    {
        get
        {
            return isGrounded;
        }

        private set
        {

            if (value == true && GetComponent<JumpManager>() != null)
                GetComponent<JumpManager>().Stop();
            if (forcedJump != null && forcedJump.IsForcedJumpActive)
                forcedJump.Stop();
            if (value == true)
                GetComponent<Player>().Anim.SetBool("isExpulsed", false);

            isGrounded = value;
        }
    }

    public PlayerIndex PlayerIndex
    {
        get
        {
            return playerIndex;
        }

        set
        {
            playerIndex = value;
        }
    }

    public bool IsUsingAController
    {
        get
        {
            return isUsingAController;
        }

        set
        {
            isUsingAController = value;
        }
    }

    public bool PlayerIndexSet
    {
        get
        {
            return playerIndexSet;
        }
        set
        {
            playerIndexSet = value;
        }
    }

    public SkillState DashingState
    {
        get
        {
            return dashingState;
        }

        set
        {
            switch (value)
            {
                case SkillState.Charging:
                case SkillState.Dashing:
                    isGravityEnabled = false;
                    BrainState = BrainState.Occupied;
                    break;
                default:
                    if (!isGravityEnabled) isGravityEnabled = true;
                    BrainState = BrainState.Free;
                    break;
            }
            dashingState = value;
        }
    }

    public SkillState StrengthState
    {
        get
        {
            return strengthState;
        }

        set
        {
            switch (value)
            {
                case SkillState.Charging:
                    isGravityEnabled = false;
                    BrainState = BrainState.Occupied;
                    break;
                case SkillState.Dashing:
                    BrainState = BrainState.Occupied;
                    break;
                default:
                    if (!isGravityEnabled) isGravityEnabled = true;
                    BrainState = BrainState.Free;
                    break;
            }
            strengthState = value;
        }
    }

    public BrainState BrainState
    {
        get
        {
            return brainState;
        }

        set
        {
            if (value == BrainState.Occupied)
            {
                canJump = false;
                canMoveXZ = false;
                ChangeDumpingValuesCameraFreeLook(noDumpingValues);
            }
            else // Free
            {
                canJump = true;
                canMoveXZ = true;
                ChangeDumpingValuesCameraFreeLook(defaultDumpingValues);
            }
            brainState = value;
        }
    }

    public SkillState PlatformistState
    {
        get
        {
            return platformistState;
        }

        set
        {
            platformistState = value;
        }
    }

    private void DashControllerState()
    {
        switch (dashingState)
        {
            case SkillState.Ready:
                if (brainState == BrainState.Occupied) return;

                if (prevState.Buttons.X == ButtonState.Released && state.Buttons.X == ButtonState.Pressed)
                {
                    // TMP         
                    if(GetComponent<EvolutionStrength>() != null) GetComponent<EvolutionStrength>().ColorChangeAsupr(Color.red);

                    GetComponent<JumpManager>().Stop();
                    DashingState = SkillState.Dashing;
                }
                break;
            case SkillState.Dashing:
                player.Rb.velocity = transform.forward * dashingVelocity;
    
                dashingTimer -= Time.fixedDeltaTime;
                // ? Timer ?
                if (dashingTimer <= 0.0f)
                {
                    dashingTimer = dashingMaxTimer;
                    DashingState = SkillState.Cooldown;

                    // TMP    
                    if (GetComponent<EvolutionStrength>() != null) GetComponent<EvolutionStrength>().ColorChangeAsupr(Color.white);
                }
                break;
            case SkillState.Cooldown:

                dashingCooldownTimer -= Time.fixedDeltaTime;
                if (dashingCooldownTimer <= 0.0f)
                {
                    dashingCooldownTimer = dashingCooldownMaxTimer;
                    DashingState = SkillState.Ready;
                }
                break;
            default: break;
        }
    }

    private void StrengthControllerState()
    {
        switch (strengthState)
        {
            case SkillState.Ready:
                if (brainState == BrainState.Occupied) return;

                if (prevState.Buttons.Y == ButtonState.Released && state.Buttons.Y == ButtonState.Pressed)
                {
                    GetComponent<EvolutionStrength>().DashStart();
                    StrengthState = SkillState.Charging;
                }

                break;
            case SkillState.Charging:
                if (state.Buttons.Y == ButtonState.Pressed)
                {
                    GetComponent<EvolutionStrength>().Levitate();
                }
                else if (state.Buttons.Y == ButtonState.Released)
                {
                    GetComponent<EvolutionStrength>().LaunchDash();
                    StrengthState = SkillState.Dashing;
                }
                break;
            case SkillState.Dashing:
                break;
            case SkillState.Cooldown:
                break;
            default: break;
        }
    }

    private void PlatformistControllerState()
    {
        // /!\ WARNING: code conflictuel si on combine les évolutions
        switch (platformistState)
        {
            case SkillState.Ready:
                if (brainState == BrainState.Occupied) return;

                if (prevState.Triggers.Right < 0.1f && state.Triggers.Right > 0.1f)
                {
                    rightTriggerHasBeenPressed = true;
                }

                if (rightTriggerHasBeenPressed && state.Triggers.Right > 0.1f)
                    timerRightTriggerPressed += Time.deltaTime;

                if (timerRightTriggerPressed > 1.5f)
                {
                    // Show pattern + buttons to swap
                    // Tant qu'on a pas relaché la gachette
                    GetComponent<EvolutionPlatformist>().IndexSelection(prevState, state);
                    waitForRightTriggerRelease = true;
                }

                if (prevState.Triggers.Right > 0.1f && state.Triggers.Right < 0.1f)
                {
                    rightTriggerHasBeenPressed = false;
                    waitForRightTriggerRelease = false;

                    if (timerRightTriggerPressed > 1.5f)
                        GetComponent<EvolutionPlatformist>().CreatePatternPlatforms();
                    else
                        GetComponent<EvolutionPlatformist>().CreatePlatform(state);

                    timerRightTriggerPressed = 0.0f;
                    PlatformistState = SkillState.Cooldown;
                }


                break;
            case SkillState.Charging:
                break;
            case SkillState.Dashing:
                break;
            case SkillState.Cooldown:
                GetComponent<EvolutionPlatformist>().TimerPlatform += Time.deltaTime; 
                break;
            default: break;
        }
    }

    private void HandleEvolutionsWithController()
    {
        if (prevState.Buttons.LeftShoulder == ButtonState.Released && state.Buttons.LeftShoulder == ButtonState.Pressed)
        {
            selectedEvolution = selectedEvolution > 0 ? (selectedEvolution - 1) % (int)Powers.Size : 0;
            GameManager.UiReference.NeedUpdate(selectedEvolution.ToString());
        }
        if (prevState.Buttons.RightShoulder == ButtonState.Released && state.Buttons.RightShoulder == ButtonState.Pressed)
        {
            selectedEvolution = (selectedEvolution + 1) % (int)Powers.Size;
            GameManager.UiReference.NeedUpdate(selectedEvolution.ToString());
        }
        if (prevState.Buttons.Y == ButtonState.Released && state.Buttons.Y == ButtonState.Pressed)
        {
            Evolution selectedEvol = GameManager.EvolutionManager.GetEvolutionByPowerName((Powers)Enum.Parse(typeof(Powers), selectedEvolution.ToString()));
            if (player.Collectables[0] >= selectedEvol.Cost)
            {
                player.EvolveGameplay2(selectedEvol);
            }
            // if has enough => evolve else nothing
        }
    }

    private void HandleMovementWithKeyBoard()
    {
        
        Vector3 initialVelocity = new Vector3(Input.GetAxisRaw("Horizontal"), 0.0f, Input.GetAxisRaw("Vertical"));
        initialVelocity.Normalize();
        initialVelocity *= (Mathf.Abs(Input.GetAxisRaw("Horizontal")) + Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.95f) ? stats.Get(Stats.StatType.GROUND_SPEED) : stats.Get(Stats.StatType.GROUND_SPEED) / 2.0f;

        player.Rb.velocity = new Vector3(initialVelocity.x, player.Rb.velocity.y, initialVelocity.z);

        Vector3 camVectorForward = new Vector3(player.cameraReference.transform.GetChild(0).forward.x, 0.0f, player.cameraReference.transform.GetChild(0).transform.forward.z);
        camVectorForward.Normalize();

        Vector3 velocityVec = initialVelocity.z * camVectorForward + initialVelocity.x * player.cameraReference.transform.GetChild(0).right + Vector3.up * player.Rb.velocity.y;

        player.Rb.velocity = velocityVec;
        transform.LookAt(transform.position + new Vector3(velocityVec.x, 0.0f, velocityVec.z));

    }

    private void HandleMovementWithController()
    {          
        Vector3 initialVelocity = new Vector3(state.ThumbSticks.Left.X, 0.0f, state.ThumbSticks.Left.Y);

        initialVelocity.Normalize();
        if (!isFreeFalling)
            initialVelocity *= (Mathf.Abs(state.ThumbSticks.Left.X) + Mathf.Abs(state.ThumbSticks.Left.Y) > 0.95f) ? stats.Get(Stats.StatType.GROUND_SPEED) : stats.Get(Stats.StatType.GROUND_SPEED) / 2.0f;
        else
            initialVelocity *= (Mathf.Abs(state.ThumbSticks.Left.X) + Mathf.Abs(state.ThumbSticks.Left.Y) > 0.95f) ? stats.Get(Stats.StatType.AIR_CONTROL) : stats.Get(Stats.StatType.AIR_CONTROL) / 2.0f;

        Vector3 camVectorForward = new Vector3(player.cameraReference.transform.GetChild(0).forward.x, 0.0f, player.cameraReference.transform.GetChild(0).forward.z);
        camVectorForward.Normalize();

        Vector3 velocityVec = initialVelocity.z * camVectorForward + Vector3.up * player.Rb.velocity.y;
        if (isGrounded)
            velocityVec += initialVelocity.x * player.cameraReference.transform.GetChild(0).right;

        player.Rb.velocity = velocityVec;
        transform.LookAt(transform.position + new Vector3(velocityVec.x, 0.0f, velocityVec.z) + initialVelocity.x * player.cameraReference.transform.GetChild(0).right);
        
        // TMP Animation
        player.GetComponent<Player>().Anim.SetFloat("MouvementSpeed", Mathf.Abs(state.ThumbSticks.Left.X) > Mathf.Abs(state.ThumbSticks.Left.Y) ? Mathf.Abs(state.ThumbSticks.Left.X) : Mathf.Abs(state.ThumbSticks.Left.Y));
        player.GetComponent<Player>().Anim.SetBool("isWalking", ((Mathf.Abs(state.ThumbSticks.Left.X) > 0.02f) || Mathf.Abs(state.ThumbSticks.Left.Y) > 0.02f) && player.GetComponent<PlayerController>().IsGrounded);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponentInParent<Ground>() != null)
        {
            //if (Physics.Raycast(transform.position, -transform.up, out hitInfo, maxDistanceOffset))
            //{
            //    if (hitInfo.transform.gameObject.GetComponentInParent<Ground>() != null)
            //        IsGrounded = true;
            //}

            //Debug.Log("normal" + collision.contacts[0].normal);
            //Debug.Log("angle" + Vector3.Angle(collision.contacts[0].normal, transform.up));
            //if (Vector3.Angle(collision.contacts[0].normal, transform.up) < 45)
            //{
            //    IsGrounded = true;
            //}

            if (isUsingAController ? state.Buttons.A == ButtonState.Released : true)
            {
                isReadyForNextJumpInput = true;
                canDoubleJump = true;
            }
            else
            {
                canDoubleJump = false;
                isWaitingForNextRelease = true;
            }
        }
   
    }

    private void Update()
    {
        if (player.Rb.velocity.y <= 0.2f && !isGrounded)
        {



            if (Physics.SphereCast(transform.position + Vector3.up, 1f, -transform.up, out hitInfo, maxDistanceOffset))
            {
                if (hitInfo.transform.gameObject.GetComponentInParent<Ground>() != null)
                {
                    IsGrounded = true;

                }
            }

            Ray ray = new Ray(transform.position, Vector3.down);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                float force = 200f;
                float forceOffset = 0.1f;
                MeshDeformer deformer = GetComponentInChildren<MeshDeformer>();
                if (deformer)
                {
                    Vector3 point = hit.point;
                    point += hit.normal * forceOffset;
                    deformer.AddDeformingForce(point, -force);
                    deformer.AddDeformingForce(point, +force / 5);

                }
            }
        }
        stats.Update();
    }

    private void HandleJumpWithController()
    {
        if (!IsGrounded && !canDoubleJump)
            return;

        // Charge jump if A button is pressed for a "long" time and only if on the ground
        if (isGrounded)
        {
            if (state.Buttons.A == ButtonState.Pressed && chargeFactor < 1.0f && isReadyForNextJumpInput)
            {
                chargeFactor += jumpChargeSpeed * Time.unscaledDeltaTime;
                // Force max charge jump if the charge reach maximum charge
                if (chargeFactor > 1.0f)
                {
                    Jump();
                }
            }

            if (prevState.Buttons.A == ButtonState.Pressed && state.Buttons.A == ButtonState.Released && isReadyForNextJumpInput)
            {
                Jump();

            }
        }

        if (state.Buttons.A == ButtonState.Released)
            hasJumpButtonBeenReleased = true;

        // Jump when the A button is released and only if on the ground
        if (!isReadyForNextJumpInput && state.Buttons.A == ButtonState.Pressed && canDoubleJump && hasJumpButtonBeenReleased)
        {
            GetComponent<JumpManager>().Stop();
            canDoubleJump = false;
            Jump();
            if (AudioManager.Instance != null && AudioManager.Instance.youpiFX != null)
                AudioManager.Instance.PlayOneShot(AudioManager.Instance.youpiFX);
        }

        // Prevent input in the air
        if (state.Buttons.A == ButtonState.Released && isWaitingForNextRelease)
        {
            isWaitingForNextRelease = false;
            isReadyForNextJumpInput = true;
            canDoubleJump = true;
        }
    }

    private void HandleJumpWithKeyboard()
    {
        if (!IsGrounded)
            return;

        if (Input.GetKeyDown(KeyCode.Space) && isReadyForNextJumpInput)
            Jump();

    }

    public void Jump()
    {
        IsGrounded = false;
        JumpManager jm;
        if (jm = GetComponent<JumpManager>())
            jm.Jump(stats.Get(Stats.StatType.GROUND_SPEED),JumpManager.JumpEnum.Basic);
        else
            Debug.LogError("No jump manager attached to player!");

        isReadyForNextJumpInput = false;
        isWaitingForNextRelease = false;
        hasJumpButtonBeenReleased = false;
        chargeFactor = 0.0f;
    }

  
    // TODO : Remi , Export this in camera controls
    public void ChangeDumpingValuesCameraFreeLook(float _newValues)
    {
        if (player.cameraReference != null && player.cameraReference.transform.GetChild(1).GetComponent<Cinemachine.CinemachineFreeLook>())
        {
            //Body
            CinemachineTransposer tr;
            tr = ((CinemachineTransposer)(player.cameraReference.transform.GetChild(1).GetComponent<Cinemachine.CinemachineFreeLook>().GetRig(0).GetCinemachineComponent(CinemachineCore.Stage.Body)));
            tr.m_XDamping = _newValues;
            tr.m_YDamping = _newValues;
            tr.m_ZDamping = _newValues;

            tr = ((CinemachineTransposer)(player.cameraReference.transform.GetChild(1).GetComponent<Cinemachine.CinemachineFreeLook>().GetRig(1).GetCinemachineComponent(CinemachineCore.Stage.Body)));
            tr.m_XDamping = _newValues;
            tr.m_YDamping = _newValues;
            tr.m_ZDamping = _newValues;

            tr = ((CinemachineTransposer)(player.cameraReference.transform.GetChild(1).GetComponent<Cinemachine.CinemachineFreeLook>().GetRig(2).GetCinemachineComponent(CinemachineCore.Stage.Body)));
            tr.m_XDamping = _newValues;
            tr.m_YDamping = _newValues;
            tr.m_ZDamping = _newValues;
        }
    }
}
