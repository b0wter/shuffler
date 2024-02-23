namespace Shuffler.Spotify.IntegrationTests

open System
open Shuffler.Contracts.ArtistsWithAlbums
open Xunit
open FsToolkit.ErrorHandling
open dotenv.net
open FsUnit.Xunit
open FsUnit.CustomMatchers

type Client() =
    let environment =
        DotEnv
            .Fluent()
            .WithTrimValues()
            .WithEncoding(System.Text.Encoding.UTF8)
            .WithOverwriteExistingVars()
            .WithProbeForEnv(probeLevelsToSearch = 4)
            .Read()

    let setEnvOrFail key =
        if key |> environment.ContainsKey then environment[key]
        else failwithf $"Environment variable '%s{key}' needs to be set in an .env file placed in the test project's root"
    
    let clientId = setEnvOrFail "SHUFFLER_SPOTIFY_CLIENT_ID"
    let clientSecret = setEnvOrFail "SHUFFLER_SPOTIFY_CLIENT_SECRET"
    let artistId = setEnvOrFail "SHUFFLER_SPOTIFY_ARTIST_ID"
    let artistName = setEnvOrFail "SHUFFLER_SPOTIFY_ARTIST_NAME"
    
    [<Fact>]
    let ``Retrieving artist with albums is success`` () =
        taskResult {
            let! artistWithAlbums = Shuffler.Spotify.Client.createConfiguredRetriever clientId clientSecret artistId ()
            artistWithAlbums.Artist.Name |> should equal artistName
            artistWithAlbums.Albums.Length |> should be (greaterThan 0)
        } |> Task.map (fun x -> match x with Ok _ -> () | Error e -> failwith e)

    [<Fact>]
    let ``Trying to retrieve data without an artist id should fail`` () =
        task {
            let! artistWithAlbums = Shuffler.Spotify.Client.createConfiguredRetriever clientId clientSecret String.Empty ()
            match artistWithAlbums with
            | Ok _ -> failwith "Request should have failed"
            | Error e -> e |> should haveSubstring "artistId"
        }

    [<Fact>]
    let ``Trying to retrieve data without an empty client id should fail`` () =
        task {
            let! artistWithAlbums = Shuffler.Spotify.Client.createConfiguredRetriever String.Empty clientSecret artistId ()
            match artistWithAlbums with
            | Ok _ -> failwith "Request should have failed"
            | Error e -> e |> should haveSubstring "clientId"
        }
        
    [<Fact>]
    let ``Trying to retrieve data without an empty client secret should fail`` () =
        task {
            let! artistWithAlbums = Shuffler.Spotify.Client.createConfiguredRetriever clientId String.Empty artistId ()
            match artistWithAlbums with
            | Ok _ -> failwith "Request should have failed"
            | Error e -> e |> should haveSubstring "clientSecret"
        }
            