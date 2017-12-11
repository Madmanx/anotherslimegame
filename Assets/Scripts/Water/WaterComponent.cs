﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterComponent : MonoBehaviour {
    public Vector3 buoyancyCentreOffset;
    public float bounceDamp;
    public float waterLevel;
    public float bounceAmplitude;
    public float compensationGravity;

    StatBuff movestatbuff;
    StatBuff dashstatbuff;
    StatBuff jumpstatbuff;
    public float tolerance;
    public float waterResistance;

    public GameObject WaterToActivateAtRuntime;

    public GameObject WaterParticleSystemToInstantiate;

    public void Start()
    {
        WaterToActivateAtRuntime.SetActive(true);

        movestatbuff = new StatBuff(Stats.StatType.GROUND_SPEED, waterResistance, -1, "water_move_debuff");
        dashstatbuff = new StatBuff(Stats.StatType.DASH_FORCE, waterResistance, -1, "water_dash_debuff");
        jumpstatbuff = new StatBuff(Stats.StatType.JUMP_HEIGHT, waterResistance, -1, "water_jump_debuff");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<Rigidbody>() != null && other.GetComponent<Player>())
        {
            if (other.transform.GetChild((int)PlayerChildren.WaterEffect).GetComponent<ParticleSystem>())
            {          
                // SEB C'est pour toi
                Instantiate(WaterParticleSystemToInstantiate, other.transform.position + (Vector3.up *2), other.transform.rotation, null);
                other.transform.GetChild((int)PlayerChildren.WaterEffect).gameObject.SetActive(true);
            }
     
            
            if (waterResistance != 0)
            {

                other.GetComponent<PlayerController>().stats.AddBuff(movestatbuff);
                other.GetComponent<PlayerController>().stats.AddBuff(dashstatbuff);
                other.GetComponent<PlayerController>().stats.AddBuff(jumpstatbuff);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<Rigidbody>() != null && other.GetComponent<Player>())
        {
            if (other.transform.GetChild((int)PlayerChildren.WaterEffect).GetComponent<ParticleSystem>())
            {
                // SEB C'est pour toi
                other.transform.GetChild((int)PlayerChildren.WaterEffect).gameObject.SetActive(false);
            }

            if (waterResistance != 0)
            {
                // TODO : Need a contains buff ?
                other.GetComponent<PlayerController>().stats.RemoveBuff(movestatbuff);
                other.GetComponent<PlayerController>().stats.RemoveBuff(dashstatbuff);
                other.GetComponent<PlayerController>().stats.RemoveBuff(jumpstatbuff);
            }

            WaterImmersionCamera waterImmersionCamera = other.GetComponent<Player>().cameraReference.transform.GetChild(0).GetComponent<WaterImmersionCamera>();
            if(waterImmersionCamera)
                waterImmersionCamera.isImmerge = false;
        }
    }
}
