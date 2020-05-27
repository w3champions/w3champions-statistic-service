﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using W3ChampionsStatisticService.Authorization;
using W3ChampionsStatisticService.Clans.Commands;
using W3ChampionsStatisticService.Ports;

namespace W3ChampionsStatisticService.Clans
{
    [ApiController]
    [Route("api/clans")]
    public class ClanController : ControllerBase
    {
        private readonly ClanCommandHandler _clanCommandHandler;
        private readonly IClanRepository _clanRepository;

        public ClanController(
            ClanCommandHandler clanCommandHandler,
            IClanRepository clanRepository)
        {
            _clanCommandHandler = clanCommandHandler;
            _clanRepository = clanRepository;
        }

        [HttpPost]
        [BnetAuth]
        public async Task<IActionResult> CreateClan(
            [FromBody] CreateClanDto clanDto,
            string actingPlayer)
        {
            var clan = await _clanCommandHandler.CreateClan(clanDto.ClanName, actingPlayer);
            return Ok(clan);
        }

        [HttpPost("{clanId}/signees")]
        public async Task<IActionResult> SignClanPetition(string clanId, [FromBody] SignClanDto clanDto)
        {
            var clan = await _clanCommandHandler.SignClanPetition(clanDto.PlayerBattleTag, clanId);
            return Ok(clan);
        }

        [HttpGet("{clanId}")]
        public async Task<IActionResult> GetClan(string clanId)
        {
            var clan = await _clanRepository.LoadClan(clanId);
            return Ok(clan);
        }

        [HttpDelete("{clanId}")]
        [BnetAuth]
        public async Task<IActionResult> DeleteClan(
            string clanId,
            string actingPlayer)
        {
            await _clanCommandHandler.DeleteClan(clanId, actingPlayer);
            return Ok();
        }

        [HttpPost("{clanId}/invites")]
        [BnetAuth]
        public async Task<IActionResult> InviteToClan(
            string clanId,
            string actingPlayer,
            [FromBody] CreateInviteDto inviteDto)
        {
            await _clanCommandHandler.InviteToClan(inviteDto.PlayerBattleTag, clanId, actingPlayer);
            return Ok();
        }

        [HttpPut("{clanId}/invites/{battleTag}")]
        [BnetAuth]
        public async Task<IActionResult> AcceptInvite(string clanId, string battleTag)
        {
            var clan = await _clanCommandHandler.AcceptInvite(clanId, battleTag);
            return Ok(clan);
        }
    }
}