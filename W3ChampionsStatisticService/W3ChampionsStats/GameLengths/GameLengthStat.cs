using System;
using System.Collections.Generic;
using System.Linq;
using W3ChampionsStatisticService.CommonValueObjects;
using W3ChampionsStatisticService.ReadModelBase;

namespace W3ChampionsStatisticService.W3ChampionsStats.GameLengths
{
    public class GameLengthStat : IIdentifiable
    {
        public void Apply(GameMode gameMode, TimeSpan duration)
        {
            var gameLengthPerMode = GameLengths.Single(m => m.GameMode == gameMode);
            gameLengthPerMode.Record(duration);
        }

        public List<GameLengthPerMode> GameLengths { get; set; } = new List<GameLengthPerMode>();
        public string Id { get; set; } = nameof(GameLengthStat);

        public static GameLengthStat Create()
        {
            return new GameLengthStat
            {
                GameLengths = new List<GameLengthPerMode>
                {
                    new GameLengthPerMode
                    {
                        GameMode = GameMode.GM_1v1,
                        Lengths = CreateLengths()
                    },
                    new GameLengthPerMode
                    {
                        GameMode = GameMode.GM_2v2_AT,
                        Lengths = CreateLengths()
                    },
                    new GameLengthPerMode
                    {
                        GameMode = GameMode.GM_4v4,
                        Lengths = CreateLengths()
                    },
                    new GameLengthPerMode
                    {
                        GameMode = GameMode.FFA,
                        Lengths = CreateLengths()
                    },
                    new GameLengthPerMode
                    {
                        GameMode = GameMode.GM_2v2,
                        Lengths = CreateLengths()
                    },
                }
            };
        }

        private static List<GameLength> CreateLengths()
        {
            var lengths = new List<GameLength>();
            for (var i = 0; i <= 120; i++)
            {
                lengths.Add(new GameLength {passedTimeInSeconds = i * 30});
            }

            return lengths;
        }
    }
}