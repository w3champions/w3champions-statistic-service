﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using W3ChampionsStatisticService.CommonValueObjects;
using W3ChampionsStatisticService.Ports;

namespace W3ChampionsStatisticService.Matches
{
    [ApiController]
    [Route("api/matches")]
    public class MatchesController : ControllerBase
    {
        private readonly IMatchRepository _matchRepository;

        public MatchesController(IMatchRepository matchRepository)
        {
            _matchRepository = matchRepository;
        }

        [HttpGet("")]
        public async Task<IActionResult> GetMatches(
            int offset = 0,
            int pageSize = 100,
            GameMode gameMode = GameMode.Undefined,
            GateWay gateWay = GateWay.Undefined)
        {
            if (pageSize > 100) pageSize = 100;
            var matches = await _matchRepository.Load(gateWay, gameMode, offset, pageSize);
            var count = await _matchRepository.Count(gateWay, gameMode);
            return Ok(new { matches, count });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMatcheDetails(string id)
        {
            var match = await _matchRepository.LoadDetails(new ObjectId(id));
            return Ok(match);
        }

        [HttpGet("search")]
        public async Task<IActionResult> GetMatchesPerPlayer(
            string playerId,
            string opponentId = null,
            GameMode gameMode = GameMode.Undefined,
            GateWay gateWay = GateWay.Undefined,
            int offset = 0,
            int pageSize = 100)
        {
            if (pageSize > 100) pageSize = 100;
            var matches = await _matchRepository.LoadFor(playerId, opponentId, gateWay, gameMode, pageSize, offset);
            var count = await _matchRepository.CountFor(playerId, opponentId, gateWay, gameMode);
            return Ok(new { matches, count });
        }


        [HttpGet("ongoing")]
        public async Task<IActionResult> GetOnGoingMatches(
            int offset = 0,
            int pageSize = 100,
            GameMode gameMode = GameMode.Undefined,
            GateWay gateWay = GateWay.Undefined)
        {
            if (pageSize > 200) pageSize = 200;
            var matches = await _matchRepository.LoadOnGoingMatches(gameMode, gateWay, offset, pageSize);
            var count = await _matchRepository.CountOnGoingMatches(gameMode, gateWay);

            PlayersObfuscator.ObfuscatePlayersForFFA(matches.ToArray());

            return Ok(new { matches, count });
        }

        [HttpGet("ongoing/{playerId}")]
        public async Task<IActionResult> GetOnGoingMatches(string playerId)
        {
            var onGoingMatch = await _matchRepository.LoadOnGoingMatchForPlayer(playerId);

            if (onGoingMatch.GameMode == GameMode.FFA)
            {
                return Ok(null);
            }

            PlayersObfuscator.ObfuscatePlayersForFFA(onGoingMatch);

            return Ok(onGoingMatch);
        }
    }
}