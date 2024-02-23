module Shuffler.Contracts.ArtistsWithAlbums

type ArtistWithAlbums = {
    Artist: Artists.Artist
    Albums: Albums.Album array
}