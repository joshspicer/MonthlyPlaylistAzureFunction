using System;
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

namespace com.joshspicer
{
    public class SpotifyDetails : TableEntity
    {
        // PartitionKey and RowKey implied.
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string MonthlyPlaylistId { get; set; }

        public void Deconstruct(out string accessToken, out string refreshToken, out DateTime expiresAt, out string monthlyPlaylistId)
        {
            accessToken = AccessToken;
            refreshToken = RefreshToken;
            expiresAt = ExpiresAt;
            monthlyPlaylistId = MonthlyPlaylistId;
        }
    }

    public static class quick_playlist
    {
        static HttpClient httpClient = new HttpClient();

        [FunctionName("monthlyplaylist")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [Table("spotify")] CloudTable cTable,
            ILogger log)
        {
            // Simple Auth
            string env_passphrase = GetEnvironmentVariable("MY_PASSPHRASE");
            string passphrase = req.Query["passphrase"];  // TODO: move to POST
            if (!passphrase.Equals(env_passphrase, StringComparison.Ordinal))
                return new OkObjectResult($"nicee");

            // Validate Input
            string playlist = req.Query["playlist"];
            if (!isValid(playlist))
                return new OkObjectResult("niceee");

            // Grab our data from storage.
            var sDetails = await GetSpotify(cTable);

            // Check if access token expired.
            if (sDetails.ExpiresAt.CompareTo(DateTime.Now) >= 0)
            {
                sDetails.AccessToken = await RefreshTokenAndUpdate(cTable);
            }

            // if (string.IsNullOrEmpty(playlist))
            // {
            //     await AddSongToPlaylist(sDetails);
            // }

            return new OkObjectResult("niceeeee");
        }

        public static bool isValid(string playlist)
        {
            // TODO
            return true;
        }

        /// Add a song to playlist
        public static async Task AddSongToPlaylist(SpotifyDetails sDetails)
        {
            var playlistID = sDetails.MonthlyPlaylistId;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sDetails.AccessToken);

            // Get current song Josh is listening to
            string uri = "https://api.spotify.com/v1/me/player/currently-playing";
            HttpResponseMessage res = await httpClient.GetAsync(uri);
            Console.WriteLine(res);


            // Post song to playlist.
            // uri = $"https://api.spotify.com/v1/users/joshspicer37/playlists/{playlistID}/tracks?uris={songID}";
            // res = await httpClient.PostAsync(uri, new MultipartContent());
            // Console.Write(res);
        }

        /// Update the Token
        public static async Task<string> RefreshTokenAndUpdate(CloudTable cTable)
        {


            // Refresh our access token.
            var clientIdClientSecret = GetEnvironmentVariable("CLIENTIDCLIENTSECRET");
            var refresh_token = GetEnvironmentVariable("REFRESH_TOKEN");

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", clientIdClientSecret);
            var uri = "https://accounts.spotify.com/api/token";
            var res = await httpClient.PostAsync(uri, new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                {"grant_type", "refresh_token"},
                {"refresh_token", refresh_token }
            }));

            // string newAccessToken;

            Console.WriteLine(res);
            Console.Write(res.Content.ReadAsStringAsync());

            // SpotifyDetails sCreds = new SpotifyDetails()
            // {
            //     PartitionKey = "1",
            //     RowKey = "1",
            //     AccessToken = newAccessToken,
            //     ExpiresAt = new DateTime().AddMinutes(55)
            // };

            // await cTable.ExecuteAsync(TableOperation.InsertOrReplace(sCreds));

            return "newAccessToken";
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
