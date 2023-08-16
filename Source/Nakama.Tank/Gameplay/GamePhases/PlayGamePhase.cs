// Copyright Pumpkin Games Ltd. All Rights Reserved.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NakamaTank.Engine;
using NakamaTank.Engine.Extensions;
using NakamaTank.NakamaMultiplayer.Players;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NakamaTank.NakamaMultiplayer.GamePhases;

/// <summary>
/// Playing the game phase
/// </summary>
/// <remarks>
/// Player paddles are moving, ball is bouncing, all the bits that make up the gameplay part of the game.
/// </remarks>
public class PlayGamePhase : GamePhase
{
    //Multiplayer networking
    readonly NetworkGameManager _networkGameManager;

    //------------------------------------------------------------------------------------------------------------------------------------------------------ 
    //------------------------------------------------------------------------------------------------------------------------------------------------------ 
    //------------------------------------------------------------------------------------------------------------------------------------------------------ 
    //Gameplay
    public event EventHandler ExitedMatch;

    const int PLAYER_OFFSET_X = 32;

    readonly Vector2[] _playerSpawnPoints = new[] {
        new Vector2(PLAYER_OFFSET_X, BaseGame.SCREEN_HEIGHT / 2),
        new Vector2(BaseGame.SCREEN_WIDTH - PLAYER_OFFSET_X, BaseGame.SCREEN_HEIGHT / 2)
    };

    int _playerSpawnPointsIdx = 0;
    int _bounceDirection = -1;

    const int MAX_GAMERS = 2;
    const int MAX_LOCAL_GAMERS = 1;

    // What kind of network latency and packet loss are we simulating?
    enum NetworkQuality
    {
        Typical,    // 100 ms latency, 10% packet loss
        Poor,       // 200 ms latency, 20% packet loss
        Perfect,    // 0 latency, 0% packet loss
    }

    NetworkQuality networkQuality;

    // How often should we send network packets?
    int framesBetweenPackets = 6;

    // How recently did we send the last network packet?
    int framesSinceLastSend;

    // Is prediction and/or smoothing enabled?
    bool enablePrediction = true;
    bool enableSmoothing = true;

    readonly Dictionary<Player, Tank> _tanks = new();

    readonly Queue<ReceivedRemotePlayerTankStateEventArgs> _networkState = new();

    public PlayGamePhase(
        NetworkGameManager networkGameManager)
    {
        _networkGameManager = networkGameManager;
    }

    public override void Initialise()
    {
        base.Initialise();

        _networkGameManager.SpawnedLocalPlayer += OnSpawnedLocalPlayer;
        _networkGameManager.SpawnedRemotePlayer += OnSpawnedRemotePlayer;
        _networkGameManager.ReceivedRemotePlayerTasnkState += OnReceivedRemotePlayerTankStatePosition;
        _networkGameManager.RemovedPlayer += OnRemovedPlayer;
    }

    protected async override void OnUpdate(GameTime gameTime)
    {
        base.OnUpdate(gameTime);

        if (BaseGame.Instance.KeyboardState.IsKeyDown(Keys.Space) && BaseGame.Instance.PreviousKeyboardState.IsKeyUp(Keys.Space))
            await QuitMatch();

        //TODO: wait for both players!
        if (_tanks.Count == 2)
             UpdateNetworkSession(gameTime);
    }

    void UpdateNetworkSession(GameTime gameTime)
    {
        // Is it time to send outgoing network packets?
        bool sendPacketThisFrame = false;

        framesSinceLastSend++;

        if (framesSinceLastSend >= framesBetweenPackets)
        {
            sendPacketThisFrame = true;
            framesSinceLastSend = 0;
        }

        var localPlayers = _networkGameManager.Players.Values
            .Where(p => p.GetType() == typeof(LocalPlayer))
            .Select(p => (LocalPlayer)p);

        var remotePlayers = _networkGameManager.Players.Values
            .Where(p => p.GetType() == typeof(RemotePlayer))
            .Select(p => (RemotePlayer)p);

        // Update our locally controlled tanks, sending their latest state at periodic intervals.
        foreach (var player in localPlayers)
            UpdateLocalGamer(player, gameTime, sendPacketThisFrame);

        // Pump the underlying session object.
        try
        {
            //networkSession.Update();
        }
        catch (Exception e)
        {
            //errorMessage = e.Message;
            //networkSession.Dispose();
            //networkSession = null;
        }

        // Make sure the session has not ended.
        //if (networkSession == null)
        //    return;

        // Read any packets telling us the state of remotely controlled tanks.
        foreach (var player in remotePlayers)
            ReadIncomingPackets(player, gameTime);

        // Apply prediction and smoothing to the remotely controlled tanks.
        foreach (var player in remotePlayers)
            _tanks[player].UpdateRemote(framesBetweenPackets, enablePrediction);

        //Update the latency and packet loss simulation options.
        //TODO
        //UpdateOptions();
    }

    /// <summary>
    /// Helper for updating a locally controlled gamer.
    /// </summary>
    void UpdateLocalGamer(LocalPlayer player, GameTime gameTime, bool sendPacketThisFrame)
    {
        // Look up what tank is associated with this local player.
        Tank tank = _tanks[player];

        // Read the inputs controlling this tank.
        var playerIndex = player.PlayerIndex;

        ReadTankInputs(playerIndex, out Vector2 tankInput, out Vector2 turretInput);

        // Update the tank.
        tank.UpdateLocal(tankInput, turretInput);

        // Periodically send our state to everyone in the session.
        if (sendPacketThisFrame)
        {
            tank.WriteNetworkPacketJson(gameTime, out var packet);

            _networkGameManager.SendMatchState(OpCodes.TANK_PACKET, packet);
        }
    }

    /// <summary>
    /// Helper for reading incoming network packets.
    /// </summary>
    void ReadIncomingPackets(Player player, GameTime gameTime)
    {
        // Keep reading as long as incoming packets are available.
        while (_networkState.Count > 0)
        {
            var state = _networkState.Dequeue();

            // Look up the tank associated with whoever sent this packet.
            Tank tank = _tanks[player];

            // Estimate how long this packet took to arrive.
            //TODO! latency!!!
            //TimeSpan latency = networkSession.SimulatedLatency + TimeSpan.FromTicks(sender.RoundtripTime.Ticks / 2);
            var latency = TimeSpan.FromSeconds(1 / 20.0f);

            // Read the state of this tank from the network packet.
            tank.ReadNetworkPacketEvent(gameTime, state, latency, enablePrediction, enableSmoothing);
        }
    }

    /// <summary>
    /// Reads input data from keyboard and gamepad, and returns
    /// this as output parameters ready for use by the tank update.
    /// </summary>
    static void ReadTankInputs(PlayerIndex playerIndex, out Vector2 tankInput, out Vector2 turretInput)
    {
        // Read the gamepad.
        GamePadState gamePad = GamePad.GetState(playerIndex);

        tankInput = gamePad.ThumbSticks.Left;
        turretInput = gamePad.ThumbSticks.Right;

        //Invert sticks as our world origin is bottom left.
        tankInput.Y = -tankInput.Y;
        turretInput.Y = -turretInput.Y;

        // Read the keyboard.
        KeyboardState keyboard = Keyboard.GetState(playerIndex);

        if (keyboard.IsKeyDown(Keys.Left))
            tankInput.X = -1;
        else if (keyboard.IsKeyDown(Keys.Right))
            tankInput.X = 1;

        //Invert keyboard y inputs too.
        if (keyboard.IsKeyDown(Keys.Up))
            tankInput.Y = -1;
        else if (keyboard.IsKeyDown(Keys.Down))
            tankInput.Y = 1;

        if (keyboard.IsKeyDown(Keys.K))
            turretInput.X = -1;
        else if (keyboard.IsKeyDown(Keys.OemSemicolon))
            turretInput.X = 1;

        if (keyboard.IsKeyDown(Keys.O))
            turretInput.Y = -1;
        else if (keyboard.IsKeyDown(Keys.L))
            turretInput.Y = 1;

        // Normalize the input vectors.
        if (tankInput.Length() > 1)
            tankInput.Normalize();

        if (turretInput.Length() > 1)
            turretInput.Normalize();
    }

    protected override void OnDraw(GameTime gameTime)
    {
        base.OnDraw(gameTime);

        if (_tanks.Count == 2)
        {
            var spriteBatch = BaseGame.Instance.SpriteBatch;

            //Draw the world
            spriteBatch.Begin(0, BlendState.AlphaBlend, null, null, RasterizerState.CullClockwise, BaseGame.Instance.BasicEffect);

            //...all the entities
            foreach (var tank in _tanks.Values)
                tank.Draw();

            spriteBatch.End();

            var centreX = BaseGame.SCREEN_WIDTH * 0.5f;

            //Draw the UI
            spriteBatch.BeginTextRendering();
            spriteBatch.DrawText(Resources.SmallFont, "Destroy all opponents!", new Vector2(centreX, BaseGame.SCREEN_HEIGHT - 32), Color.White, Alignment.Centre);
            spriteBatch.End();
        }
    }

    /// <summary>
    /// Quits the current match.
    /// </summary>
    public async Task QuitMatch()
    {
        Logger.WriteLine($"PlayGamePhase.QuitMatch");

        await _networkGameManager.QuitMatch();

        ExitedMatch?.Invoke(this, EventArgs.Empty);
    }

    void OnSpawnedLocalPlayer(object sender, SpawnedPlayerEventArgs e)
    {
        var position = _playerSpawnPoints[_playerSpawnPointsIdx];

        Logger.WriteLine($"PlayGamePhase.OnSpawnedLocalPlayer - create local tank at position: {position}");
        _tanks[e.Player] = new Tank(position);

        PrepareNextPlayer();
    }

    void OnSpawnedRemotePlayer(object sender, SpawnedPlayerEventArgs e)
    {
        var position = _playerSpawnPoints[_playerSpawnPointsIdx];

        Logger.WriteLine($"PlayGamePhase.OnSpawnedRemotePlayer - create remote tank at position: {position}");
        _tanks[e.Player] = new Tank(position);

        PrepareNextPlayer();
    }

    void PrepareNextPlayer()
    {
        //Cycle through the spawn points so that players are located in the correct postions and flipping the bounce direction
        _playerSpawnPointsIdx = (_playerSpawnPointsIdx + 1) % _playerSpawnPoints.Length;
        _bounceDirection = -_bounceDirection;
    }

    void OnReceivedRemotePlayerTankStatePosition(object sender, ReceivedRemotePlayerTankStateEventArgs e)
    {
        _networkState.Enqueue(e);
    }

    void OnRemovedPlayer(object sender, RemovedPlayerEventArgs e)
    {
        //_ecsManager.DestroyEntity(e.SessionId);
    }
}
