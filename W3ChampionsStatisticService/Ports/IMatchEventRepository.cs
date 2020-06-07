﻿using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using W3ChampionsStatisticService.PadEvents;

namespace W3ChampionsStatisticService.Ports
{
    public interface IMatchEventRepository
    {
        Task<List<MatchFinishedEvent>> Load(string lastObjectId,  int pageSize = 100);
        Task<List<MatchStartedEvent>> LoadStartedMatches();
        Task InsertIfNotExisting(MatchFinishedEvent matchFinishedEvent);
        Task Insert(List<MatchFinishedEvent> matchFinishedEvent);
        Task<List<RankingChangedEvent>> CheckoutForRead();
        Task<List<LeagueConstellationChangedEvent>> LoadLeagueConstellationChanged();
        Task DeleteStartedEvent(ObjectId nextEventId);
    }
}