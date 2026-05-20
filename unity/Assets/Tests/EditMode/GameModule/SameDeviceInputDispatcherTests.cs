using System.Collections.Generic;
using MiniGames.GameModule.Input;
using NUnit.Framework;
using UnityEngine;

namespace MiniGames.Tests.GameModule
{
    public class SameDeviceInputDispatcherTests
    {
        private static readonly Vector2 ScreenSize = new Vector2(1000, 1000);

        [Test]
        public void Two_players_top_and_bottom_get_their_own_touches()
        {
            var d = new SameDeviceInputDispatcher(playerCount: 2, screenSize: ScreenSize);
            var touches = new List<SameDeviceInputDispatcher.TouchSample>
            {
                new SameDeviceInputDispatcher.TouchSample {
                    Id = 1, Position = new Vector2(500, 200), Began = true
                },
                new SameDeviceInputDispatcher.TouchSample {
                    Id = 2, Position = new Vector2(500, 800), Began = true
                }
            };

            d.Tick(touches);

            Assert.IsTrue(d.Inputs[0].TouchActive, "P1 (bottom) should see its touch");
            Assert.AreEqual(200f, d.Inputs[0].TouchPosition.y);
            Assert.IsTrue(d.Inputs[1].TouchActive, "P2 (top) should see its touch");
            Assert.AreEqual(800f, d.Inputs[1].TouchPosition.y);
        }

        [Test]
        public void Touch_started_in_a_region_keeps_ownership_when_it_drags_across()
        {
            var d = new SameDeviceInputDispatcher(playerCount: 2, screenSize: ScreenSize);

            // Begin in bottom (P1).
            d.Tick(new[]
            {
                new SameDeviceInputDispatcher.TouchSample {
                    Id = 7, Position = new Vector2(500, 200), Began = true
                }
            });
            Assert.IsTrue(d.Inputs[0].TouchActive);
            Assert.IsFalse(d.Inputs[1].TouchActive);

            // Drag into top region (would otherwise belong to P2).
            d.Tick(new[]
            {
                new SameDeviceInputDispatcher.TouchSample {
                    Id = 7, Position = new Vector2(500, 900), Began = false
                }
            });
            Assert.IsTrue(d.Inputs[0].TouchActive, "ownership should stay with P1");
            Assert.IsFalse(d.Inputs[1].TouchActive);
        }

        [Test]
        public void Touch_end_releases_ownership()
        {
            var d = new SameDeviceInputDispatcher(playerCount: 2, screenSize: ScreenSize);

            d.Tick(new[]
            {
                new SameDeviceInputDispatcher.TouchSample {
                    Id = 3, Position = new Vector2(500, 200), Began = true, Ended = true
                }
            });

            // Next frame: empty input -> nobody active.
            d.Tick(new SameDeviceInputDispatcher.TouchSample[0]);
            Assert.IsFalse(d.Inputs[0].TouchActive);
            Assert.IsFalse(d.Inputs[1].TouchActive);
        }

        [Test]
        public void Four_players_split_into_quadrants()
        {
            var d = new SameDeviceInputDispatcher(playerCount: 4, screenSize: ScreenSize);
            d.Tick(new[]
            {
                new SameDeviceInputDispatcher.TouchSample { Id = 1, Position = new Vector2(200, 200), Began = true },
                new SameDeviceInputDispatcher.TouchSample { Id = 2, Position = new Vector2(800, 200), Began = true },
                new SameDeviceInputDispatcher.TouchSample { Id = 3, Position = new Vector2(200, 800), Began = true },
                new SameDeviceInputDispatcher.TouchSample { Id = 4, Position = new Vector2(800, 800), Began = true }
            });

            for (int i = 0; i < 4; i++)
                Assert.IsTrue(d.Inputs[i].TouchActive, $"player {i} should be active");
        }
    }
}
