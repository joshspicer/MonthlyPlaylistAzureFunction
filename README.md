# MonthlyPlaylistAzureFunction
Automating Spotify playlist management with an Azure Function

> “Hey Siri - add this song to my monthly playlist!"
>
> ![1](https://joshspicer.com/assets/resources-playlist-azure/1.PNG)
>
> Not helpful...

For a long time i've found it inconvenient that, in order to add songs to a playlist I need to, (1) unlock my phone, (2) open Spotify, (3) usually navigate through 4 pages of menus, (4) sift through (an unsearchable) list of playlists, and (5) tap on the right playlist.

I want there to be a better way to add songs to my "monthly" playlist. As you'll see in this guide I decided to leverage a Siri Shortcut to trigger a simple Azure Function, dropping the current track into the right playlist with _only one tap_.

I offer two modes:

If you submit a simple POST, it will execute the function and add my currently playing song to the previously defined "monthly" playlist.

```bash
curl -X POST \
-d '{"passphrase":"letmein"}' \
-H "Content-Type: application/json" \
http://localhost:7071/api/playlist
```

You can also specify a playlist ID, we will store that ID in the function's state. All future song POSTs will be placed into this playlist. It assumes you have write access to the provided playlist.

```bash
curl -X POST \
-d '{"passphrase":"letmeinplzz", \
			"playlist": "0OBq0h6EjCmaPXjeCB4IlM"}' \
-H "Content-Type: application/json" \
http://localhost:7071/api/playlist
```

Playlist IDs are easily found by "sharing" a song or playlist from the Spotify app. This is all [documented](https://developer.spotify.com/documentation/web-api/reference/object-model/) by Spotify.

## Writeup

For the full writeup, check out https://joshspicer.com/playlists-azure
