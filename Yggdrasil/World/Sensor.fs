module Yggdrasil.World.Sensor

open System
open Yggdrasil.Types
open Yggdrasil.Navigation.Maps
type Entity = {
    Id: Id
    Type: EntityType
    Name: string
}

type Location = {
    Id: Id
    Map: Map
    Position: Position
}

type Health = {
    Id: Id
    HP: int
    MaxHP: int
}

type Movement = {
    Id: Id
    Map: Map
    Speed: float
    Origin: Position
    Target: Position
    Delay: float
}
    
type Message =
    | New of Entity
    | Location of Location
    | Movement of Movement
    | Health of Health

type Sensor = {
    Entities: IObservable<Entity>
    Messages: IObservable<Message>
    Locations: IObservable<Location>
    Health: IObservable<Health>
}

type Player = {
    Id: Id
    Request: Request -> unit
    Sensor: Sensor
    Subscriptions: IDisposable list
}