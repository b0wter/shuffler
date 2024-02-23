module Shuffler.Contracts.Client

open System.Threading.Tasks

type ConfiguredRetriever = Unit -> Task<Result<ArtistsWithAlbums.ArtistWithAlbums, string>>