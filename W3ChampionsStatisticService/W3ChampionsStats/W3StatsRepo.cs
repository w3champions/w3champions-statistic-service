using System.Threading.Tasks;
using W3ChampionsStatisticService.Ports;
using W3ChampionsStatisticService.ReadModelBase;
using W3ChampionsStatisticService.W3ChampionsStats.GameLengths;
using W3ChampionsStatisticService.W3ChampionsStats.GamesPerDays;
using W3ChampionsStatisticService.W3ChampionsStats.RaceAndWinStats;

namespace W3ChampionsStatisticService.W3ChampionsStats
{
    public class W3StatsRepo : MongoDbRepositoryBase, IW3StatsRepo
    {
        public W3StatsRepo(DbConnctionInfo connectionInfo) : base(connectionInfo)
        {
        }

        public Task<Wc3Stats> Load()
        {
            return LoadFirst<Wc3Stats>(s => s.Id == nameof(Wc3Stats));
        }

        public Task Save(Wc3Stats stat)
        {
            return Upsert(stat, s => s.Id == stat.Id);
        }

        public Task<GamesPerDay> LoadGamesPerDay()
        {
            return LoadFirst<GamesPerDay>(s => s.Id == nameof(GamesPerDay));
        }

        public Task Save(GamesPerDay stat)
        {
            return Upsert(stat, s => s.Id == stat.Id);
        }

        public Task<GameLengthStats> LoadGameLengths()
        {
            return LoadFirst<GameLengthStats>(s => s.Id == nameof(GameLengthStats));
        }

        public Task Save(GameLengthStats stat)
        {
            return Upsert(stat, s => s.Id == stat.Id);
        }
    }
}