module Shuffler.Contracts.Client

open System.Threading.Tasks

type ConfiguredRetriever = Unit -> Task<Result<ArtistsWithAlbums.ArtistWithAlbums, string>>

type AlbumCoverOffset = Albums.Album -> int * int