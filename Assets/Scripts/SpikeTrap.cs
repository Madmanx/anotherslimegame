﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpikeTrap : MonoBehaviour {
    [SerializeField]
    CollectableType damageOn = CollectableType.Points;
    [SerializeField]
    int damage;
	void OnTriggerEnter(Collider col)
    {
        if(col.GetComponentInParent<Player>())
        {
            Player p = col.GetComponentInParent<Player>();
            p.CanDoubleJump = true;
            p.UpdateCollectableValue(damageOn, -damage);

            PlayerController pController = p.GetComponent<PlayerController>(); 
            p.GetComponent<JumpManager>().Jump(JumpManager.JumpEnum.Basic);
        }
    }
}
