﻿using RMORMod.Content.Shared.Components.Body;
using RoR2;
using UnityEngine;
using RMORMod.Content.RMORSurvivor;
using UnityEngine.Networking;

namespace RMORMod.Content.HANDSurvivor.Components.Body
{
    public class HANDNetworkComponent : NetworkBehaviour
    {
        private CharacterBody characterBody;
        private OverclockController overclockController;
        public void Awake()
        {
            characterBody = base.GetComponent<CharacterBody>();
            overclockController = base.GetComponent<OverclockController>();
        }

        [Server]
        public void ResetSpecialStock()
        {
            if (NetworkServer.active)
            {
                RpcResetSpecialStock();
            }
        }

        [ClientRpc]
        private void RpcResetSpecialStock()
        {
            characterBody.skillLocator.special.stock = 0;
        }

        [Server]
        public void SquashEnemy(uint networkID)
        {
            if (NetworkServer.active)
            {
                RpcAddSquash(networkID);
            }
        }

        [ClientRpc]
        private void RpcAddSquash(uint networkID)
        {
            GameObject go = ClientScene.FindLocalObject(new NetworkInstanceId(networkID));
            if (go)
            {
                CharacterMaster cm = go.GetComponent<CharacterMaster>();
                if (cm)
                {
                    GameObject bodyObject = cm.GetBodyObject();
                    if (bodyObject)
                    {
                        SquashedComponent sq = bodyObject.GetComponent<SquashedComponent>();
                        if (sq)
                        {
                            sq.ResetGraceTimer();
                        }
                        else
                        {
                            sq = bodyObject.AddComponent<SquashedComponent>();
                        }
                        sq.triggerer = base.gameObject;
                    }
                }
            }
        }

        [Server]
        public void ExtendOverclockServer(float duration)
        {
            if (!NetworkServer.active) return;
            RpcExtendOverclock(duration);
        }

        [ClientRpc]
        private void RpcExtendOverclock(float duration)
        {
            if (this.hasAuthority && overclockController)
            {
                overclockController.ExtendOverclock(duration);
            }
        }

        [Server]
        public void AddSpecialStockServer()
        {
            if (!NetworkServer.active) return;
            RpcAddSpecialStock();
        }

        [ClientRpc]
        public void RpcAddSpecialStock()
        {
            if (hasAuthority && characterBody.skillLocator.special.stock < characterBody.skillLocator.special.maxStock && (characterBody.skillLocator.special.skillDef == Skilldefs.SpecialMissile))
            {
                characterBody.skillLocator.special.stock++;
                if (characterBody.skillLocator.special.stock == characterBody.skillLocator.special.maxStock)
                {
                    characterBody.skillLocator.special.rechargeStopwatch = 0f;
                }
            }
        }

        private void OnDestroy()
        {
            Util.PlaySound("Play_MULT_shift_end", base.gameObject);
        }
    }
}
