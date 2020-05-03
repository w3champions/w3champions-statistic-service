﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using W3ChampionsStatisticService.Matches;
using W3ChampionsStatisticService.Ports;

namespace W3ChampionsStatisticService.Ladder
{
    [ApiController]
    [Route("api/ladder")]
    public class LadderController : ControllerBase
    {
        private readonly IRankRepository _rankRepository;
        private readonly IMatchEventRepository _matchEventRepository;

        public LadderController(
            IRankRepository rankRepository,
            IMatchEventRepository matchEventRepository)
        {
            _rankRepository = rankRepository;
            _matchEventRepository = matchEventRepository;
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchPlayer(string searchFor, int gateWay = 20, GameMode gameMode = GameMode.GM_1v1)
        {
            var players = await _rankRepository.LoadPlayerOfLeagueLike(searchFor, gateWay, gameMode);
            return Ok(players);
        }

        [HttpGet("{leagueId}")]
        public async Task<IActionResult> GetLadder([FromRoute] int leagueId, int gateWay = 20)
        {
            var playersInLadder = await _rankRepository.LoadPlayersOfLeague(leagueId, gateWay);
            if (playersInLadder == null)
            {
                return NoContent();
            }

            return Ok(playersInLadder);
        }

        [HttpGet("league-constellation")]
        public async Task<IActionResult> GetLeagueConstellation()
        {
            var leagues = await _rankRepository.LoadLeagueConstellation();
            return Ok(leagues);
        }
    }
}