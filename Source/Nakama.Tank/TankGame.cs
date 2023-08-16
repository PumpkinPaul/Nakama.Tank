// Copyright Pumpkin Games Ltd. All Rights Reserved.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Nakama;
using Nakama.Tank.Engine;
using NakamaTank.Engine;
using NakamaTank.NakamaMultiplayer;
using NakamaTank.NakamaMultiplayer.GamePhases;

namespace NakamaTank;

/// <summary>
/// Very simple multiplayer implementation of the game, TANK using the Nakama framework.
/// </summary>
/// <remarks>
/// Basing a solution using the Nakama documentation...
/// https://dotnet.docs.heroiclabs.com/html/index.html
/// </remarks>
public class TankGame : BaseGame
{
    public readonly GamePhaseManager GamePhaseManager;

    PlayerProfile _playerProfile;

    //------------------------------------------------------------------------------------------------------------------------------------------------------ 
    //------------------------------------------------------------------------------------------------------------------------------------------------------ 
    //------------------------------------------------------------------------------------------------------------------------------------------------------ 
    //Multiplayer
    readonly NakamaConnection _nakamaConnection;
    readonly NetworkGameManager _networkGameManager;

    public TankGame()
    {
        Window.Title = "Nakama.Tank - Multiplayer";

        _playerProfile = PlayerProfile.LoadOrCreate(LocalApplicationDataPath, "playerProfile.json");

        _nakamaConnection = new NakamaConnection(_playerProfile);
        _networkGameManager = new NetworkGameManager(_nakamaConnection);

        GamePhaseManager = new GamePhaseManager();
        GamePhaseManager.Add(new MainMenuPhase(_nakamaConnection));
        GamePhaseManager.Add(new PlayGamePhase(_networkGameManager));

        // Show the main menu, hide the in-game menu when player quits the match
        GamePhaseManager.Get<PlayGamePhase>().ExitedMatch += (sender, e) => GamePhaseManager.ChangePhase<MainMenuPhase>();
    }

    protected async override void Initialize()
    {
        base.Initialize();

        await _networkGameManager.Connect();
        _nakamaConnection.Socket.ReceivedMatchmakerMatched += OnReceivedMatchmakerMatched;

        GamePhaseManager.Initialise();
        GamePhaseManager.ChangePhase<MainMenuPhase>();
    }

    /// <summary>
    /// Called when a MatchmakerMatched event is received from the Nakama server.
    /// </summary>
    /// <param name="matched">The MatchmakerMatched data.</param>
    public void OnReceivedMatchmakerMatched(IMatchmakerMatched matched)
    {
        Logger.WriteLine($"NakamaMultiplayerGame.OnReceivedMatchmakerMatched");
        Logger.WriteLine($"Changing game phase to begin a new play session");

        GamePhaseManager.ChangePhase<PlayGamePhase>();
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        if (KeyboardState.IsKeyDown(Keys.Escape) && PreviousKeyboardState.IsKeyUp(Keys.Escape))
            Exit();

        GamePhaseManager.Update();
    }

    protected override void OnDraw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        GamePhaseManager.Draw();
    }
}
