﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using W3ChampionsStatisticService.CommonValueObjects;
using W3ChampionsStatisticService.ReadModelBase;

namespace W3ChampionsStatisticService.Matches
{
    public class OngoingMatchesCache : MongoDbRepositoryBase, IOngoingMatchesCache
    {
        private List<OnGoingMatchup> _values = new List<OnGoingMatchup>();
        private DateTimeOffset _lastUpdate = DateTimeOffset.MinValue;

        public async Task<long> CountOnGoingMatches(GameMode gameMode, GateWay gateWay)
        {
            await UpdateCacheIfNeeded();
            return _values.Count(m => (gameMode == GameMode.Undefined || m.GameMode == gameMode)
                                      && (gateWay == GateWay.Undefined || m.GateWay == gateWay));
        }

        public async Task<List<OnGoingMatchup>> LoadOnGoingMatches(GameMode gameMode, GateWay gateWay, int offset, int pageSize)
        {
            await UpdateCacheIfNeeded();

            return _values.Where(m => gameMode == GameMode.Undefined || m.GameMode == gameMode
                && (gateWay == GateWay.Undefined || m.GateWay == gateWay)).Skip(offset).Take(pageSize).ToList();
        }

        public async Task<OnGoingMatchup> LoadOnGoingMatchForPlayer(string playerId)
        {
            await UpdateCacheIfNeeded();
            return _values.FirstOrDefault(m => m.Team1Players.Contains(playerId)
                                      || m.Team2Players.Contains(playerId)
                                      || m.Team3Players.Contains(playerId)
                                      || m.Team4Players.Contains(playerId));
        }

        private async Task UpdateCacheIfNeeded()
        {
            var difference = DateTimeOffset.Now - _lastUpdate;
            if (difference > TimeSpan.FromSeconds(120))
            {
                _lastUpdate = DateTimeOffset.Now;
                var mongoCollection = CreateCollection<OnGoingMatchup>();
                _values = await mongoCollection.Find(r => true).SortByDescending(s => s.Id).ToListAsync();
            }
        }

        public OngoingMatchesCache(MongoClient mongoClient) : base(mongoClient)
        {
        }
    }

    public interface IOngoingMatchesCache
    {
        Task<long> CountOnGoingMatches(GameMode gameMode, GateWay gateWay);
        Task<List<OnGoingMatchup>> LoadOnGoingMatches(GameMode gameMode, GateWay gateWay, int offset, int pageSize);
        Task<OnGoingMatchup> LoadOnGoingMatchForPlayer(string playerId);
    }
}