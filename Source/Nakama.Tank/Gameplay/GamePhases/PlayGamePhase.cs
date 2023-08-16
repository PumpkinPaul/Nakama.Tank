// Copyright Pumpkin Games Ltd. All Rights Reserved.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using NakamaTank.Engine;
using System;
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
        _networkGameManager.ReceivedRemotePlayerPosition += OnReceivedRemotePlayerPosition;
        _networkGameManager.ReceivedRemoteBallState += OnReceivedRemoteBallState;
        _networkGameManager.ReceivedRemoteScore += OnReceivedRemoteScore;
        _networkGameManager.RemovedPlayer += OnRemovedPlayer;
    }

    protected async override void OnUpdate()
    {
        base.OnUpdate();

        if (BaseGame.Instance.KeyboardState.IsKeyDown(Keys.Space) && BaseGame.Instance.PreviousKeyboardState.IsKeyUp(Keys.Space))
            await QuitMatch();

        //_ecsManager.Update();
    }

    protected override void OnDraw()
    {
        base.OnDraw();

        //_ecsManager.Draw();
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

    void OnSpawnedLocalPlayer(object sender, EventArgs e)
    {
        var position = _playerSpawnPoints[_playerSpawnPointsIdx];

        //_ecsManager.SpawnLocalPlayer(position, _bounceDirection);

        PrepareNextPlayer();
    }

    void OnSpawnedRemotePlayer(object sender, SpawnedRemotePlayerEventArgs e)
    {
        var position = _playerSpawnPoints[_playerSpawnPointsIdx];

        //_ecsManager.SpawnRemotePlayer(position, _bounceDirection);

        PrepareNextPlayer();
    }

    void PrepareNextPlayer()
    {
        //Cycle through the spawn points so that players are located in the correct postions and flipping the bounce direction
        _playerSpawnPointsIdx = (_playerSpawnPointsIdx + 1) % _playerSpawnPoints.Length;
        _bounceDirection = -_bounceDirection;
    }

    void OnReceivedRemotePlayerPosition(object sender, ReceivedRemotePlayerPositionEventArgs e)
    {
        //_ecsManager.ReceivedMatchDataVelocityAndPosition(e.Position, e.SessionId);
    }

    void OnReceivedRemoteBallState(object sender, ReceivedRemoteBallStateEventArgs e)
    {
        //_ecsManager.ReceivedMatchDataDirectionAndPosition(e.Direction, e.Position);
    }

    void OnReceivedRemoteScore(object sender, ReceivedRemoteScoreEventArgs e)
    {
        //_gameState.Player1Score = e.Player1Score;
        //_gameState.Player2Score = e.Player2Score;
    }

    void OnRemovedPlayer(object sender, RemovedPlayerEventArgs e)
    {
        //_ecsManager.DestroyEntity(e.SessionId);
    }
}
