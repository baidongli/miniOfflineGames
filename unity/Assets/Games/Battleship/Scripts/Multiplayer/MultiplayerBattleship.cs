using System;
using MiniGames.Games.Battleship.Logic;

namespace MiniGames.Games.Battleship.Multiplayer
{
    /// <summary>
    /// Battleship orchestrator: turn-based with HIDDEN STATE. Each peer
    /// keeps its OwnFleet private. The wire protocol is asymmetric:
    ///   shooter broadcasts ShotFired, target replies with ShotResult.
    /// The shooter's local state is updated only when ShotResult arrives -
    /// they never learn opponent positions speculatively.
    ///
    /// Seat assignment: lex-smaller PlayerId = seat 0 (opens first).
    /// </summary>
    public sealed class MultiplayerBattleship
    {
        public readonly BattleshipGame Game;
        public readonly string LocalPlayerId;
        public readonly string RemotePlayerId;
        public int LocalMoveCount { get; private set; }
        public int RemoteMoveCount { get; private set; }

        public event Action<BTLShipsReadyMessage>  ShipsReadyOutgoing;
        public event Action<BTLShotFiredMessage>   ShotFiredOutgoing;
        public event Action<BTLShotResultMessage>  ShotResultOutgoing;
        public event Action<BTLResignMessage>      ResignOutgoing;

        public event Action GameEnded;

        public MultiplayerBattleship(string localPlayerId, string remotePlayerId)
        {
            LocalPlayerId = localPlayerId;
            RemotePlayerId = remotePlayerId;
            bool localFirst = string.CompareOrdinal(localPlayerId, remotePlayerId) < 0;
            Game = new BattleshipGame(localSeat: localFirst ? (byte)0 : (byte)1);
            Game.PhaseChanged += () =>
            {
                if (Game.Phase == BattleshipPhase.GameOver) GameEnded?.Invoke();
            };
        }

        public bool DeclareReady()
        {
            if (!Game.DeclareMyFleetReady()) return false;
            ShipsReadyOutgoing?.Invoke(new BTLShipsReadyMessage { PlayerId = LocalPlayerId });
            return true;
        }

        public void OnShipsReadyReceived(BTLShipsReadyMessage m)
        {
            if (m.PlayerId == LocalPlayerId) return;
            Game.NoteOpponentFleetReady();
        }

        public bool TryLocalShoot(int x, int y)
        {
            if (!Game.TryShoot(x, y)) return false;
            LocalMoveCount++;
            ShotFiredOutgoing?.Invoke(new BTLShotFiredMessage
            {
                PlayerId = LocalPlayerId, X = x, Y = y, MoveNumber = LocalMoveCount
            });
            return true;
        }

        public void OnShotFiredReceived(BTLShotFiredMessage m)
        {
            if (m.PlayerId == LocalPlayerId) return;
            if (Game.Phase != BattleshipPhase.Playing) return;
            if (m.MoveNumber != RemoteMoveCount + 1) return;
            RemoteMoveCount++;

            // Resolve against my fleet.
            var result = Game.ProcessIncomingShot(m.X, m.Y, out var sunkKind, out var sunkCells);
            var reply = new BTLShotResultMessage
            {
                PlayerId = LocalPlayerId,
                X = m.X, Y = m.Y,
                Result = (byte)result,
                SunkKind = (byte)sunkKind,
                InResponseToMove = m.MoveNumber
            };
            if (sunkCells != null)
                foreach (var (cx, cy) in sunkCells)
                { reply.SunkCellsFlat.Add(cx); reply.SunkCellsFlat.Add(cy); }
            ShotResultOutgoing?.Invoke(reply);
        }

        public void OnShotResultReceived(BTLShotResultMessage m)
        {
            if (m.PlayerId == LocalPlayerId) return;
            if (Game.Phase != BattleshipPhase.Playing) return;
            var result = (ShotResult)m.Result;
            var sunkCells = new System.Collections.Generic.List<(int x, int y)>();
            for (int i = 0; i + 1 < m.SunkCellsFlat.Count; i += 2)
                sunkCells.Add((m.SunkCellsFlat[i], m.SunkCellsFlat[i + 1]));
            Game.RecordOpponentResult(m.X, m.Y, result, sunkCells, (ShipKind)m.SunkKind);
        }

        public void Resign()
        {
            ResignOutgoing?.Invoke(new BTLResignMessage { PlayerId = LocalPlayerId });
        }

        public void OnResignReceived(BTLResignMessage m)
        {
            // UI surfaces resignation; engine state untouched (game already over for the resigner).
        }
    }
}
