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

    let index (album: Shuffler.Contracts.Albums.Album) (hasPreviousAlbum: bool) (coverCenterX: int, coverCenterY: int) =
        let sourceSet = album.AlbumArts |> List.map (fun image -> $"%s{image.Url} %i{image.Width}w") |> String.concat ", "
        let largestAlbumArt = album.AlbumArts |> List.maxBy (_.Width)
        let largestAlbumArtUrl = largestAlbumArt.Url
        let largestAlbumWidth = largestAlbumArt.Width
        let aspectRatio = (largestAlbumArt.Width |> float) / (largestAlbumArt.Height |> float)
        let boxShadowSize = (largestAlbumWidth |> float) / 4.4 // 4.4 is predefined by the design
        div [ _id "background-image-container"; _class "h-100 w-100"; _style $"background-image: url(%s{largestAlbumArtUrl}); background-position: %i{coverCenterX}%% %i{coverCenterY}%%"] [
            
            div [ _id "background-color-overlay"; _class "h-100vh"] [
                
                div [ _id "layout-container"; _class "d-flex justify-content-space-between no-wrap-column"] [

                    div [ _class "d-flex justify-content-center align-items-center"; _style "height: 100px; margin-top: 5vh"] [
                        a [_class "mr-05 p-20"; _href "https://github.com/b0wter/shuffler"] [img [_class "social-button"; _src "/img/github.svg"; _alt "Link to github"]]
                        a [_class "ml-05 p-20"; _href "https://x.com/b0wter"] [img [_class "social-button"; _src "/img/x.svg"; _alt "Link to X"]]
                    ]

                    div [_class "d-flex justify-content-center"; _style "max-height: calc(80vh - 300px)"] [
                        div [ _style $"position: relative; aspect-ratio: %f{aspectRatio}; width: min(95vw, %i{largestAlbumWidth}px)" ] [
                            img [_id "cover-img"; _class "center-in-relative-parent"; _style $"z-index: 2; max-height: 100%%; max-width: min(95vw, %i{largestAlbumWidth}px); border-radius: 20px;"; _srcset sourceSet; _alt "album cover"]
                            div [_id "cover-text"; _class "center-in-relative-parent"; _style "display: none" ] [ encodedText album.Name ]
                            div [_class "center-in-relative-parent"; _style $"z-index: 1; aspect-ratio: 1; height: 100%%; opacity: 0.30; background: linear-gradient(45deg, #DF030E 0%%, #04A5E3 100%%); box-shadow: %f{boxShadowSize}px %f{boxShadowSize}px %f{boxShadowSize}px; border-radius: 20.02px; filter: blur(%f{boxShadowSize}px)"] []
                        ]
                    ]

                    div [] [
                        div [_class "d-flex align-items-center justify-content-center"] [
                            if hasPreviousAlbum then
                                a [ _onclick "history.back()" ] [img [_class "p-15"; _style "padding: 1.5em; transform: scaleX(-1)"; _src "/img/next.svg"; _alt "return to previous album"]]
                            else
                                img [ _style "padding: 1.5rem; height: 25px; width: 25px transform: scaleX(-1)"; _src "/img/empty-circle.svg" ]
                            a [_href album.UrlToOpen] [img [_style "height: 10rem"; _src "img/play.svg"; _alt $"play current album '%s{album.Name}' on Spotify"]]
                            a [_href "/"] [img [_class "p-15"; _src "/img/next.svg"; _alt "get next suggestion"]]
                        ]
                        div [_style "text-decoration: none; color:white";] [
                            div [] [
                                div [_style "font-family: urbanist,sans-serif; text-transform: uppercase"; _class "d-flex justify-content-center align-items-center"] [ encodedText "0 albums on blacklist"]
                                div [_style "font-family: urbanist,sans-serif; font-weight: 1000; height: 5rem; text-transform: uppercase"; _class "d-flex justify-content-center align-items-center"] [
                                    a [_href $"/block/%s{album.Id}"; _class "z-2 non-styled-link d-flex align-items-center mr-10"] [
                                        img [_style "height: 2rem"; _class "mr-05"; _src "/img/block.svg"; _alt "block current album"]
                                        div [] [ encodedText "add"]
                                    ]
                                    a [_href "/clearAll"; _class "z-2 non-styled-link d-flex align-items-center ml-10"] [
                                        img [_style "height: 2rem"; _class "mr-05"; _src "/img/clear-format-white.svg"; _alt "clear album blacklist"]
                                        div [] [ encodedText "clear"]
                                    ]
                                ]
                            ]
                        ]
                    ]

                    div [_class "z-1"; _style "opacity: 0.4; width: 239.62px; height: 244.60px; left: calc(50vw - 120px); top: calc(100vh); position: absolute; transform: rotate(-43.55deg); transform-origin: 0 0"] [
                        div [_class "z-1"; _style "width: 133.77px; height: 179.56px; left: 0; top: 0; position: absolute; transform: rotate(-43.55deg); transform-origin: 0 0; background: #DF030E; box-shadow: 210.86053466796875px 210.86053466796875px 210.86053466796875px; filter: blur(210.86px)"] []
                        div [_class "z-1"; _style "width: 133.54px; height: 183.31px; left: 119.12px; top: -28.67px; position: absolute; transform: rotate(-43.55deg); transform-origin: 0 0; background: #04A5E3; box-shadow: 210.86053466796875px 210.86053466796875px 210.86053466796875px; filter: blur(210.86px)"] []
                    ]
                ]
            ]
        ]

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
            let albumCoverOffsets = ctx.RequestServices.GetService<Shuffler.Contracts.Client.AlbumCoverOffset>()
            let! artistWithAlbums = retriever()
            
            let previous =
                match ctx.GetQueryStringValue("previous") with
                | Ok previousId when artistWithAlbums.Albums |> Array.exists (fun a -> a.Id = previousId) ->
                    Some previousId
                | _ -> None
                    
            let artist = artistWithAlbums.Artist
            let album, coverCenter =
                match id with
                | Some i ->
                    match artistWithAlbums.Albums |> Array.tryFind (fun a -> a.Id = i) with
                    | Some a ->
                        a, a |> albumCoverOffsets
                    | None -> failwithf $"There is no album with the given id '%s{i}'"
                | None ->
                    let a = artistWithAlbums.Albums[random.Next(artistWithAlbums.Albums.Length)]
                    a, a |> albumCoverOffsets
                
            let view = Views.layout artist.Name [Views.index album false coverCenter]
            return! (htmlView view next ctx)
        } |> mapError 

let toJson (_: HttpFunc) (ctx: HttpContext) (result: System.Threading.Tasks.Task<Result<'a, string>>) =
    task {
        match! result with
        | Ok payload -> return! ctx.WriteJsonAsync payload
        | Error e -> return! ctx.WriteJsonAsync {| error = e |}
    }

let apiHandler (id: string option) =
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
            return {| artist = artist; album = album |}
        } |> toJson next ctx
        
let health () =
    fun (_: HttpFunc) (ctx: HttpContext) ->
        task {
            return! ctx.WriteTextAsync "OK"
        }

let webApp =
    choose [
        GET >=>
            choose [
                route "/api/" >=> apiHandler None
                route "/api" >=> apiHandler None
                route "/health" >=> health ()
                route "/health/" >=> health ()
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
            let getEnvOrFail name =
                let value = Environment.GetEnvironmentVariable(name)
                if value |> String.IsNullOrWhiteSpace then failwithf "Cannot start because environment variable %s is not set" name
                else value
            
            let clientId = "SHUFFLER_SPOTIFY_CLIENT_ID" |> getEnvOrFail
            let clientSecret = "SHUFFLER_SPOTIFY_CLIENT_SECRET" |> getEnvOrFail
            let artistId = "SHUFFLER_SPOTIFY_ARTIST_ID" |> getEnvOrFail
            
            Shuffler.Spotify.Client.createConfiguredRetriever clientId clientSecret artistId)
    let configureAlbumCoverOp : Func<IServiceProvider, Shuffler.Contracts.Client.AlbumCoverOffset> = Func<IServiceProvider, Shuffler.Contracts.Client.AlbumCoverOffset>(
        fun (_: IServiceProvider) -> Shuffler.Spotify.Client.albumOffset)
    services.AddSingleton<Shuffler.Contracts.Client.ConfiguredRetriever, Shuffler.Contracts.Client.ConfiguredRetriever>(configuredRetrieverOp) |> ignore
    services.AddSingleton<Shuffler.Contracts.Client.AlbumCoverOffset, Shuffler.Contracts.Client.AlbumCoverOffset>(configureAlbumCoverOp) |> ignore
    
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