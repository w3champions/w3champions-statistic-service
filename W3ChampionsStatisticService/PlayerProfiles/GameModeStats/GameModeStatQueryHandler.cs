﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using W3ChampionsStatisticService.CommonValueObjects;
using W3ChampionsStatisticService.Ladder;
using W3ChampionsStatisticService.Ports;
using W3ChampionsStatisticService.Services;

namespace W3ChampionsStatisticService.PlayerProfiles.GameModeStats
{
    public class GameModeStatQueryHandler
    {
        private readonly IPlayerRepository _playerRepository;
        private readonly IRankRepository _rankRepository;

        public GameModeStatQueryHandler(
            IPlayerRepository playerRepository,
            IRankRepository rankRepository)
        {
            _playerRepository = playerRepository;
            _rankRepository = rankRepository;
        }

        public async Task<List<PlayerGameModeStatPerGateway>> LoadPlayerStatsWithRanks(
            string battleTag,
            GateWay gateWay,
            int season)
        {
            var player = await _playerRepository.LoadGameModeStatPerGateway(battleTag, gateWay, season);
            var leaguesOfPlayer = await _rankRepository.LoadPlayerOfLeague(battleTag, season);

            foreach (var rank in leaguesOfPlayer)
            {
                PopulateLeague(player, rank);
            }

            return player;
        }

        private void PopulateLeague(
            List<PlayerGameModeStatPerGateway> player,
            Rank rank)
        {
            if (rank.RankNumber == 0) return;
            var gameModeStat = player.Single(g => g.Id == rank.Id);

            gameModeStat.Division = rank.LeagueDivision;
            gameModeStat.LeagueOrder = rank.LeagueOrder;
            gameModeStat.RankingPoints = rank.RankingPoints;
            gameModeStat.LeagueId = rank.League;
            gameModeStat.Rank = rank.RankNumber;
        }
    }
}