using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using W3ChampionsStatisticService.Achievements.Models;
using W3ChampionsStatisticService.CommonValueObjects;
using W3ChampionsStatisticService.Ladder;
using W3ChampionsStatisticService.PlayerStats.RaceOnMapVersusRaceStats;
using W3ChampionsStatisticService.PlayerProfiles;
using W3ChampionsStatisticService.Ports;
using W3ChampionsStatisticService.Matches;
using W3ChampionsStatisticService.ReadModelBase;
using W3ChampionsStatisticService.PadEvents;

namespace W3ChampionsStatisticService.Achievements {

    //TODO: refactor this to work with the individual achievements
    // their update logic will be part of their class
     public class AchievementRepositoryHandler : IReadModelHandler  {

        private readonly IAchievementRepository _achievementRepository;
        private readonly IMatchRepository _matchRepository;
        private readonly IPlayerRepository _playerRepository;
        private readonly IPlayerStatsRepository _playerStatsRepository;

        // in order to add more achievements add the ID here, and update
        // UpdateCurrentPlayerAchievementList and UpdateCurrentPlayerAchievements
        private long[] ActiveAchievementIds = {0, 1};

        public AchievementRepositoryHandler(
            IAchievementRepository achievementRepository,
            IMatchRepository matchRepository,
            IPlayerRepository playerRepository,
            IPlayerStatsRepository playerStatsRepository) {
            _achievementRepository = achievementRepository;
            _matchRepository = matchRepository;    
            _playerRepository = playerRepository;
            _playerStatsRepository = playerStatsRepository;
        }

        public async Task Update(MatchFinishedEvent nextEvent){
            try {
                if(nextEvent.WasFakeEvent){return;}
                var matchup = await GetMatchupFromMatch(nextEvent.match);
                var teams = matchup.Teams;
                foreach(Team team in teams){
                    foreach(PlayerOverviewMatches player in team.Players){
                        var needsUpdateWithCurrentMatchup = true;
                        var battleTag = player.BattleTag;
                        var playerAchievements = await _achievementRepository.GetPlayerAchievements(battleTag);

                        if (playerAchievements == null){
                            var playerProfile = await _playerRepository.LoadPlayerProfile(battleTag);
                            playerAchievements = await CreateNewPlayerAchievements(playerProfile);
                            needsUpdateWithCurrentMatchup = false;
                        }

                        if (needsUpdateWithCurrentMatchup){
                            var matchups = new List<Matchup>{matchup};
                            for(int i = 0; i < playerAchievements.PlayerAchievementList.Count; i++){
                                playerAchievements.PlayerAchievementList[i] = await UpdateCurrentPlayerAchievement(
                                    playerAchievements.PlayerAchievementList[i], null, battleTag, matchups, false);
                            }
                        } 
                        
                        if (playerAchievements.PlayerAchievementList.Count < ActiveAchievementIds.Length) {
                            var playerProfile = await _playerRepository.LoadPlayerProfile(battleTag);
                            playerAchievements = await AddAdditionalAchievements(playerAchievements, playerProfile);
                        }

                        await _achievementRepository.UpsertPlayerAchievements(playerAchievements);
                    }
                }
            }catch(Exception e){
                Console.WriteLine($"Exception occured when attempting to update player achievements: {e}");
            }
        }

        private async Task<PlayerAchievements> AddAdditionalAchievements(PlayerAchievements playerAchievements, PlayerOverallStats playerProfile){
            var achievementsToAdd = UpdateCurrentPlayerAchievementList(playerAchievements.PlayerAchievementList);
            for(int i = 0; i < achievementsToAdd.Count; i++){
                achievementsToAdd[i] = await UpdateCurrentPlayerAchievement(achievementsToAdd[i], playerProfile, null, null, true);
                playerAchievements.PlayerAchievementList.Add(achievementsToAdd[i]);
            }
            return playerAchievements;
        } 

        private async Task<Matchup> GetMatchupFromMatch(Match match){
            var id = ObjectId.Parse(match.id);
            var matchupDetail = await _matchRepository.LoadDetails(id);
            return matchupDetail.Match;
        }

        public async Task<PlayerAchievements> GetPlayerAchievementsFromUI(string battleTag){
            var needsSave = false;
            var playerAchievements = await _achievementRepository.GetPlayerAchievements(battleTag);
            if (playerAchievements == null){
                var playerProfile = await _playerRepository.LoadPlayerProfile(battleTag);
                if (playerProfile != null){
                    playerAchievements = await CreateNewPlayerAchievements(playerProfile);
                    needsSave = true;
                } else {
                    return null;
                } 
            } else {
                if (playerAchievements.PlayerAchievementList.Count < ActiveAchievementIds.Length){
                    var achievementsToAdd = UpdateCurrentPlayerAchievementList(playerAchievements.PlayerAchievementList);
                    var playerProfile = await _playerRepository.LoadPlayerProfile(battleTag);
                    playerAchievements = await AddAdditionalAchievements(playerAchievements, playerProfile);
                    needsSave = true;
                }
            }
            if(needsSave){await _achievementRepository.UpsertPlayerAchievements(playerAchievements);}
            return playerAchievements;
        }

        private List<int> ConvertSeasonsToSimpleList(List<Season> seasons){
            var seasonList = new List<int>();
            foreach (Season s in seasons){seasonList.Add(s.Id);}
            seasonList.Reverse();
            return seasonList;
        }

        private List<Achievement> UpdateCurrentPlayerAchievementList(List<Achievement> currentAchievementsList){
            if (currentAchievementsList == null) {currentAchievementsList = new List<Achievement>();}
                var currentListIds = new List<long>();
                foreach(Achievement achievement in currentAchievementsList){
                    currentListIds.Add(achievement.Id);
                }
                foreach(long activeAchievementsId in ActiveAchievementIds){
                    if(!currentListIds.Contains(activeAchievementsId)){
                        switch (activeAchievementsId) {
                            case 0:
                            currentAchievementsList.Add(new MapWith25WinsAchievement());
                            break;
                            case 1:
                            currentAchievementsList.Add(new Win10GamesWithATPartnerAchievement());
                            break;
                        }

                    }
            }
            return currentAchievementsList;
        }

        private async Task<List<Matchup>> GetAllPlayerMatches(PlayerOverallStats playerOverallStats){
            var playerMatches = new List<Matchup>();
            var battleTag = playerOverallStats.BattleTag;
            var playerRaceOnMapVersusRaceRatios = new List<PlayerRaceOnMapVersusRaceRatio>();
            var seasons = ConvertSeasonsToSimpleList(playerOverallStats.ParticipatedInSeasons);

            foreach(int s in seasons){
                var playerRaceOnMapVersusRaceRatio = await _playerStatsRepository.LoadMapAndRaceStat(battleTag, s);
                playerRaceOnMapVersusRaceRatios.Add(playerRaceOnMapVersusRaceRatio);
                var seasonalMatches = await _matchRepository.LoadFor(battleTag, null, GateWay.Undefined, GameMode.Undefined, 100, 0, s);

                foreach(Matchup matchup in seasonalMatches){
                    playerMatches.Add(matchup);
                }
            }
            return playerMatches;
        }

        private async Task<Achievement> UpdateCurrentPlayerAchievement(
            Achievement playerAchievement,
            PlayerOverallStats playerOverallStats,
            string battleTag,
            List<Matchup> matches,
            bool isFirstRun
            ){
            if (matches == null){matches = await GetAllPlayerMatches(playerOverallStats);}
            if (battleTag == null){battleTag = playerOverallStats.BattleTag;}
            if (isFirstRun){playerAchievement.Counter = new Dictionary<string, int>();}
        
            var achievementProgressCounter = playerAchievement.Counter;
            switch(playerAchievement.Id){
                case 0:
                    var firstMapTo25Wins = "";
                    foreach(Matchup matchup in matches){
                        var map = matchup.Map;
                        var teams = matchup.Teams;
                        if(PlayerDidWin(battleTag, teams)){
                            var hitWinsLimit = AddToWinsCount(achievementProgressCounter, map, 25);
                            if(playerAchievement.ProgressCurrent < playerAchievement.ProgressEnd){
                                playerAchievement.ProgressCurrent = CheckMostWins(achievementProgressCounter);
                            }
                            if (hitWinsLimit){firstMapTo25Wins = map; break;}
                        }
                    }
                    if(firstMapTo25Wins != ""){
                        playerAchievement.Caption = $"Player has completed this achievement with 25 games won on {firstMapTo25Wins}";
                        playerAchievement.Completed = true;
                    }
                    break;
                case 1:
                    var firstPartnerTo10Wins = "";
                    foreach(Matchup matchup in matches){
                        if (matchup.GameMode != GameMode.GM_2v2_AT){continue;}
                        if (PlayerDidWin(battleTag, matchup.Teams)){
                            var teamMate = GetPlayerTeamMate(battleTag, matchup.Teams);
                            var hitWinsLimit = AddToWinsCount(achievementProgressCounter, teamMate, 10);
                            if(playerAchievement.ProgressCurrent < playerAchievement.ProgressEnd){
                                playerAchievement.ProgressCurrent = CheckMostWins(achievementProgressCounter);
                            }
                            if(hitWinsLimit){firstPartnerTo10Wins = teamMate; break;}
                        }
                    }
                    if(firstPartnerTo10Wins != ""){
                        playerAchievement.Caption = $"Player has completed this achievement with {firstPartnerTo10Wins}";
                        playerAchievement.Completed = true;
                    }
                    break;
            }
            playerAchievement.Counter = achievementProgressCounter;
            return playerAchievement;
        }

        private string GetPlayerTeamMate(string battleTag, IList<Team> teams){
            foreach(Team team in teams){
                var players = team.Players;
                foreach(PlayerOverviewMatches player in players){
                    if(player.BattleTag != battleTag && player.Won){
                        return player.BattleTag;
                    }
                }
            }
            return string.Empty;
        }

        private long CheckMostWins(Dictionary<string,int> winsCount){
            long maxValue = 0;
            foreach(var wins in winsCount){
                if(wins.Value > maxValue){ maxValue = wins.Value;}
            }
            return maxValue;
        }

        private bool AddToWinsCount(Dictionary<string,int> winsCount, string unit, int maxCount) {
            var didReachMaxCount = false;
            if(!winsCount.ContainsKey(unit)){
                winsCount.Add(unit, 1);
            } else {
                winsCount[unit] += 1;
                if (winsCount[unit] == maxCount){
                    didReachMaxCount = true;
                }
            }
            return didReachMaxCount;
        }

        private bool PlayerDidWin(string battleTag, IList<Team> teams){
            foreach(Team team in teams){
                var players = team.Players;
                foreach(PlayerOverviewMatches player in players){
                    var playerName = player.BattleTag;
                    if (playerName == battleTag){return player.Won;}
                }
            }
            return false;
        }

        private async Task<PlayerAchievements> CreateNewPlayerAchievements(PlayerOverallStats playerOverallStats){
            var newPlayerAchievements = new PlayerAchievements();
            newPlayerAchievements.PlayerId = playerOverallStats.BattleTag;
            var playerMatches = await GetAllPlayerMatches(playerOverallStats);
            newPlayerAchievements.PlayerAchievementList = UpdateCurrentPlayerAchievementList(null);
            for(int i = 0; i < newPlayerAchievements.PlayerAchievementList.Count; i++){
                newPlayerAchievements.PlayerAchievementList[i] =
                    await UpdateCurrentPlayerAchievement(newPlayerAchievements.PlayerAchievementList[i], playerOverallStats, null, playerMatches, true);
            }
            return newPlayerAchievements;
        }
    }
}