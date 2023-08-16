// Copyright Pumpkin Games Ltd. All Rights Reserved.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NakamaTank.Engine;
using NakamaTank.Engine.Extensions;
using NakamaTank.NakamaMultiplayer;
using NakamaTank.NakamaMultiplayer.Players;
using System;

namespace NakamaTank;

/// <summary>
/// Represents the player controlled tank.
/// </summary>
public class Tank
{
    const float TANK_TURN_RATE = 0.01f;
    const float TURRET_TURN_RATE = 0.03f;
    const float TANK_SPEED = 0.3f;
    const float TANK_FRICTION = 0.9f;

    // To implement smoothing, we need more than one copy of the tank state.
    // We must record both where it used to be, and where it is now, an also
    // a smoothed value somewhere in between these two states which is where
    // we will draw the tank on the screen. To simplify managing these three
    // different versions of the tank state, we move all the state fields into
    // this internal helper structure.
    record struct TankState
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float TankRotation;
        public float TurretRotation;
    }

    // This is the latest master copy of the tank state, used by our local
    // physics computations and prediction. This state will jerk whenever
    // a new network packet is received.
    TankState _simulationState;

    // This is a copy of the state from immediately before the last
    // network packet was received.
    TankState _previousState;

    // This is the tank state that is drawn onto the screen. It is gradually
    // interpolated from the previousState toward the simultationState, in
    // order to smooth out any sudden jumps caused by discontinuities when
    // a network packet suddenly modifies the simultationState.
    TankState _displayState;

    // Used to interpolate displayState from previousState toward simulationState.
    float _currentSmoothing;

    // Averaged time difference from the last 100 incoming packets, used to
    // estimate how our local clock compares to the time on the remote machine.
    readonly RollingAverage _clockDelta = new(100);

    // Input controls can be read from keyboard, gamepad, or the network.
    Vector2 _tankInput;
    Vector2 _turretInput;

    Vector2 _screenSize;

    /// <summary>
    /// Constructs a new Tank instance.
    /// </summary>
    public Tank(Vector2 position)
    {
        _simulationState.Position = position;
        _simulationState.TankRotation = -MathHelper.PiOver2;
        _simulationState.TurretRotation = -MathHelper.PiOver2;

        // Initialize all three versions of our state to the same values.
        _previousState = _simulationState;
        _displayState = _simulationState;

        _screenSize = new Vector2(BaseGame.SCREEN_WIDTH, BaseGame.SCREEN_HEIGHT);
    }

    /// <summary>
    /// Moves a locally controlled tank in response to the specified inputs.
    /// </summary>
    public void UpdateLocal(Vector2 tankInput, Vector2 turretInput)
    {
        this._tankInput = tankInput;
        this._turretInput = turretInput;

        // Update the master simulation state.
        UpdateState(ref _simulationState);

        // Locally controlled tanks have no prediction or smoothing, so we
        // just copy the simulation state directly into the display state.
        _displayState = _simulationState;
    }

    /// <summary>
    /// Applies prediction and smoothing to a remotely controlled tank.
    /// </summary>
    public void UpdateRemote(int framesBetweenPackets, bool enablePrediction)
    {
        // Update the smoothing amount, which interpolates from the previous
        // state toward the current simultation state. The speed of this decay
        // depends on the number of frames between packets: we want to finish
        // our smoothing interpolation at the same time the next packet is due.
        float smoothingDecay = 1.0f / framesBetweenPackets;

        _currentSmoothing -= smoothingDecay;

        if (_currentSmoothing < 0)
            _currentSmoothing = 0;

        if (enablePrediction)
        {
            // Predict how the remote tank will move by updating
            // our local copy of its simultation state.
            UpdateState(ref _simulationState);

            // If both smoothing and prediction are active,
            // also apply prediction to the previous state.
            if (_currentSmoothing > 0)
            {
                UpdateState(ref _previousState);
            }
        }

        if (_currentSmoothing > 0)
        {
            // Interpolate the display state gradually from the
            // previous state to the current simultation state.
            ApplySmoothing();
        }
        else
        {
            // Copy the simulation state directly into the display state.
            _displayState = _simulationState;
        }
    }

    /// <summary>
    /// Writes our local tank state into a 'network packet'.
    /// </summary>
    public void WriteNetworkPacketJson(GameTime gameTime, out string packet)
    {
        packet = MatchDataJson.TankPacket(
            // Send our current time.
            (float)gameTime.TotalGameTime.TotalSeconds,
            // Send the current state of the tank.
            _simulationState.Position,
            _simulationState.Velocity,
            _simulationState.TankRotation,
            _simulationState.TurretRotation,
            // Also send our current inputs. These can be used to more accurately
            // predict how the tank is likely to move in the future.
            _tankInput,
            _turretInput);
    }

    /// <summary>
    /// Writes our local tank state into a network packet.
    /// </summary>
    public void WriteNetworkPacket(GameTime gameTime, PacketWriter packetWriter)
    {
        // Send our current time.
        packetWriter.Write((float)gameTime.TotalGameTime.TotalSeconds);

        // Send the current state of the tank.
        packetWriter.Write(_simulationState.Position);
        packetWriter.Write(_simulationState.Velocity);
        packetWriter.Write(_simulationState.TankRotation);
        packetWriter.Write(_simulationState.TurretRotation);

        // Also send our current inputs. These can be used to more accurately
        // predict how the tank is likely to move in the future.
        packetWriter.Write(_tankInput);
        packetWriter.Write(_turretInput);
    }

    /// <summary>
    /// Reads the state of a remotely controlled tank from a network packet.
    /// </summary>
    public void ReadNetworkPacketEvent(
        GameTime gameTime,
        ReceivedRemotePlayerTankStateEventArgs packetReader,
        TimeSpan latency,
        bool enablePrediction,
        bool enableSmoothing)
    {
        if (enableSmoothing)
        {
            // Start a new smoothing interpolation from our current
            // state toward this new state we just received.
            _previousState = _displayState;
            _currentSmoothing = 1;
        }
        else
        {
            _currentSmoothing = 0;
        }

        // Read what time this packet was sent.
        float packetSendTime = packetReader.TotalSeconds;

        // Read simulation state from the network packet.
        _simulationState.Position = packetReader.Position;
        _simulationState.Velocity = packetReader.Velocity;
        _simulationState.TankRotation = packetReader.TankRotation;
        _simulationState.TurretRotation = packetReader.TurretRotation;

        // Read remote inputs from the network packet.
        _tankInput = packetReader.TankInput;
        _turretInput = packetReader.TurretInput;

        // Optionally apply prediction to compensate for
        // how long it took this packet to reach us.
        if (enablePrediction)
        {
            ApplyPrediction(gameTime, latency, packetSendTime);
        }
    }

    /// <summary>
    /// Reads the state of a remotely controlled tank from a network packet.
    /// </summary>
    public void ReadNetworkPacket(
        GameTime gameTime,
        PacketReader packetReader,
        TimeSpan latency,
        bool enablePrediction, 
        bool enableSmoothing)
    {
        if (enableSmoothing)
        {
            // Start a new smoothing interpolation from our current
            // state toward this new state we just received.
            _previousState = _displayState;
            _currentSmoothing = 1;
        }
        else
        {
            _currentSmoothing = 0;
        }

        // Read what time this packet was sent.
        float packetSendTime = packetReader.ReadSingle();

        // Read simulation state from the network packet.
        _simulationState.Position = packetReader.ReadVector2();
        _simulationState.Velocity = packetReader.ReadVector2();
        _simulationState.TankRotation = packetReader.ReadSingle();
        _simulationState.TurretRotation = packetReader.ReadSingle();

        // Read remote inputs from the network packet.
        _tankInput = packetReader.ReadVector2();
        _turretInput = packetReader.ReadVector2();

        // Optionally apply prediction to compensate for
        // how long it took this packet to reach us.
        if (enablePrediction)
        {
            ApplyPrediction(gameTime, latency, packetSendTime);
        }
    }

    /// <summary>
    /// Applies smoothing by interpolating the display state somewhere
    /// in between the previous state and current simulation state.
    /// </summary>
    void ApplySmoothing()
    {
        _displayState.Position = Vector2.Lerp(_simulationState.Position, _previousState.Position, _currentSmoothing);

        _displayState.Velocity = Vector2.Lerp(_simulationState.Velocity, _previousState.Velocity, _currentSmoothing);

        _displayState.TankRotation = MathHelper.Lerp(_simulationState.TankRotation, _previousState.TankRotation, _currentSmoothing);

        _displayState.TurretRotation = MathHelper.Lerp(_simulationState.TurretRotation, _previousState.TurretRotation, _currentSmoothing);
    }

    /// <summary>
    /// Incoming network packets tell us where the tank was at the time the packet
    /// was sent. But packets do not arrive instantly! We want to know where the
    /// tank is now, not just where it used to be. This method attempts to guess
    /// the current state by figuring out how long the packet took to arrive, then
    /// running the appropriate number of local updates to catch up to that time.
    /// This allows us to figure out things like "it used to be over there, and it
    /// was moving that way while turning to the left, so assuming it carried on
    /// using those same inputs, it should now be over here".
    /// </summary>
    void ApplyPrediction(GameTime gameTime, TimeSpan latency, float packetSendTime)
    {
        // Work out the difference between our current local time
        // and the remote time at which this packet was sent.
        float localTime = (float)gameTime.TotalGameTime.TotalSeconds;

        float timeDelta = localTime - packetSendTime;

        // Maintain a rolling average of time deltas from the last 100 packets.
        _clockDelta.AddValue(timeDelta);

        // The caller passed in an estimate of the average network latency, which
        // is provided by the XNA Framework networking layer. But not all packets
        // will take exactly that average amount of time to arrive! To handle
        // varying latencies per packet, we include the send time as part of our
        // packet data. By comparing this with a rolling average of the last 100
        // send times, we can detect packets that are later or earlier than usual,
        // even without having synchronized clocks between the two machines. We
        // then adjust our average latency estimate by this per-packet deviation.

        float timeDeviation = timeDelta - _clockDelta.AverageValue;

        latency += TimeSpan.FromSeconds(timeDeviation);

        TimeSpan oneFrame = TimeSpan.FromSeconds(1.0 / 60.0);

        // Apply prediction by updating our simulation state however
        // many times is necessary to catch up to the current time.
        while (latency >= oneFrame)
        {
            UpdateState(ref _simulationState);

            latency -= oneFrame;
        }
    }

    /// <summary>
    /// Updates one of our state structures, using the current inputs to turn
    /// the tank, and applying the velocity and inertia calculations. This
    /// method is used directly to update locally controlled tanks, and also
    /// indirectly to predict the motion of remote tanks.
    /// </summary>
    void UpdateState(ref TankState state)
    {
        // Gradually turn the tank and turret to face the requested direction.
        state.TankRotation = TurnToFace(state.TankRotation, _tankInput, TANK_TURN_RATE);

        state.TurretRotation = TurnToFace(state.TurretRotation, _turretInput, TURRET_TURN_RATE);

        // How close the desired direction is the tank facing?
        Vector2 tankForward = new((float)Math.Cos(state.TankRotation), (float)Math.Sin(state.TankRotation));

        Vector2 targetForward = new(_tankInput.X, -_tankInput.Y);

        float facingForward = Vector2.Dot(tankForward, targetForward);

        // If we have finished turning, also start moving forward.
        if (facingForward > 0)
        {
            float speed = facingForward * facingForward * TANK_SPEED;

            state.Velocity += tankForward * speed;
        }

        // Update the position and velocity.
        state.Position += state.Velocity;
        state.Velocity *= TANK_FRICTION;

        // Clamp so the tank cannot drive off the edge of the screen.
        state.Position = Vector2.Clamp(state.Position, Vector2.Zero, _screenSize);
    }

    /// <summary>
    /// Gradually rotates the tank to face the specified direction.
    /// See the Aiming sample (creators.xna.com) for details.
    /// </summary>
    static float TurnToFace(float rotation, Vector2 target, float turnRate)
    {
        if (target == Vector2.Zero)
            return rotation;

        float angle = (float)Math.Atan2(-target.Y, target.X);

        float difference = rotation - angle;

        while (difference > MathHelper.Pi)
            difference -= MathHelper.TwoPi;

        while (difference < -MathHelper.Pi)
            difference += MathHelper.TwoPi;

        turnRate *= Math.Abs(difference);

        if (difference < 0)
            return rotation + Math.Min(turnRate, -difference);
        else
            return rotation - Math.Min(turnRate, difference);
    }

    public void Draw()
    {
        var spriteBatch = BaseGame.Instance.SpriteBatch;

        //Tank
        var size = new Vector2(32);

        Vector2 origin = new(Resources.TankTexture.Width / 2, Resources.TankTexture.Height / 2);

        spriteBatch.Draw(Resources.TankTexture, _displayState.Position, null, Color.White, _displayState.TankRotation, origin, 1, SpriteEffects.None, 0);

        //Turret
        spriteBatch.DrawLine(_displayState.Position, _displayState.Position + VectorHelper.Polar(_displayState.TurretRotation, size.X), Color.Red, thickness: 2);
    }
}
