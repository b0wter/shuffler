module Shuffler.Contracts.Albums

type AlbumArtUrl = {
    Url: string
    Height: int
    Width: int
}

type Album = {
    Id: string
    Name: string
    UrlToOpen: string
    AlbumArts: AlbumArtUrl list
}