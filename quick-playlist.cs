using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace com.joshspicer
{

    public class SpotifyCreds : TableEntity
    {
        // PartitionKey and RowKey implied.
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }


    public static class quick_playlist
    {
        [FunctionName("monthlyplaylist")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            // [Table("spotify", "1", "1")] SpotifyCreds creds,
            [Table("spotify")] CloudTable cTable,
            ILogger log)
        {

            await UpdateSpotifyToken("sammmmmmmy", cTable);

            log.LogInformation(await GetSpotifyToken(cTable));

            // Simple Auth
            string env_passphrase = GetEnvironmentVariable("MY_PASSPHRASE");
            string passphrase = req.Query["passphrase"];  // TODO: move to POST
            if (passphrase != env_passphrase)
            {
                return new BadRequestObjectResult("no");
            }

            // TODO: input validation
            string song = req.Query["song"];
            string updatePlaylistString = req.Query["playlist"];

            if (!string.IsNullOrEmpty(song))
            {
                // Add a message to the output collection.
            }

            string responseMessage = string.IsNullOrEmpty(song)
                ? $"No song given."
                : $"song={song}";

            return new OkObjectResult(responseMessage);
        }

        /// Add a song to playlist
        public static void AddSongToPlaylist(string playlistID, string songID)
        {

        }

        /// Update the Token
        public static async Task UpdateSpotifyToken(string aToken, CloudTable cTable)
        {
            SpotifyCreds sCreds = new SpotifyCreds()
            {
                PartitionKey = "1",
                RowKey = "1",
                AccessToken = aToken
            };

            await cTable.ExecuteAsync(TableOperation.InsertOrReplace(sCreds));
        }

        /// Get the token
        public static async Task<string> GetSpotifyToken(CloudTable cTable)
        {
            TableOperation retrieve = TableOperation.Retrieve<SpotifyCreds>("1", "1");
            var result = await cTable.ExecuteAsync(retrieve);

            return ((SpotifyCreds)result.Result).AccessToken;
        }

        /// Get an environment variable
        public static string GetEnvironmentVariable(string name)
        {
            return System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }
    }
}
