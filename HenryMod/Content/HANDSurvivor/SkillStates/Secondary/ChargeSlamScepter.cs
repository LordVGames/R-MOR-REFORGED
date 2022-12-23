﻿namespace EntityStates.HAND_Junked.Secondary
{
    public class ChargeSlamScepter : ChargeSlam
    {
        public override void SetNextState()
        {
            this.outer.SetNextState(new FireSlamScepter() { chargePercent = this.chargePercent });
        }
    }
}
