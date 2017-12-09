﻿using UnityEngine;

public enum PlayerChildren { SlimeMesh, ShadowProjector, WaterEffect, Canvas, CameraTarget };
public class Player : MonoBehaviour {

    Rigidbody rb;
    bool canDoubleJump = false;

    [Header("Collectables")]
    [SerializeField] int[] collectables;

    public uint activeEvolutions = 0;

    public Transform respawnPoint;
    public GameObject cameraReference;

    Animator anim;
    public bool hasBeenTeleported = false;

    [SerializeField]
    Vector3[] keysInitialPosition;
    [SerializeField]
    Quaternion[] keysInitialRotation;

    public bool isEdgeAssistActive = true;
    PlayerController playerController;

    public Rigidbody Rb
    {
        get
        {
            if (rb == null)
                rb = GetComponent<Rigidbody>();
            return rb;
        }

        set
        {
            rb = value;
        }
    }

    public Animator Anim
    {
        get
        {
            if (anim == null)
                anim = GetComponentInChildren<Animator>();
            return anim;
        }

        set
        {
            anim = value;
        }
    }

    public bool CanDoubleJump
    {
        get
        {
            return canDoubleJump;
        }

        set
        {
            canDoubleJump = value;
        }
    }

    public int[] Collectables
    {
        get
        {
            return collectables;
        }

        set
        {
            collectables = value;
        }
    }

    public Vector3[] KeysInitialPosition
    {
        get
        {
            return keysInitialPosition;
        }
    }

    public Quaternion[] KeysInitialRotation
    {
        get
        {
            return keysInitialRotation;
        }
    }

    public PlayerController PlayerController
    {
        get
        {
            if (playerController == null)
                playerController = GetComponent<PlayerController>();
            return playerController;
        }

    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        collectables = new int[(int)CollectableType.Size];

        keysInitialPosition = new Vector3[Utils.GetMaxValueForCollectable(CollectableType.Key)];
        keysInitialRotation = new Quaternion[Utils.GetMaxValueForCollectable(CollectableType.Key)];
    }

    public void UpdateCollectableValue(CollectableType type, int pickedValue)
    {
        collectables[(int)type] = Mathf.Clamp(collectables[(int)type] + pickedValue, 0, Utils.GetMaxValueForCollectable(type));
        if (type == CollectableType.Key)
            GameManager.Instance.PlayerUI.RefreshKeysPlayerUi(this, collectables[(int)type]);
        if (type == CollectableType.Points)
            GameManager.Instance.PlayerUI.RefreshPointsPlayerUi(this, collectables[(int)type]);

        if (!Utils.IsAnEvolutionCollectable(type))
            return;

        EvolutionCheck(type);
    }

    public void AddKeyInitialPosition(Transform _tr)
    {
        int currentlyHold = collectables[(int)CollectableType.Key];

        keysInitialPosition[currentlyHold - 1] = _tr.position;
        keysInitialRotation[currentlyHold - 1] = _tr.rotation;
    }

    void EvolutionCheck(CollectableType type)
    {
        if (GameManager.CurrentGameMode.evolutionMode == EvolutionMode.GrabCollectableAndAutoEvolve)
        {
            Evolution evolution = GameManager.EvolutionManager.GetEvolutionByCollectableType(type);
            if (collectables[(int)type] >= evolution.Cost)
                EvolveGameplay1(evolution);
        }
        else if (GameManager.CurrentGameMode.evolutionMode == EvolutionMode.GrabEvolution)
        {
            if (activeEvolutions == 0)
            {
                Evolution evolution = GameManager.EvolutionManager.GetEvolutionByCollectableType(type);
                PermanentEvolution(evolution);
            }
        }
    }

    // GAMEPLAY TEST 1: all of this should be in an Evolution class handling all evolution parameters (+ we should be able to pickup collectables and "refresh" an evolution indefinitely)
    private void EvolveGameplay1(Evolution evolution)
    {
        GameManager.EvolutionManager.AddEvolutionComponent(gameObject, evolution);
        collectables[(int)evolution.AssociatedCollectable] -= evolution.Cost;
    }

    public void EvolveGameplay2(Evolution evolution)
    {
        GameManager.EvolutionManager.AddEvolutionComponent(gameObject, evolution);
        collectables[0] -= evolution.Cost;
    }

    private void PermanentEvolution(Evolution evolution)
    {
        GameManager.EvolutionManager.AddEvolutionComponent(gameObject, evolution, true);
    }

    private void Update()
    {
        if (Rb.velocity.y < 0.0f && PlayerController != null && PlayerController.IsGrounded)
        {
            if (!Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, 5.0f))
                PlayerController.IsGrounded = false;
        }

    }
}
