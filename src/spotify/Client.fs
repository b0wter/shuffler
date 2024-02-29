namespace Shuffler.Spotify

open System
open System.Threading.Tasks
open FsToolkit.ErrorHandling
open Shuffler.Contracts.Client
open SpotifyAPI.Web
open Shuffler.Contracts.Albums
open Shuffler.Contracts.Artists
open Shuffler.Contracts.ArtistsWithAlbums

module Client =
    type private TemporaryClient = {
        ValidUntil: DateTime
        Client: SpotifyClient
    }
    
    let private isClientStillValid (client: TemporaryClient) =
        client.ValidUntil > DateTime.UtcNow
        
    let private login : clientId:string * clientSecret:string -> Task<Result<SpotifyClient, string>> =
        let mutable tempClient : TemporaryClient option = None
        fun (clientId, clientSecret) ->
            task {
                try
                    match tempClient with
                    | Some temp when isClientStillValid temp -> return (Ok temp.Client)
                    | Some _ | None ->
                        let config = SpotifyClientConfig.CreateDefault()
                        let request = ClientCredentialsRequest(clientId, clientSecret)
                        let! response = OAuthClient(config).RequestToken(request)
                        let client = SpotifyClient(config.WithToken(response.AccessToken))
                        do tempClient <- Some { ValidUntil = response.CreatedAt + TimeSpan.FromSeconds(response.ExpiresIn); Client = client}
                        return (Ok client)
                with
                | exn -> return Error exn.Message
            }
    
    let private artistDetails (client: SpotifyClient) (artistId: string) =
        task {
            try
                let! fullArtist = client.Artists.Get(artistId)
                let artist = { Artist.Name = fullArtist.Name; Id = fullArtist.Id }
                return Ok artist
            with
            | exn -> return Error exn.Message
        }
        
    let private albums (client: SpotifyClient) artistId : Task<Result<Album array, string>> =
        let convertImage (image: Image) : AlbumArtUrl =
            { Url = image.Url; Width = image.Width; Height = image.Height }
            
        let convertAlbum (album: SimpleAlbum) =
            let urlToOpen = (album.ExternalUrls |> Seq.find (fun pair -> pair.Key = "spotify")).Value
            let imageUrls = album.Images |> Seq.map convertImage |> List.ofSeq
            { Id = album.Id; Name = album.Name; UrlToOpen = urlToOpen; AlbumArts = imageUrls }
        
        let rec get (offset: int) (pageSize: int) acc : Task<SimpleAlbum seq list> =
            task {
                let request = ArtistsAlbumsRequest(Limit = pageSize, Offset = offset)
                let! result = client.Artists.GetAlbums(artistId, request)
                let newAcc : SimpleAlbum seq list = result.Items :: acc
                
                if result.Items.Count = pageSize then
                    return! (get (offset + pageSize) pageSize newAcc)
                else
                    return newAcc
            }
    
        task {
            try
                let! response = get 0 50 []
                let albums = response |> Seq.collect id |> Seq.map convertAlbum |> Array.ofSeq
                return Ok albums
            with
            | exn -> return (Error exn.Message)
        }
        
    let artistWithAlbums =
        let cache = System.Collections.Generic.Dictionary<string, ArtistWithAlbums>()
        fun clientId clientSecret artistId ->
            taskResult {
                if   clientId     |> String.IsNullOrWhiteSpace then return! (Error "clientId is empty")
                elif clientSecret |> String.IsNullOrWhiteSpace then return! (Error "clientSecret is empty")
                elif artistId     |> String.IsNullOrWhiteSpace then return! (Error "artistId is empty")
                else
                    try
                        if artistId |> (not << cache.ContainsKey) then
                            let! client = login (clientId, clientSecret)
                            let! artist = artistDetails client artistId
                            let! albums = albums client artistId
                            cache.Add(artistId, { Artist = artist; Albums = albums })
                        return cache[artistId]
                    with
                    | exn -> return! (Error exn.Message)
            }

    let createConfiguredRetriever clientId clientSecret artistId : ConfiguredRetriever =
        fun () -> artistWithAlbums clientId clientSecret artistId
            
    let albumOffset (album: Album) : int * int =
        System.Text.RegularExpressions.Regex.Match(album.Name, "\d{3}").Captures
        |> Seq.tryHead
        |> Option.map (fun hit ->
            let number = hit.Value |> Int32.Parse
            if number <= 123 then (50, 50)
            else (60, 50))
        |> Option.defaultValue (60, 50)
        