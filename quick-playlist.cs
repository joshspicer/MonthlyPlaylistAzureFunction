using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace com.joshspicer
{
    public static class quick_playlist
    {
        [FunctionName("quick_playlist")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            // log.LogInformation("C# HTTP trigger function processed a request.");

            // TODO: input validation
            string song = req.Query["song"];
            string passphrase = req.Query["passphrase"];
            string updatePlaylistString = req.Query["playlist"];

            // string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            // dynamic data = JsonConvert.DeserializeObject(requestBody);

            song = song ?? data?.song;

            if (!string.IsNullOrEmpty(song))
            {
                // Add a message to the output collection.
            }

            string responseMessage = string.IsNullOrEmpty(song)
                ? $"No song given."
                : $"song={song}";

            return new OkObjectResult(responseMessage);
        }

        public static void AddSongToPlaylist(string playlistID, string songID)
        {

        }
    }
}
