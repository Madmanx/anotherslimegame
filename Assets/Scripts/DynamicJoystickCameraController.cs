﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UWPAndXInput;
using Cinemachine;

public class DynamicJoystickCameraController : MonoBehaviour {
    public PlayerIndex playerIndex;
    GamePadState state;
    GamePadState prevState;
    Vector3 startLowOffset;
    Vector3 startMidOffset;
    Vector3 startHighOffset;

    public float cameraXAdjuster = 0.4f;
    public float cameraYAdjuster = 0.4f;

    private float timer = 0.0f;

    Cinemachine.CinemachineFreeLook freelookCamera;

    bool needToTendToMiddleRig = false;

    void Start () {
        freelookCamera = GetComponent<Cinemachine.CinemachineFreeLook>();
        startHighOffset = (freelookCamera.GetRig(0).GetCinemachineComponent<CinemachineComposer>()).m_TrackedObjectOffset;
        startMidOffset = (freelookCamera.GetRig(1).GetCinemachineComponent<CinemachineComposer>()).m_TrackedObjectOffset;
        startLowOffset = (freelookCamera.GetRig(2).GetCinemachineComponent<CinemachineComposer>()).m_TrackedObjectOffset;
    }
	
	void Update () {

        if (GameManager.Instance.PlayerStart.PlayersReference[(int)playerIndex].GetComponent<PlayerController>().IsUsingAController)
        {
            prevState = state;
            state = GamePad.GetState(playerIndex);

            if (prevState.Buttons.RightStick == ButtonState.Released && state.Buttons.RightStick == ButtonState.Pressed)
            {
                timer = 0.0f;
                freelookCamera.m_RecenterToTargetHeading.m_enabled = true;
            }

            if (freelookCamera.m_RecenterToTargetHeading.m_enabled)
            {
                timer += Time.deltaTime;
                if (timer >= freelookCamera.m_RecenterToTargetHeading.m_RecenterWaitTime + freelookCamera.m_RecenterToTargetHeading.m_RecenteringTime)
                {
                    freelookCamera.m_RecenterToTargetHeading.m_enabled = false;
                }
            }

            if (Mathf.Abs(state.ThumbSticks.Right.X ) > 0.1f)
            {
                freelookCamera.m_XAxis.m_InputAxisValue = -state.ThumbSticks.Right.X * cameraXAdjuster;
                freelookCamera.m_RecenterToTargetHeading.m_enabled = false;
                needToTendToMiddleRig = false;
            }
            else
                freelookCamera.m_XAxis.m_InputAxisValue = 0;

            if (Mathf.Abs(state.ThumbSticks.Right.Y) > 0.1f)
            {
                freelookCamera.m_YAxis.m_InputAxisValue = state.ThumbSticks.Right.Y * cameraYAdjuster;
                freelookCamera.m_RecenterToTargetHeading.m_enabled = false;
                needToTendToMiddleRig = false;

            }
            else
                freelookCamera.m_YAxis.m_InputAxisValue = 0;


            ////Need a more complex function ?
            freelookCamera.m_XAxis.m_InputAxisValue += Mathf.Abs(state.ThumbSticks.Left.X) > 0.1f ? (freelookCamera.m_XAxis.m_InvertAxis?-1:1) * state.ThumbSticks.Left.X* Mathf.Lerp(0.5f, 1.0f, Mathf.Abs(state.ThumbSticks.Left.X))/2.0f : 0;

            if (Mathf.Abs(state.ThumbSticks.Left.Y) > 0.1f)
            {
                needToTendToMiddleRig = true;
            }
            TendToMiddleRig();
        }
    }

    public void TendToMiddleRig()
    {
        if (needToTendToMiddleRig)
        {
            freelookCamera.m_YAxis.Value = 0.5f;
        }
    }
}
