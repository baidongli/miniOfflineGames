using System;
using System.Collections.Generic;

namespace MiniGames.Games.Battleship.Logic
{
    /// <summary>
    /// One peer's view of a Battleship game. Holds two boards:
    ///  - OwnFleet: this player's ships (PRIVATE - not broadcast).
    ///  - OpponentTracker: built up from incoming shot results.
    ///
    /// The orchestrator coordinates the asymmetric protocol:
    ///   shooter: TryShoot() -> emit ShotFired wire message.
    ///   target:  ProcessIncomingShot() -> emit ShotResult wire message.
    ///   shooter: RecordOpponentResult() -> update OpponentTracker.
    /// </summary>
    public sealed class BattleshipGame
    {
        public BattleshipBoard OwnFleet { get; } = new BattleshipBoard();
        public BattleshipBoard OpponentTracker { get; } = new BattleshipBoard();

        public BattleshipPhase Phase { get; private set; } = BattleshipPhase.Setup;
        public byte LocalSeat { get; }
        public byte CurrentTurnSeat { get; private set; }
        public bool MyFleetReady { get; private set; }
        public bool OpponentFleetReady { get; private set; }
        public byte? Winner { get; private set; }

        public event Action PhaseChanged;
        public event Action<int, int, ShotResult> MyShotResolved;
        public event Action<int, int, ShotResult> IncomingShotResolved;

        public BattleshipGame(byte localSeat)
        {
            LocalSeat = localSeat;
            CurrentTurnSeat = 0;  // seat 0 always opens
        }

        // --- Setup phase ---

        public bool TryPlaceShip(ShipKind kind, int x, int y, ShipOrientation o)
        {
            if (Phase != BattleshipPhase.Setup) return false;
            return OwnFleet.TryPlaceShip(kind, x, y, o);
        }

        public bool DeclareMyFleetReady()
        {
            if (Phase != BattleshipPhase.Setup) return false;
            if (OwnFleet.PlacedShipCount < Fleet.Standard.Length) return false;
            if (MyFleetReady) return false;
            MyFleetReady = true;
            CheckStartPlaying();
            return true;
        }

        public void NoteOpponentFleetReady()
        {
            if (Phase != BattleshipPhase.Setup) return;
            OpponentFleetReady = true;
            CheckStartPlaying();
        }

        private void CheckStartPlaying()
        {
            if (MyFleetReady && OpponentFleetReady)
            {
                Phase = BattleshipPhase.Playing;
                PhaseChanged?.Invoke();
            }
        }

        // --- Playing phase ---

        public bool IsLocalTurn => Phase == BattleshipPhase.Playing && CurrentTurnSeat == LocalSeat;

        /// <summary>Local player chooses to shoot at (x, y) on opponent's grid. Validates only.</summary>
        public bool TryShoot(int x, int y)
        {
            if (!IsLocalTurn) return false;
            if (!OpponentTracker.InBounds(x, y)) return false;
            if (OpponentTracker.ShotAt(x, y)) return false;
            return true;
        }

        /// <summary>Target side: incoming opponent shot is resolved against OwnFleet.</summary>
        public ShotResult ProcessIncomingShot(int x, int y, out ShipKind sunkKind, out List<(int x, int y)> sunkCells)
        {
            sunkKind = 0;
            sunkCells = null;
            if (Phase != BattleshipPhase.Playing) return ShotResult.Miss;
            var result = OwnFleet.RegisterIncomingShot(x, y, out sunkKind, out sunkCells);
            IncomingShotResolved?.Invoke(x, y, result);

            // After processing, pass the turn to the shooter (if they
            // happened to be us - this code path runs on the target).
            // Turn already belongs to the OTHER seat; nothing to flip here.
            // But: check if this shot finished off our fleet.
            if (OwnFleet.AllShipsSunk())
            {
                Phase = BattleshipPhase.GameOver;
                Winner = (byte)(1 - LocalSeat);   // the OTHER player won
                PhaseChanged?.Invoke();
            }
            return result;
        }

        /// <summary>Shooter side: opponent told us the result of our shot. Update tracker, advance turn.</summary>
        public void RecordOpponentResult(int x, int y, ShotResult result, IList<(int x, int y)> sunkCells, ShipKind sunkKind)
        {
            OpponentTracker.RecordShotResult(x, y, result);
            if (result == ShotResult.Sunk && sunkCells != null)
                OpponentTracker.RecordSunkShip(sunkCells, sunkKind);
            MyShotResolved?.Invoke(x, y, result);

            // One shot per turn (classic Battleship rules); pass the turn.
            if (Phase == BattleshipPhase.Playing)
            {
                CurrentTurnSeat = (byte)(1 - CurrentTurnSeat);
                // Final-shot-game-over is signaled by opponent's GameOver
                // message OR by us inferring all-sunk on our OpponentTracker
                // (every opp ship's cells are now marked).
                if (AllOpponentShipsTracked())
                {
                    Phase = BattleshipPhase.GameOver;
                    Winner = LocalSeat;
                    PhaseChanged?.Invoke();
                }
            }
        }

        private bool AllOpponentShipsTracked()
        {
            // We've sunk all of opponent's ships when our tracker has the
            // full Fleet.TotalCells worth of confirmed sunk cells.
            int sunkCells = 0;
            for (int y = 0; y < BattleshipBoard.Size; y++)
                for (int x = 0; x < BattleshipBoard.Size; x++)
                {
                    byte v = OpponentTracker.ShipAt(x, y);
                    // RecordSunkShip wrote the real ShipKind byte; sentinel 255
                    // means "hit unknown" (not yet sunk).
                    if (v >= 1 && v <= 5) sunkCells++;
                }
            return sunkCells >= Fleet.TotalCells;
        }
    }
}
