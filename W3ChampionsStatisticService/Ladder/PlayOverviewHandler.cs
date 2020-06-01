﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using W3ChampionsStatisticService.CommonValueObjects;
using W3ChampionsStatisticService.PadEvents;
using W3ChampionsStatisticService.Ports;
using W3ChampionsStatisticService.ReadModelBase;

namespace W3ChampionsStatisticService.Ladder
{
    public class PlayOverviewHandler : IReadModelHandler
    {
        private readonly IPlayerRepository _playerRepository;

        public PlayOverviewHandler(
            IPlayerRepository playerRepository
            )
        {
            _playerRepository = playerRepository;
        }

        public async Task Update(MatchFinishedEvent nextEvent)
        {
            var winners = nextEvent.match.players.Where(p => p.won).ToList();
            var losers = nextEvent.match.players.Where(p => !p.won).ToList();

            if (winners.Count == 0 || losers.Count == 0)
            {
                // We should log the bad event here
                return;
            }

            // for broken events
            if (winners.Count == 0 || losers.Count == 0) return;

            var winner = await UpdatePlayers(nextEvent, winners);
            await _playerRepository.UpsertPlayerOverview(winner);

            await UpdateLosers(nextEvent, losers);
        }

        private async Task UpdateLosers(MatchFinishedEvent nextEvent, List<PlayerMMrChange> losers)
        {
            foreach (var losingTeam in losers.GroupBy(x => x.team))
            {
                var loser = await UpdatePlayers(nextEvent, losingTeam.ToList());
                await _playerRepository.UpsertPlayerOverview(loser);
            }
        }

        private async Task<PlayerOverview> UpdatePlayers(MatchFinishedEvent nextEvent, List<PlayerMMrChange> players)
        {
            var winnerPlayerIds = players.Select(w => PlayerId.Create(w.battleTag)).ToList();

            var match = nextEvent.match;
            var winnerIdCombined = new BattleTagIdCombined(
                players.Select(p => PlayerId.Create(p.battleTag)).ToList(),
                match.gateway,
                match.gameMode, match.season);

            var winner = await _playerRepository.LoadOverview(winnerIdCombined.Id)
                         ?? PlayerOverview.Create(
                             winnerPlayerIds,
                             match.gateway,
                             match.gameMode,
                             match.season);

            winner.RecordWin(
                players.First().won,
                (int?) players.First().updatedMmr?.rating ?? (int?) players.First().mmr?.rating ?? 0);

            return winner;
        }
    }
}