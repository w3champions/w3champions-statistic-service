﻿using System;
using System.Linq;
using System.Threading.Tasks;
using W3ChampionsStatisticService.Extensions;
using W3ChampionsStatisticService.Matches;
using W3ChampionsStatisticService.PadEvents;
using W3ChampionsStatisticService.Ports;
using W3ChampionsStatisticService.ReadModelBase;

namespace W3ChampionsStatisticService.W3ChampionsStats.HeroWinrate
{
    public class HeroWinRatePerHeroModelHandler : IReadModelHandler
    {
        private readonly IW3StatsRepo _w3Stats;

        public HeroWinRatePerHeroModelHandler(
            IW3StatsRepo w3Stats
            )
        {
            _w3Stats = w3Stats;
        }

        public async Task Update(MatchFinishedEvent nextEvent)
        {
            if (nextEvent.result == null
                || nextEvent.match.gameMode != GameMode.GM_1v1
                || nextEvent.match.players.All(p => p.won)
                || nextEvent.match.players.All(p => !p.won)
                || nextEvent.result.players.Any(p => p.heroes.Count == 0)) return;

            var heroComboIdWinner = ExtractHeroComboId(nextEvent, p => p.won);
            var heroComboIdLooser = ExtractHeroComboId(nextEvent, p => !p.won);

            await UpdateStat(heroComboIdWinner, heroComboIdLooser, true);
            await UpdateStat(heroComboIdLooser, heroComboIdWinner, false);
        }

        private static string ExtractHeroComboId(MatchFinishedEvent nextEvent, Func<PlayerMMrChange, bool> func)
        {
            var winner = nextEvent.match.players.Single(func);
            var winnerHeroes = nextEvent.result.players.Single(p => p.battleTag == winner.battleTag).heroes;
            var heroComboIdWinner = string.Join("_", winnerHeroes.Select(h => h.icon.ParseReforgedName()));
            return heroComboIdWinner;
        }

        private async Task UpdateStat(string heroComboIdWinner, string heroComboIdLooser, bool won)
        {
            var winnerWinrate = await _w3Stats.LoadHeroWinrate(heroComboIdWinner) ??
                                HeroWinRatePerHero.Create(heroComboIdWinner);
            winnerWinrate.RecordGame(won, heroComboIdLooser);
            await _w3Stats.Save(winnerWinrate);
        }
    }
}