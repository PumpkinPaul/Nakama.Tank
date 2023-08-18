// Copyright Pumpkin Games Ltd. All Rights Reserved.

// Multiple Tank game with client side prediction

// See PlayGamePhase for networked multiplayer - the main updte loop consists of the following actions:

/*

'simulation' state
------------------
This is the latest master copy of the tank state, used by our local physics computations and prediction. 
This state will jerk whenever a new network packet is received.

'previous' state
------------------
This is a copy of the state from immediately before the last network packet was received.

'display' state
------------------
This is the tank state that is drawn onto the screen. It is gradually interpolated from the 'previous' state
toward the 'simultation' state, in order to smooth out any sudden jumps caused by discontinuities when
a network packet suddenly modifies the 'simultation' state.

====================================================================================================
Phase 1 'UpdateLocalGamer'
====================================================================================================
For each local player...
#1 Read inputs (keyboard, gamepad, etc)
#2 Apply inputs to update the 'simulation' state
#3 Local players have no prediction so set 'display' state to 'simulation' state
#4 Send local state info to the server (positions, rotations, inputs).
    We don't want to flood the network!
    The game runs at a fixed timestep of 60 fps but we'll send data maybe 10 or 20 fps

====================================================================================================
Phase 2 'ReadIncomingPackets'
====================================================================================================
For each remote player...
#1 Read incoming packets from the network and apply to the 'simulation' state
#2 Set 'previous' state to 'display' state if smoothing
#3 Apply predication if enabled
  Get delta between local time and packet time
  Keep a rolling average of this delta time - (averaged time difference from the last 100 incoming 
      packets, used to estimate how our local clock compares to the time on the remote machine).
  Update 'simulation' state as many times as neccessary to catch up to current time

====================================================================================================
Phase 3 'UpdateRemote'
====================================================================================================
For each remote player...
#1 Update the smoothing amount, which interpolates from the previous state toward the current 'simultation' state
#2 Update the 'simulation' state if predicting
#3 Update the 'previous' state if smoothing amount > 0
#4 Apply smoothing - set 'display' state, interpolating from 'previous' state to 'simulation' state

*/

using System;

namespace NakamaTank;

static class Program
{
    [STAThread]
    static void Main()
    {
        new TankGame().Run();
    }
}
