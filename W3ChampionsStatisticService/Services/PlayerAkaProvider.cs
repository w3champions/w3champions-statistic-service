using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using W3ChampionsStatisticService.Cache;
using W3ChampionsStatisticService.PlayerProfiles.War3InfoPlayerAkas;
using W3ChampionsStatisticService.PersonalSettings;

namespace W3ChampionsStatisticService.Services
{
    public class PlayerAkaProvider
    {
        private static CachedData<List<PlayerAka>> PlayersAkaCache = new CachedData<List<PlayerAka>>(() => FetchAkasSync(), TimeSpan.FromMinutes(60));
        
        public static List<PlayerAka> FetchAkasSync()
        {
            try
            {
               return FetchAkas().GetAwaiter().GetResult();
            }
            catch
            {
                return new List<PlayerAka>();
            }
        }

        public static async Task<List<PlayerAka>> FetchAkas() // list of all Akas requested from W3info
        { 
            var war3infoApiKey = Environment.GetEnvironmentVariable("WAR3_INFO_API_KEY"); // Change this to secret for dev
            var war3infoApiUrl = "https://warcraft3.info/api/v1/aka/battle_net";

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("client-id", war3infoApiKey);

            var response = await httpClient.GetAsync(war3infoApiUrl);
            string data = await response.Content.ReadAsStringAsync();

            var stringData = JsonSerializer.Deserialize<List<PlayerAka>>(data);
            
            return stringData;
        }

        
        public Player GetPlayerAkaData(string battleTag) // string should be received all lower-case.
        {
            var akas = PlayersAkaCache.GetCachedData();
            var aka = akas.Find(x => x.aka == battleTag);

            if (aka != null) {
                return aka.player;
            }
            return new Player(); // returns an default values if they are not in the database
        }

        public Player GetAkaDataByPreferences(string battletag, PersonalSetting settings)
        {
            var playerAkaData = getAkaData(battletag.ToLower());
            var cleanAkaData = getAkaData(battletag.ToLower());;
            
            if (settings != null && settings.AliasSettings != null)  // Strip the data if the player doesn't want it shown.
            {
                Console.WriteLine("Settings not null");
                if (!settings.AliasSettings.showAka) {
                    playerAkaData.name = null; 
                    playerAkaData.main_race = null; 
                    playerAkaData.country = null;
                    Console.WriteLine("ShowAka was false");
                } else 
                {
                    playerAkaData.name = cleanAkaData.name;
                    playerAkaData.main_race = cleanAkaData.main_race;
                    playerAkaData.country = cleanAkaData.country;
                    Console.WriteLine("ShowAka was true");
                }
            
                if (!settings.AliasSettings.showW3info) 
                {
                    playerAkaData.id = 0;
                    Console.WriteLine("ShowW3info was false");
                } else 
                {
                    playerAkaData.id = cleanAkaData.id;
                    Console.WriteLine("ShowW3info was true");
                }
            
                if (!settings.AliasSettings.showLiquipedia) 
                {
                    playerAkaData.liquipedia = null;
                    Console.WriteLine("ShowLiquipedia was false");
                } else 
                {
                    playerAkaData.liquipedia = cleanAkaData.liquipedia;
                    Console.WriteLine("ShowLiquipedia was true");
                }

            }
            
            return playerAkaData;
        }
    }
}