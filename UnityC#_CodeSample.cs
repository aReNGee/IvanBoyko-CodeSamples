/// <summary>
///     This file contains a sample of the original draft of the Unity C# code used to move the player's ship in the Android game "Drill Down".
///     Namespaces have been omitted but a selection of variable declarations have been retained to showcase naming schemes and formatting.
///     The code enters at PlayerRequestsToMove() and moves into to MovePlayerInDirection() and then finally EndOfPlayerMovement().
///
///     This code is for illustration purposes only and cannot be executed.
/// </summary>

public class PlayerController : MonoBehaviour
{
    // ########################################
    // Variables.
    // ########################################

    #region Variables

    public enum MovementDirection
    {
        Left,
        Right,
        Down
    }

    public static PlayerController Instance;

    // 0 is the leftmost cell, 4 is the rightmost.
    private int _indexOfPlayerLocation = 2;
    public int IndexOfPlayerLocation
    {
        get { return _indexOfPlayerLocation; }
    }

    #endregion // Variables.

    // ########################################
    // Methods.
    // ########################################

    #region Methods

    /// <summary>
    ///     Method that checks whether or not the player is able to move in the chosen direction.
    /// </summary>
    private void PlayerRequestsToMove(MovementDirection moveDir)
    {
        if (_inputDisabled) return;
        if (moveDir == PlayerController.MovementDirection.Left)
        {
            if (_indexOfPlayerLocation == 0)
            {
                return;
            }
            MovePlayerInDirection(MovementDirection.Left);
        }
        else if (moveDir == PlayerController.MovementDirection.Right)
        {
            if (_indexOfPlayerLocation == 4)
            {
                return;
            }
            MovePlayerInDirection(MovementDirection.Right);
        }
        else if (moveDir == PlayerController.MovementDirection.Down)
        {
            MovePlayerInDirection(MovementDirection.Down);
        }
        else
        {
            return;
        }
    }

    /// <summary>
    ///     Method that actually moves the player in the chosen direction.
    /// </summary>
    private void MovePlayerInDirection(MovementDirection moveDir)
    {
        // We disable input while the player moves - no double moves!
        // Turn off the buttons so the player doesn't try to click twice.
        ToggleButtonInteractibility(false);

        // Find out the tile you're moving towrds.
        Tile tileWeAreMovingOnto = WorldController.Instance.WhatTileIsAdjacentToPlayerInTheFollowingDirection(moveDir, _indexOfPlayerLocation);

        // We will eventually be charging fuel and damaging the hull etc.
        // First, calculate how long it will take to drill through the given material.
        float timeToDrill = tileWeAreMovingOnto.Hardness / AssetController.Instance.DrillAsset.DrillDictionary[_playerDrillLevel].DrillStrength;

        // Always takes a set time to move onto the station.
        if (tileWeAreMovingOnto.SuperType == TileData.TileSuperType.Station)
        {
            timeToDrill = 1;
        }

        // There is a minimum time required no matter how fast your drill is.
        if (timeToDrill < 0.25f)
        {
            timeToDrill = 0.25f;
        }

        // There is a "slow max" time required where no matter how long it "should" take, it will take the same amount of time.
        // There is also a "fast max" time for things that are faster than the slow max time.
        // However, this does not affect the fuel impact of the drilling.

        float fuelCostTime = timeToDrill;

        if (timeToDrill > 4f)
        {
            timeToDrill = 2.0f;
        }
        else if (timeToDrill > 1.5f)
        {
            timeToDrill = 1.5f;
        }

        // New formula for fuel - subtract hardness multiplied by timeToDrill if timeToDrill is over 1.
        float fuelCostToMove = tileWeAreMovingOnto.Hardness * fuelCostTime;
        if (fuelCostToMove < tileWeAreMovingOnto.Hardness)
        {
            fuelCostToMove = tileWeAreMovingOnto.Hardness;
        }

        bool outOfFuel = false;

        if (!_playerFuelTank.SpendFuel(fuelCostToMove))
        {
            // We ran out of fuel, game over.
            AudioController.Instance.PlaySoundEffect(AudioController.SoundEffectType.OutOfFuel);
            outOfFuel = true;
        }
        else
        {
            // Drill sound FX.
            // Our base sound lasts 2.0 seconds, so we will need to change the pitch to match the duration.
            // pitch = timetodrill / 0.5f;
            // No drill sound effect for station or empty tiles
            if (tileWeAreMovingOnto.SuperType == TileData.TileSuperType.Station)
            {
                AudioController.Instance.PlaySoundEffect(AudioController.SoundEffectType.Station);
            }
            else if (tileWeAreMovingOnto.SuperType == TileData.TileSuperType.Empty)
            {
                AudioController.Instance.PlaySoundEffect(AudioController.SoundEffectType.EmptyTile);
            }
            else
            {
                AudioController.Instance.PlaySoundEffect(AudioController.SoundEffectType.Drill, 0.5f / timeToDrill);
            }
        }

        if (!outOfFuel)
        {
            // Visually deplete the fuel gauge. Uses timeToDrill so it visually syncs up with the drilling motion.
            _fuelTankImage.DOFillAmount(_playerFuelTank.Capacity / _playerFuelTank.MaxCapacity, timeToDrill);

            // Set the color of the fuel tank so the player knows at a glance how close they are to defeat
            float percentage = (float)_playerFuelTank.Capacity / (float)_playerFuelTank.MaxCapacity;
            float hueToUse = 120.0f * percentage / 360.0f;
            Color colorOfThisText = Color.HSVToRGB(hueToUse, 0.85f, 0.76f);
            _colorOfPlayerFuelTank = colorOfThisText;
            _fuelTankImage.DOColor(_colorOfPlayerFuelTank, timeToDrill);

            GameController.Instance.SetFuel((int)_playerFuelTank.Capacity, (int)_playerFuelTank.MaxCapacity, timeToDrill);

            // Next, actually move the player.
            _transform.DOMove(tileWeAreMovingOnto.transform.position, timeToDrill).SetEase(Ease.OutSine).OnComplete(() => EndOfPlayerMovement(moveDir));
        }
        else
        {
            // Visually deplete the fuel gauge entirely
            _fuelTankImage.DOFillAmount(0, timeToDrill);

            // Set the color of the fuel tank so the player knows at a glance that they are defeated)
            Color colorOfThisText = Color.HSVToRGB(0, 0.85f, 0.76f);
            _colorOfPlayerFuelTank = colorOfThisText;
            _fuelTankImage.DOColor(_colorOfPlayerFuelTank, timeToDrill);

            GameController.Instance.SetFuel(0, (int)_playerFuelTank.MaxCapacity, timeToDrill);

            // Next, actually move the player, but only move them halfway because they failed to make it to the next tile.
            _transform.DOMove((tileWeAreMovingOnto.transform.position + _transform.position) / 2, timeToDrill).SetEase(Ease.OutSine).OnComplete(() => EndOfPlayerMovement(moveDir));
        }

        /// End Fuel
        if (moveDir == MovementDirection.Down)
        {
            if (!outOfFuel)
            {
                // If we are moving down, we also need to move the camera.
                _mainCameraTransform.DOMoveY(-1.0f, timeToDrill).SetEase(Ease.OutSine).SetRelative(true);

                // Player has increased their current depth, move them 10m down.
                GameController.Instance.AddScore(10, timeToDrill);
                // Show the player getting closer
                WorldController.Instance.MoveDown(10, timeToDrill);
            }
            else
            {
                // If we are moving down, we also need to move the camera.
                _mainCameraTransform.DOMoveY(-0.5f, timeToDrill).SetEase(Ease.OutSine).SetRelative(true);

                // Player has increased their current depth, move them 10m down.
                GameController.Instance.AddScore(5, timeToDrill);
                // Show the player getting closer
                WorldController.Instance.MoveDown(5, timeToDrill);
            }
        }
        else
        {
            // Still need to change the color of the text as the amount of fuel changes.
            // Not the world's cleanest workaround, but seems to work.
            WorldController.Instance.MoveDown(0, timeToDrill);
        }

        // Don't perform drill movement when moving onto a station.
        if (tileWeAreMovingOnto.SuperType != TileData.TileSuperType.Station && tileWeAreMovingOnto.SuperType != TileData.TileSuperType.Empty)
        {
            // We also need to move the drill at the same time. First, we'll rotate the drill in the direction its supposed to go.
            if (moveDir == PlayerController.MovementDirection.Left)
            {
                _drillTransform.rotation = Quaternion.Euler(new Vector3(0.0f, 0.0f, -90.0f));
                _drillTransform.DOPunchPosition(new Vector3(-0.5f, 0.0f, 0.0f), timeToDrill, 0).SetRelative(true).SetEase(Ease.InSine);
                _drillSpriteTransform.DOPunchPosition(new Vector3(0.1f, 0.0f, 0.0f), timeToDrill, ShakeDrillStrength(timeToDrill)).SetRelative(true).SetEase(Ease.Linear);
                //_drillTransform.do
            }
            else if (moveDir == PlayerController.MovementDirection.Right)
            {
                _drillTransform.rotation = Quaternion.Euler(new Vector3(0.0f, 0.0f, 90.0f));
                _drillTransform.DOPunchPosition(new Vector3(0.5f, 0.0f, 0.0f), timeToDrill, 0).SetRelative(true).SetEase(Ease.InSine);
                _drillSpriteTransform.DOPunchPosition(new Vector3(0.1f, 0.0f, 0.0f), timeToDrill, ShakeDrillStrength(timeToDrill)).SetRelative(true).SetEase(Ease.Linear);
            }
            else if (moveDir == PlayerController.MovementDirection.Down)
            {
                _drillTransform.rotation = Quaternion.Euler(new Vector3(0.0f, 0.0f, 0.0f));
                _drillTransform.DOPunchPosition(new Vector3(0.0f, -0.5f, 0.0f), timeToDrill, 0).SetRelative(true).SetEase(Ease.InSine);
                _drillSpriteTransform.DOPunchPosition(new Vector3(0.1f, 0.0f, 0.0f), timeToDrill, ShakeDrillStrength(timeToDrill)).SetRelative(true).SetEase(Ease.Linear);
            }
        }
    }

    /// <summary>
    ///     Method that completes the move, updates everything, and re enables player input.
    /// </summary>
    private void EndOfPlayerMovement(MovementDirection moveDir)
    {
        if (_playerFuelTank.Capacity > 0.0f)
        {
            // Let the worldcontroller know we moved to keep everything in sync.
            WorldController.Instance.PlayerMovedInTheFollowingDirection(moveDir, _indexOfPlayerLocation);

            // Shift the player's index.
            if (moveDir == PlayerController.MovementDirection.Left)
            {
                _indexOfPlayerLocation--;
            }
            else if (moveDir == PlayerController.MovementDirection.Right)
            {
                _indexOfPlayerLocation++;
            }

            // Apply radar to relevant tiles.
            WorldController.Instance.ApplyRadarToAllTiles();

            // Reenable input if we still have fuel.            
            ToggleButtonInteractibility(true);
        }
        else
        {
            ToggleButtonInteractibility(false);
            // We don't toggle radar for GameOver
            WorldController.Instance.GameOver();
        }
    }

    /// <summary>
    ///     Method that uses the time it will take to complete drilling to determine how much to shake the drill.
    /// </summary>
    private int ShakeDrillStrength(float timeToDrill)
    {
        // Below a certain threshhold, the drill doesn't shake at all
        // Below another threshold and above a certain threshhold, the drill shakes slowly
        // Between those two thresholds, the drill shakes rapidly

        float doesntShake = 0.2f;
        float shakesALittle = 0.5f;
        float shakesALot = 1.0f;

        int weakShake = 5;
        int strongShake = 10;

        if (timeToDrill <= doesntShake)
        {
            // Do nothing.
            return 0;
        }
        else if (timeToDrill <= shakesALittle)
        {
            return weakShake;
        }
        else if (timeToDrill <= shakesALot)
        {
            return strongShake;
        }
        else
        {
            return weakShake;
        }

    #endregion // Methods.
}