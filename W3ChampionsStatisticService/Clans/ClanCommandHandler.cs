﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using W3ChampionsStatisticService.Ports;

namespace W3ChampionsStatisticService.Clans
{
    public class ClanCommandHandler

    {
        private readonly IClanRepository _clanRepository;
        private readonly IRankRepository _rankRepository;

        public ClanCommandHandler(IClanRepository clanRepository,
            IRankRepository rankRepository)
        {
            _clanRepository = clanRepository;
            _rankRepository = rankRepository;
        }

        public async Task<Clan> CreateClan(string clanName, string clanAbbrevation, string battleTagOfFounder)
        {
            var memberShip = await _clanRepository.LoadMemberShip(battleTagOfFounder) ?? ClanMembership.Create(battleTagOfFounder);
            var clan = Clan.Create(clanName, clanAbbrevation, memberShip);
            var wasSaved = await _clanRepository.TryInsertClan(clan);
            if (!wasSaved) throw new ValidationException("Clan Name allready taken");
            memberShip.ClanId = clan.ClanId;
            await _clanRepository.UpsertMemberShip(memberShip);
            return clan;
        }

        public async Task InviteToClan(string battleTag, string clanId, string personWhoInvitesBattleTag)
        {
            var clanMemberShip = await _clanRepository.LoadMemberShip(battleTag)
                                 ?? ClanMembership.Create(battleTag);
            var clan = await _clanRepository.LoadClan(clanId);

            if (clan == null)
            {
                throw new ValidationException("Clan not found");
            }

            clan.Invite(clanMemberShip, personWhoInvitesBattleTag);

            await _clanRepository.UpsertClan(clan);
            await _clanRepository.UpsertMemberShip(clanMemberShip);
        }

        public async Task<Clan> AcceptInvite(string playerBattleTag, string clanId)
        {
            var clan = await _clanRepository.LoadClan(clanId);
            var clanMemberShip = await _clanRepository.LoadMemberShip(playerBattleTag) ?? ClanMembership.Create(playerBattleTag);
            clan.AcceptInvite(clanMemberShip);
            await _clanRepository.UpsertClan(clan);
            await _clanRepository.UpsertMemberShip(clanMemberShip);
            return clan;
        }

        public async Task DeleteClan(string clanId, string actingPlayer)
        {
            var clan = await _clanRepository.LoadClan(clanId);
            if (clan.ChiefTain != actingPlayer) throw new ValidationException("Only Chieftain can delete the clan");
            
            await _clanRepository.DeleteClan(clanId);

            clan.Members.Add(clan.ChiefTain);
            var memberShips = await _clanRepository.LoadMemberShips(clan.Members);

            foreach (var member in memberShips)
            {
                member.LeaveClan();
            }

            await _clanRepository.SaveMemberShips(memberShips);
        }

        public async Task<Clan> GetClanForPlayer(string battleTag)
        {
            var membership = await _clanRepository.LoadMemberShip(battleTag);
            if (membership?.ClanId != null)
            {
                var clan = await LoadClan(membership.ClanId);
                return clan;
            }

            return null;
        }

        public async Task RevokeInvitationToClan(string battleTag, string clanId, string personWhoInvitesBattleTag)
        {
            var clanMemberShip = await _clanRepository.LoadMemberShip(battleTag);
            var clan = await _clanRepository.LoadClan(clanId);

            if (clan == null || clanMemberShip == null) throw new ValidationException("Clan or member not found");

            clan.RevokeInvite(clanMemberShip, personWhoInvitesBattleTag);

            await _clanRepository.UpsertClan(clan);
            await _clanRepository.UpsertMemberShip(clanMemberShip);
        }

        public async Task<Clan> RejectInvite(string clanId, string battleTag)
        {
            var clanMemberShip = await _clanRepository.LoadMemberShip(battleTag);
            var clan = await _clanRepository.LoadClan(clanId);

            if (clan == null || clanMemberShip == null) throw new ValidationException("Clan or member not found");

            clan.RejectInvite(clanMemberShip);

            await _clanRepository.UpsertClan(clan);
            await _clanRepository.UpsertMemberShip(clanMemberShip);

            return clan;
        }

        public async Task<Clan> LeaveClan(string clanId, string battleTag)
        {
            var clanMemberShip = await _clanRepository.LoadMemberShip(battleTag);
            var clan = await _clanRepository.LoadClan(clanId);

            if (clan == null || clanMemberShip == null) throw new ValidationException("Clan or member not found");

            clan.LeaveClan(clanMemberShip);

            await _clanRepository.UpsertClan(clan);
            await _clanRepository.UpsertMemberShip(clanMemberShip);

            return clan;
        }

        public async Task<Clan> RemoveShamanFromClan(string shamanId, string clanId, string actingPlayer)
        {
            var clan = await _clanRepository.LoadClan(clanId);

            if (clan == null) throw new ValidationException("Clan not found");

            clan.RemoveShaman(shamanId, actingPlayer);

            await _clanRepository.UpsertClan(clan);

            return clan;
        }

        public async Task<Clan> AddShamanToClan(string shamanId, string clanId, string actingPlayer)
        {
            var clan = await _clanRepository.LoadClan(clanId);

            if (clan == null) throw new ValidationException("Clan not found");

            clan.AddShaman(shamanId, actingPlayer);

            await _clanRepository.UpsertClan(clan);

            return clan;
        }

        public async Task<Clan> KickPlayer(string battleTag, string clanId, string actingPlayer)
        {
            var clanMemberShip = await _clanRepository.LoadMemberShip(battleTag);
            var clan = await _clanRepository.LoadClan(clanId);

            if (clan == null || clanMemberShip == null) throw new ValidationException("Clan or member not found");

            clan.KickPlayer(clanMemberShip, actingPlayer);

            await _clanRepository.UpsertMemberShip(clanMemberShip);
            await _clanRepository.UpsertClan(clan);

            return clan;
        }

        public async Task<Clan> SwitchChieftain(string newChieftain, string clanId, string actingPlayer)
        {
            var clan = await _clanRepository.LoadClan(clanId);

            if (clan == null) throw new ValidationException("Clan not found");

            clan.SwitchChieftain(newChieftain, actingPlayer);

            await _clanRepository.UpsertClan(clan);

            return clan;
        }

        public async Task<Clan> LoadClan(string clanId)
        {
            var clan = await _clanRepository.LoadClan(clanId);
            var seasons = await _rankRepository.LoadSeasons();
            var season = seasons.Max(s => s.Id);

            var list = new List<string>();
            list.AddRange(clan.Members);
            list.AddRange(clan.Shamans);
            list.Add(clan.ChiefTain);
            var ranksFromClan = await _rankRepository.LoadRanksForPlayers(list, season);

            clan.Ranks = ranksFromClan.ToList();

            return clan;
        }
    }
}