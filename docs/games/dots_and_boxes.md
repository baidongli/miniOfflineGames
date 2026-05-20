# Dots and Boxes

Childhood paper-and-pen classic. 2-4 players take turns drawing edges
between dots. Complete the fourth side of a box and you claim it - and
**get another turn**.

## Rules

- Default grid: **5 x 5 boxes** (6 x 6 dots). Configurable.
- On your turn, draw any **horizontal or vertical edge** between two
  adjacent dots that hasn't been drawn yet.
- If your edge completes the 4th side of one or more boxes, those boxes
  become yours and **you go again**. A single move can claim **two
  boxes** at once when the edge sits between them.
- If your edge doesn't complete a box, the turn passes to the next
  player.
- Game ends when all boxes are claimed.
- Winner: most boxes. Ties draw.

## Controls

- Tap an undrawn edge. Long-press for preview / undo (UI sugar).

## Multiplayer (turn-based, 2-4 players)

Same orchestrator family as Connect Four / Reversi. Differences:
- Seat assignment is by **sorting all player ids alphabetically** -
  the lex-smallest id is seat 0, plays first.
- One `DBMoveMessage` per edge. The receiver applies it via the
  engine; whose turn it is afterward is fully determined by whether a
  box was completed (no extra "turn passes" flag needed on the wire).
- MoveNumber-tracked de-duplication.

## AI

`SimpleDotsAI` plays the obvious greedy moves:
1. **Capture** any box whose 4th edge is available.
2. **Avoid sacrifices** - never draw an edge that brings a box to
   exactly 3 sides (handing the opponent a free claim).
3. **Last resort** - pick any legal edge.

Doesn't do chain analysis (the strategic deep layer of D&B) so it loses
to a careful human, but it plays a respectable casual game.

## Files

- `Games/DotsAndBoxes/Scripts/Logic/` - board with hEdges / vEdges /
  owners, game with bonus-turn rule.
- `Games/DotsAndBoxes/Scripts/Multiplayer/` - turn-based wire protocol
  with deterministic seat assignment for 2-4 players.
- `Games/DotsAndBoxes/Scripts/AI/` - capture-then-avoid-3 heuristic.
- `Games/DotsAndBoxes/Scripts/DotsAndBoxesModule.cs` - IGameModule binding.
