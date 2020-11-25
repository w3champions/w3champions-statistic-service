using System.Threading.Tasks;
using MongoDB.Bson;
using NUnit.Framework;
using W3ChampionsStatisticService.Matches;

namespace WC3ChampionsStatisticService.UnitTests
{
    [TestFixture]
    public class MatchupDetailTests : IntegrationTestBase
    {
        [Test]
        public async Task LoadDetails_NotDetailsAvailable()
        {
            var matchFinishedEvent = TestDtoHelper.CreateFakeEvent();
            matchFinishedEvent.match.id = "nmhcCLaRc7";
            matchFinishedEvent.Id = ObjectId.GenerateNewId();
            var matchRepository = new MatchRepository(MongoClient, new OngoingMatchesCache(MongoClient));

            await matchRepository.Insert(Matchup.Create(matchFinishedEvent));

            var result = await matchRepository.LoadDetails(matchFinishedEvent.Id);
            Assert.AreEqual("nmhcCLaRc7", result.Match.MatchId);
        }

        [Test]
        public async Task LoadDetails()
        {
            var matchFinishedEvent = TestDtoHelper.CreateFakeEvent();
            matchFinishedEvent.match.id = "nmhcCLaRc7";
            matchFinishedEvent.Id = ObjectId.GenerateNewId();
            matchFinishedEvent.result.players[0].heroes[0].icon = "Archmage";
            matchFinishedEvent.result.players[1].heroes[0].icon = "Warden";

            await InsertMatchEvent(matchFinishedEvent);

            var matchRepository = new MatchRepository(MongoClient, new OngoingMatchesCache(MongoClient));

            await matchRepository.Insert(Matchup.Create(matchFinishedEvent));

            var result = await matchRepository.LoadDetails(matchFinishedEvent.Id);

            Assert.AreEqual("nmhcCLaRc7", result.Match.MatchId);
            Assert.AreEqual("Archmage", result.PlayerScores[0].Heroes[0].icon);
            Assert.AreEqual("Warden", result.PlayerScores[1].Heroes[0].icon);
        }
    }
}