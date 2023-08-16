// Copyright Pumpkin Games Ltd. All Rights Reserved.

using Microsoft.Xna.Framework;

namespace NakamaTank.NakamaMultiplayer.GamePhases;

/// <summary>
/// Base class for all the different game phases (e.g. main menu, playing the game, game over, etc).
/// </summary>
public abstract class GamePhase
{
    /// <summary>The number of ticks since the state was created.</summary>
    protected int ElapsedTicks;

    public virtual void Initialise() { }

    public void Create()
    {
        ElapsedTicks = 0;

        OnCreate();
    }

    public virtual bool SupportsPause => false;

    public void Update(GameTime gameTime)
    {
        ElapsedTicks++;

        OnUpdate(gameTime);
    }

    public void Draw(GameTime gameTime) => OnDraw(gameTime);

    public void Destroy() => OnDestroy();

    protected virtual void OnCreate() { }
    protected virtual void OnUpdate(GameTime gameTime) { }
    protected virtual void OnDraw(GameTime gameTime) { }
    protected virtual void OnDestroy() { }
}
