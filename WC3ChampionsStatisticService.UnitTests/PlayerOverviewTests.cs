using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using W3ChampionsStatisticService.CommonValueObjects;
using W3ChampionsStatisticService.Ladder;
using W3ChampionsStatisticService.PlayerProfiles;

namespace WC3ChampionsStatisticService.UnitTests
{
    [TestFixture]
    public class PlayerOverviewTests : IntegrationTestBase
    {
        [Test]
        public async Task LoadAndSave()
        {
            var playerRepository = new PlayerRepository(MongoClient);

            var player = PlayerOverview.Create(new List<PlayerId> { PlayerId.Create("peter#123")}, GateWay.Europe, GameMode.GM_1v1, 0);
            await playerRepository.UpsertPlayerOverview(player);
            var playerLoaded = await playerRepository.LoadOverview(player.Id);

            Assert.AreEqual(player.Id, playerLoaded.Id);
            Assert.AreEqual(GateWay.Europe, playerLoaded.GateWay);
        }


        [Test]
        public async Task LoadAndSearch()
        {
            var playerRepository = new PlayerRepository(MongoClient);

            var player = PlayerOverview.Create(new List<PlayerId> { PlayerId.Create("peter#123")}, GateWay.Europe, GameMode.GM_1v1, 0);
            await playerRepository.UpsertPlayerOverview(player);
            var playerLoaded = (await playerRepository.LoadOverviewLike("PeT", GateWay.Europe)).Single();

            Assert.AreEqual(player.Id, playerLoaded.Id);
            Assert.AreEqual(GateWay.Europe, playerLoaded.GateWay);
        }

        [Test]
        public async Task LoadAndSearch_EmptyString()
        {
            var playerRepository = new PlayerRepository(MongoClient);

            var player = PlayerOverview.Create(new List<PlayerId> { PlayerId.Create("peter#123")}, GateWay.Europe, GameMode.GM_1v1, 0);
            await playerRepository.UpsertPlayerOverview(player);
            Assert.IsEmpty(await playerRepository.LoadOverviewLike("", GateWay.Europe));
        }

        [Test]
        public async Task LoadAndSearch_NulLString()
        {
            var playerRepository = new PlayerRepository(MongoClient);

            var player = PlayerOverview.Create(new List<PlayerId> { PlayerId.Create("peter#123")}, GateWay.Europe, GameMode.GM_1v1, 0);
            await playerRepository.UpsertPlayerOverview(player);
            Assert.IsEmpty(await playerRepository.LoadOverviewLike(null, GateWay.Europe));
        }

        [Test]
        public void UpdateOverview()
        {
            var player = PlayerOverview.Create(new List<PlayerId> { PlayerId.Create("peter#123")}, GateWay.Europe, GameMode.GM_1v1, 0);
            player.RecordWin(true, 1230);
            player.RecordWin(false, 1240);
            player.RecordWin(false, 1250);

            Assert.AreEqual(3, player.Games);
            Assert.AreEqual(1, player.Wins);
            Assert.AreEqual(2, player.Losses);
            Assert.AreEqual("peter#123", player.PlayerIds[0].BattleTag);
            Assert.AreEqual("peter", player.PlayerIds[0].Name);
            Assert.AreEqual("0_peter#123@20_GM_1v1", player.Id);
            Assert.AreEqual(1250, player.MMR);
        }

        [Test]
        public void UpdateOverview_2v2AT()
        {
            var player = PlayerOverview.Create(new List<PlayerId> { PlayerId.Create("peter#123"), PlayerId.Create("wolf#123")}, GateWay.Europe, GameMode.GM_2v2_AT, 0);
            player.RecordWin(true, 1230);

            Assert.AreEqual(1, player.Games);
            Assert.AreEqual(1, player.Wins);

            Assert.AreEqual(GameMode.GM_2v2_AT, player.GameMode);
            Assert.AreEqual(1, player.Games);
            Assert.AreEqual(1, player.Wins);
            Assert.AreEqual(0, player.Losses);
        }

        [Test]
        public async Task UpdateOverview_HandlerUpdate_1v1()
        {
            var matchFinishedEvent = TestDtoHelper.CreateFakeEvent();
            var playerRepository = new PlayerRepository(MongoClient);
            var playOverviewHandler = new PlayOverviewHandler(playerRepository);

            matchFinishedEvent.match.players[0].won = true;
            matchFinishedEvent.match.players[1].won = false;
            matchFinishedEvent.match.players[0].battleTag = "peter#123";
            matchFinishedEvent.match.gateway = GateWay.America;
            matchFinishedEvent.match.gameMode = GameMode.GM_1v1;

            await playOverviewHandler.Update(matchFinishedEvent);

            var playerProfile = await playerRepository.LoadOverview("0_peter#123@10_GM_1v1");

            Assert.AreEqual(1, playerProfile.Wins);
            Assert.AreEqual(0, playerProfile.Losses);
            Assert.AreEqual(GameMode.GM_1v1, playerProfile.GameMode);
        }

        [Test]
        public async Task UpdateOverview_HandlerUpdate_2v2()
        {
            var matchFinishedEvent = TestDtoHelper.CreateFake2v2Event();
            var playerRepository = new PlayerRepository(MongoClient);
            var playOverviewHandler = new PlayOverviewHandler(playerRepository);

            matchFinishedEvent.match.players[0].battleTag = "peter#123";
            matchFinishedEvent.match.players[1].battleTag = "wolf#123";
            matchFinishedEvent.match.gateway = GateWay.America;
            matchFinishedEvent.match.gameMode = GameMode.GM_2v2_AT;

            await playOverviewHandler.Update(matchFinishedEvent);

            var playerProfile = await playerRepository.LoadOverview("0_peter#123@10_wolf#123@10_GM_2v2_AT");

            Assert.AreEqual(1, playerProfile.Wins);
            Assert.AreEqual(0, playerProfile.Losses);
            Assert.AreEqual(GameMode.GM_2v2_AT, playerProfile.GameMode);
        }

        [Test]
        public async Task UpdateOverview_HandlerUpdate_FFA()
        {
            var matchFinishedEvent = TestDtoHelper.CreateFakeFFAEvent();
            var playerRepository = new PlayerRepository(MongoClient);
            var playOverviewHandler = new PlayOverviewHandler(playerRepository);

            await playOverviewHandler.Update(matchFinishedEvent);

            var winners = matchFinishedEvent.match.players.Where(x => x.won);

            Assert.AreEqual(1, winners.Count());

            foreach (var player in winners)
            {
                var playerProfile = await playerRepository.LoadOverview($"0_{player.battleTag}@20_FFA");

                Assert.AreEqual(1, playerProfile.Wins);
                Assert.AreEqual(0, playerProfile.Losses);
                Assert.AreEqual(GameMode.FFA, playerProfile.GameMode);
            }

            var losers = matchFinishedEvent.match.players.Where(x => !x.won);

            Assert.AreEqual(3, losers.Count());

            foreach (var player in losers)
            {
                var playerProfile = await playerRepository.LoadOverview($"0_{player.battleTag}@20_FFA");

                Assert.AreEqual(0, playerProfile.Wins);
                Assert.AreEqual(1, playerProfile.Losses);
                Assert.AreEqual(GameMode.FFA, playerProfile.GameMode);
            }
        }

        [Test]
        public async Task UpdateOverview_HandlerUpdate_1v1_doubleWins()
        {
            var matchFinishedEvent = TestDtoHelper.CreateFakeEvent();
            var playerRepository = new PlayerRepository(MongoClient);
            var playOverviewHandler = new PlayOverviewHandler(playerRepository);

            matchFinishedEvent.match.players[0].battleTag = "peter#123";
            matchFinishedEvent.match.gateway = GateWay.America;
            matchFinishedEvent.match.gameMode = GameMode.GM_1v1;

            await playOverviewHandler.Update(matchFinishedEvent);
            await playOverviewHandler.Update(matchFinishedEvent);
            await playOverviewHandler.Update(matchFinishedEvent);

            var playerProfile = await playerRepository.LoadOverview("0_peter#123@10_GM_1v1");

            Assert.AreEqual(3, playerProfile.Wins);
            Assert.AreEqual(0, playerProfile.Losses);
            Assert.AreEqual(GameMode.GM_1v1, playerProfile.GameMode);
        }
    }
}