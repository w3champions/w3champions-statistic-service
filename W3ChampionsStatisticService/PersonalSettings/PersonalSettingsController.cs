﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using W3ChampionsStatisticService.Chats;
using W3ChampionsStatisticService.PlayerProfiles;
using W3ChampionsStatisticService.Ports;
using W3ChampionsStatisticService.WebApi.ActionFilters;

namespace W3ChampionsStatisticService.PersonalSettings
{
    [ApiController]
    [Route("api/personal-settings")]
    public class PersonalSettingsController : ControllerBase
    {
        private readonly IBlizzardAuthenticationService _authenticationService;
        private readonly IPersonalSettingsRepository _personalSettingsRepository;
        private readonly IPlayerRepository _playerRepository;
        private readonly ChatAuthenticationService _chatAuthenticationService;
        private readonly PersonalSettingsCommandHandler _commandHandler;

        public PersonalSettingsController(
            IBlizzardAuthenticationService authenticationService,
            IPersonalSettingsRepository personalSettingsRepository,
            IPlayerRepository playerRepository,
            ChatAuthenticationService chatAuthenticationService,
            PersonalSettingsCommandHandler commandHandler)
        {
            _authenticationService = authenticationService;
            _personalSettingsRepository = personalSettingsRepository;
            _playerRepository = playerRepository;
            _chatAuthenticationService = chatAuthenticationService;
            _commandHandler = commandHandler;
        }

        [HttpGet("{battleTag}")]
        public async Task<IActionResult> GetPersonalSetting(string battleTag)
        {
            var setting = await _personalSettingsRepository.Load(battleTag);
            if (setting == null)
            {
                var player = await _playerRepository.LoadPlayerProfile(battleTag);
                return Ok(new PersonalSetting(battleTag) { Players = new List<PlayerOverallStats> { player } });
            }
            return Ok(setting);
        }

        [HttpGet("{commaSeparatedBattleTags}/many")]
        public async Task<IActionResult> GetPersonalSettings(string commaSeparatedBattleTags)
        {
            var splitBattleTags = commaSeparatedBattleTags.Split(new string[] { "," }, System.StringSplitOptions.RemoveEmptyEntries);

            var settings = await _personalSettingsRepository.LoadMany(splitBattleTags);

            if (settings != null)
            {
                return Ok(settings.Select(x => new {
                    x.Id,
                    x.Country,
                    x.Location,
                    x.ProfilePicture
                }));
            }

            return Ok(new object[0]);
        }

        [HttpPut("{battleTag}")]
        public async Task<IActionResult> SetPersonalSetting(
           string battleTag,
           [FromBody] PersonalSettingsDTO dto)
        {
            var setting = await _personalSettingsRepository.Load(battleTag) ?? new PersonalSetting(battleTag);

            setting.Update(dto);

            await _personalSettingsRepository.Save(setting);

            return Ok();
        }

        [HttpPut("{battleTag}/api-key")]
        public async Task<IActionResult> SetApiKey(
            string battleTag)
        {
            var chatUser = await _chatAuthenticationService.GetUserByBattleTag(battleTag) ?? new ChatUser(battleTag);
            chatUser.CreatApiKey();
            await _chatAuthenticationService.SaveUser(chatUser);

            return Ok(chatUser);
        }

        [HttpGet("{battleTag}/api-key")]
        [CheckIfBattleTagBelongsToAuthCode]
        public async Task<IActionResult> GetApiKey(
            string battleTag)
        {
            var chatUser = await _chatAuthenticationService.GetUserByBattleTag(battleTag);
            if (chatUser == null)
            {
                chatUser = new ChatUser(battleTag);
                await _chatAuthenticationService.SaveUser(chatUser);
            }

            return Ok(chatUser);
        }

        [HttpPut("{battleTag}/profile-picture")]
        [CheckIfBattleTagBelongsToAuthCode]
        public async Task<IActionResult> SetProfilePicture(
            string battleTag,
            [FromBody] SetPictureCommand command)
        {
            var result = await _commandHandler.UpdatePicture(battleTag, command);

            if (!result) return BadRequest();

            return Ok();
        }
    }
}