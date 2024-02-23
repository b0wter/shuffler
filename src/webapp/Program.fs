module Shuffler.WebApp.App

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe
open FsToolkit.ErrorHandling

// ---------------------------------
// Views
// ---------------------------------

module Views =
    open Giraffe.ViewEngine

    let layout (artistName: string) (content: XmlNode list) =
        html [] [
            head [] [
                meta [ _charset "utf-8" ]
                meta [ _name "viewport"; _content "width=device-width, initial-scale=1" ]
                meta [ _name "msapplication-TileColor"; _content "#ffffff" ]
                meta [ _name "msapplication-TileImage"; _content "/ms-icon-144x144.png" ]
                meta [ _name "theme-color"; _content "#ffffff" ]
                meta [ _name "description"; _content $"randomly selects an album from '%s{artistName}' for you to to listen to on Spotify" ]
                meta [ _name "og:title"; _content $"%s{artistName} Shuffler" ]
                meta [ _name "og:description"; _content $"randomly selects an album from '%s{artistName}' for you to to listen to on Spotify" ]
                meta [ _name "og:image"; _content "/ms-icon-144x144.png" ]
                meta [ _name "og:type"; _content "website" ]
                
                title []  [ encodedText $"%s{artistName} Shuffler" ]
                
                link [ _rel "apple-touch-icon"; _sizes "57x57"; _href "/apple-icon-57x57.png" ]
                link [ _rel "apple-touch-icon"; _sizes "57x57"; _href "/apple-icon-57x57.png" ]
                link [ _rel "apple-touch-icon"; _sizes "60x60"; _href "/apple-icon-60x60.png" ]
                link [ _rel "apple-touch-icon"; _sizes "72x72"; _href "/apple-icon-72x72.png" ]
                link [ _rel "apple-touch-icon"; _sizes "76x76"; _href "/apple-icon-76x76.png" ]
                link [ _rel "apple-touch-icon"; _sizes "114x114"; _href "/apple-icon-114x114.png" ]
                link [ _rel "apple-touch-icon"; _sizes "120x120"; _href "/apple-icon-120x120.png" ]
                link [ _rel "apple-touch-icon"; _sizes "144x144"; _href "/apple-icon-144x144.png" ]
                link [ _rel "apple-touch-icon"; _sizes "152x152"; _href "/apple-icon-152x152.png" ]
                link [ _rel "apple-touch-icon"; _sizes "180x180"; _href "/apple-icon-180x180.png" ]
                link [ _rel "icon"; _type "image/png"; _sizes "192x192"; _href "/android-icon-192x192.png" ]
                link [ _rel "icon"; _type "image/png"; _sizes "32x32"; _href "/favicon-32x32.png" ]
                link [ _rel "icon"; _type "image/png"; _sizes "96x96"; _href "/favicon-96x96.png" ]
                link [ _rel "icon"; _type "image/png"; _sizes "16x16"; _href "/favicon-16x16.png" ]
                                
                link [ _rel  "stylesheet"
                       _type "text/css"
                       _href "/css/main.css" ]
            ]
            body [] content
        ]

    let index (artist : Shuffler.Contracts.Artists.Artist) (album: Shuffler.Contracts.Albums.Album) (previousAlbumId: string option) =
        let sourceSet = album.AlbumArts |> List.map (fun image -> $"")
        [
            div [ _class "container center-text" ] [
                div [ _id "header" ] [
                    a [ _class "light-gray"; _href "https://github.com/b0wter/spotify_shuffle" ] [ text "GitHub" ]
                    a [ _class "light-gray ml-1"; _href "https://github.com/b0wter/spotify_shuffle" ] [ text "Twitter" ]
                    p [ _id "clear-blacklist"; _class "small" ] [
                        a [ _class "light-gray"; _href "#"; _onclick "var shouldDelete = confirm('Remove all items from blacklist?'); if(shouldDelete) { document.getElementById('clear-blacklist').children[0].innerHTML = 'clear blacklist (0 elements)'; document.cookie = 'ignoredEpisodes' +'=; Path=/; Expires=Thu, 01 Jan 1970 00:00:01 GMT;'; }; " ] [ text "clear blacklist (XYZ elements)" ]
                    ]
                    if previousAlbumId.IsSome then
                        div [ _id "blacklist-alert"; _class "alert-warning mb-2" ] [ text $"%s{previousAlbumId.Value} is now blacklisted" ] [
                            a [ _class "alert-warning"; _href $"/?undo=%s{previousAlbumId.Value}" ] [ text "undo" ]
                        ]
                    
                ]
                
                a [] [
                    img [ _class "max-width-90" _srcset=   
                ]
            ]
        ]
(*
<div class="flex-container">
    <div class="row"> 
        <div class="flex-item">1</div>
        <div class="flex-item">2</div>
        <div class="flex-item">3</div>
        <div class="flex-item">4</div>
    </div>
</div>
*)            
        ] |> layout artist.Name

// ---------------------------------
// Web app
// ---------------------------------

let mapError (result: System.Threading.Tasks.Task<Result<HttpContext option, string>>) : System.Threading.Tasks.Task<HttpContext option> =
    task {
        match! result with
        | Ok o -> return o
        | Error e -> return failwith e
    }

let indexHandler (id: string option) =
    let random = Random()
    fun (next: HttpFunc) (ctx: HttpContext) ->
        taskResult {
            let retriever = ctx.RequestServices.GetService<Shuffler.Contracts.Client.ConfiguredRetriever>()
            let! artistWithAlbums = retriever()
            let artist = artistWithAlbums.Artist
            let album =
                match id with
                | Some i ->
                    match artistWithAlbums.Albums |> Array.tryFind (fun a -> a.Id = i) with
                    | Some a -> a
                    | None -> failwithf $"There is no album with the given id '%s{i}'"
                | None -> artistWithAlbums.Albums[random.Next(artistWithAlbums.Albums.Length)]
            let view = Views.index artist album
            return! (htmlView view next ctx)
        } |> mapError 

let webApp =
    choose [
        GET >=>
            choose [
                route "/" >=> indexHandler None
                routef "/%s" (fun s -> indexHandler (Some s))
            ]
        setStatusCode 404 >=> text "Not Found" ]

// ---------------------------------
// Error handler
// ---------------------------------

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

// ---------------------------------
// Config and Main
// ---------------------------------

let configureCors (builder : CorsPolicyBuilder) =
    builder
        .WithOrigins(
            "http://localhost:5000",
            "https://localhost:5001")
       .AllowAnyMethod()
       .AllowAnyHeader()
       |> ignore

let configureApp (app : IApplicationBuilder) =
    let env = app.ApplicationServices.GetService<IWebHostEnvironment>()
    (match env.IsDevelopment() with
    | true  ->
        app.UseDeveloperExceptionPage()
    | false ->
        app .UseGiraffeErrorHandler(errorHandler)
            .UseHttpsRedirection())
        .UseCors(configureCors)
        .UseStaticFiles()
        .UseGiraffe(webApp)

let configureServices (services : IServiceCollection) =
    let configuredRetrieverOp : Func<IServiceProvider, Shuffler.Contracts.Client.ConfiguredRetriever> = Func<IServiceProvider, Shuffler.Contracts.Client.ConfiguredRetriever>(
        fun (serviceProvider: IServiceProvider) ->
            let clientId = Environment.GetEnvironmentVariable("SHUFFLER_SPOTIFY_CLIENT_ID")
            let clientSecret = Environment.GetEnvironmentVariable("SHUFFLER_SPOTIFY_CLIENT_SECRET")
            let artistId = Environment.GetEnvironmentVariable("SHUFFLER_SPOTIFY_ARTIST_ID")
            Shuffler.Spotify.Client.createConfiguredRetriever clientId clientSecret artistId)
    services.AddSingleton<Shuffler.Contracts.Client.ConfiguredRetriever, Shuffler.Contracts.Client.ConfiguredRetriever>(configuredRetrieverOp) |> ignore
    
    services.AddCors()    |> ignore
    services.AddGiraffe() |> ignore

let configureLogging (builder : ILoggingBuilder) =
    builder.AddConsole()
           .AddDebug() |> ignore

[<EntryPoint>]
let main args =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot     = Path.Combine(contentRoot, "WebRoot")
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(
            fun webHostBuilder ->
                webHostBuilder
                    .UseContentRoot(contentRoot)
                    .UseWebRoot(webRoot)
                    .Configure(Action<IApplicationBuilder> configureApp)
                    .ConfigureServices(configureServices)
                    .ConfigureLogging(configureLogging)
                    |> ignore)
        .Build()
        .Run()
    0