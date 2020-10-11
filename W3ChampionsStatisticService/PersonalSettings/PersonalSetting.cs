﻿using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using W3ChampionsStatisticService.CommonValueObjects;
using W3ChampionsStatisticService.PlayerProfiles;

namespace W3ChampionsStatisticService.PersonalSettings
{
    public class PersonalSetting
    {
        public PersonalSetting(string battleTag, List<PlayerOverallStats> players = null)
        {
            Id = battleTag;
            Players = players ?? new List<PlayerOverallStats>();
        }

        public string ProfileMessage { get; set; }
        [BsonIgnore]
        [JsonIgnore]
        public PlayerOverallStats RaceWins => Players?.SingleOrDefault() ?? PlayerOverallStats.Create(Id);
        public List<RaceWinLoss> WinLosses => RaceWins.WinLosses;
        [JsonIgnore]
        [BsonIgnoreIfNull]
        public List<PlayerOverallStats> Players { get; set; }

        public string Twitch { get; set; }

        public string YouTube { get; set; }

        public string Twitter { get; set; }

        public string Country { get; set; }

        public string Location { get; set; }

        public string HomePage { get; set; }
        public ProfilePicture ProfilePicture { get; set; } = ProfilePicture.Default();
        public string Id { get; set; }

        public SpecialPicture[] SpecialPictures { get; set; } = new SpecialPicture[0];

        public bool SetProfilePicture(SetPictureCommand cmd)
        {
            bool isValid = false;

            if (cmd.avatarCategory == AvatarCategory.Special)
            {
                isValid = SpecialPictures.Any(x => x.PictureId == cmd.pictureId);
            }
            else
            {
                var winsPerRace = RaceWins?.GetWinsPerRace((Race)cmd.avatarCategory);
                isValid = winsPerRace >= PictureRange.FirstOrDefault(p => p.PictureId == cmd.pictureId)?.NeededWins;
            }
           
            if (isValid)
            {
                ProfilePicture = new ProfilePicture(cmd.avatarCategory, cmd.pictureId, cmd.description);
            }

            return isValid;
        }

        public List<RaceToMaxPicture> PickablePictures => new List<RaceToMaxPicture>
        {
            new RaceToMaxPicture(Race.HU, GetMaxOf(RaceWins.GetWinsPerRace(Race.HU)) ),
            new RaceToMaxPicture(Race.OC, GetMaxOf(RaceWins.GetWinsPerRace(Race.OC)) ),
            new RaceToMaxPicture(Race.NE, GetMaxOf(RaceWins.GetWinsPerRace(Race.NE)) ),
            new RaceToMaxPicture(Race.UD, GetMaxOf(RaceWins.GetWinsPerRace(Race.UD)) ),
            new RaceToMaxPicture(Race.RnD, GetMaxOf(RaceWins.GetWinsPerRace(Race.RnD)) )
        };

        private long GetMaxOf(long getWinsPerRace)
        {
            return PictureRange.Where(r => r.NeededWins <= getWinsPerRace).Max(r => r.PictureId);
        }

        [BsonIgnore]
        public List<WinsToPictureId> PictureRange => new List<WinsToPictureId>
        {
            new WinsToPictureId(0, 0),
            new WinsToPictureId(1, 5),
            new WinsToPictureId(2, 20),
            new WinsToPictureId(3, 70),
            new WinsToPictureId(4, 150),
            new WinsToPictureId(5, 250),
            new WinsToPictureId(6, 400),
            new WinsToPictureId(7, 600),
            new WinsToPictureId(8, 900),
            new WinsToPictureId(9, 1200),
            new WinsToPictureId(10, 1500)
        };

        public void Update(PersonalSettingsDTO dto)
        {
            ProfileMessage = dto.ProfileMessage;
            Twitch = dto.Twitch;
            YouTube = dto.Youtube;
            Twitter = dto.Twitter;
            HomePage = dto.HomePage;
            Country = dto.Country;
        }
    }
}