using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Linq;
using Newtonsoft.Json;

namespace com.joshspicer
{
    public class SpotifyDetails : TableEntity
    {
        // PartitionKey and RowKey implied.
        public string AccessToken { get; set; }
        public string RefreshTokenLeft { get; set; }
        public string RefreshTokenRight { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string MonthlyPlaylistId { get; set; }
    }

    public static class quick_playlist
    {
        static HttpClient httpClient = new HttpClient();
        static ILogger log;

        [FunctionName("monthlyplaylist")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            [Table("spotify")] CloudTable cTable,
            ILogger _log)
        {
            // Access logging globally.
            log = _log;

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            // Simple Auth
            string env_passphrase = GetEnvironmentVariable("MY_PASSPHRASE");
            string passphrase = data?["passphrase"];
            if (!passphrase.Equals(env_passphrase, StringComparison.Ordinal))
                return new OkObjectResult($"done");

            // Validate Input
            string playlist = data?["playlist"];
            if (playlist != null && !isValid(playlist))
                return new OkObjectResult("done.");

            // Grab our data from storage.
            var sDetails = await GetSpotify(cTable);

            // Check if access token expired.
            if (sDetails.ExpiresAt.ToUniversalTime().CompareTo(DateTime.Now.ToUniversalTime()) <= 0)
            {
                log.LogInformation("[+] Refreshing token.");
                sDetails.AccessToken = await RefreshTokenAndUpdate(cTable, sDetails);
            }
            else
            {
                log.LogInformation("[+] Not refreshing token.");
            }

            // Determine what mode we're in
            if (!string.IsNullOrEmpty(playlist))
            {
                // Provided a playlist. Update monthly playlist.
                await UpdateMonthlyPlaylist(cTable, playlist, sDetails);
            }
            else
            {
                // No playlist provided. Function normally.
                await AddSongToPlaylist(sDetails);
            }

            return new OkObjectResult("Completed.");
        }

        /// Playlist IDs are base-62 [a-zA-Z0-9] encoded
        public static bool isValid(string playlist)
        {
            return playlist.All(Char.IsLetterOrDigit);
        }

        /// Add a song to playlist
        public static async Task AddSongToPlaylist(SpotifyDetails sDetails)
        {
            var playlistID = sDetails.MonthlyPlaylistId;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sDetails.AccessToken);

            // Get current song Josh is listening to
            string uri = "https://api.spotify.com/v1/me/player/currently-playing";
            HttpResponseMessage res = await httpClient.GetAsync(uri);

            if (!res.StatusCode.Equals(System.Net.HttpStatusCode.OK))
                log.LogError($"[-] Error Getting current playing song ({res.StatusCode})");

            string songID = JObject.Parse(await res.Content.ReadAsStringAsync()).SelectToken("item.uri").Value<string>();
            log.LogInformation($"[+] songID={songID}");

            string username = GetEnvironmentVariable("SPOT_USERNAME");

            //Post song to playlist.
            uri = $"https://api.spotify.com/v1/users/{username}/playlists/{playlistID}/tracks?uris={songID}";
            res = await httpClient.PostAsync(uri, new MultipartContent());

            if (!res.StatusCode.Equals(System.Net.HttpStatusCode.Created))
                log.LogError($"[-] Error posting song to playlit ({res.StatusCode})");

            log.LogInformation($"[+] Posting song to playlist: {res.StatusCode}");

        }

        /// Update the monthly playlist
        public static async Task UpdateMonthlyPlaylist(CloudTable cTable, string playlistId, SpotifyDetails sDetails)
        {
            SpotifyDetails sCreds = new SpotifyDetails()
            {
                PartitionKey = "1",
                RowKey = "1",
                MonthlyPlaylistId = playlistId,
                ExpiresAt = sDetails.ExpiresAt
            };
            await cTable.ExecuteAsync(TableOperation.InsertOrMerge(sCreds));
        }

        /// Update the Token
        public static async Task<string> RefreshTokenAndUpdate(CloudTable cTable, SpotifyDetails sDetails)
        {
            // Refresh our access token.
            var clientIdClientSecret = GetEnvironmentVariable("CLIENTIDCLIENTSECRET");
            var refresh_token = $"{sDetails.RefreshTokenLeft}{sDetails.RefreshTokenRight}";

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", clientIdClientSecret);
            var uri = "https://accounts.spotify.com/api/token";
            var res = await httpClient.PostAsync(uri, new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                {"grant_type", "refresh_token"},
                {"refresh_token", refresh_token }
            }));
            log.LogInformation($"[+] Refreshed Token: {res.StatusCode}");

            // Parse out returned access token.
            var newAccessToken = "";
            if (JObject.Parse(await res.Content.ReadAsStringAsync()).TryGetValue("access_token", out var token))
            {
                newAccessToken = token.Value<string>() ?? "";
            }

            SpotifyDetails sCreds = new SpotifyDetails()
            {
                PartitionKey = "1",
                RowKey = "1",
                AccessToken = newAccessToken,
                ExpiresAt = DateTime.Now.AddMinutes(55)
            };

            await cTable.ExecuteAsync(TableOperation.InsertOrMerge(sCreds));
            return newAccessToken;
        }

        /// Get the token
        public static async Task<SpotifyDetails> GetSpotify(CloudTable cTable)
        {
            TableOperation retrieve = TableOperation.Retrieve<SpotifyDetails>("1", "1");
            var result = await cTable.ExecuteAsync(retrieve);

            return ((SpotifyDetails)result.Result);
        }

        /// Get an environment variable
        public static string GetEnvironmentVariable(string name)
        {
            return System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }
    }
}
