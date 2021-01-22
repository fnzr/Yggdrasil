module Yggdrasil.IO.Incoming

open System
open FSharpPlus.Data
open NLog
open Yggdrasil.Game.Event
open Yggdrasil.Types
open Yggdrasil.Utils
open Yggdrasil.Game
open Yggdrasil.PacketParser.Decoder
open Yggdrasil.PacketParser.Attributes
open Yggdrasil.PacketParser.Location
open Yggdrasil.PacketParser.Spawn
open Yggdrasil.PacketParser.Combat
//open Yggdrasil.Game.Player
open FSharpPlus.Lens
let Logger = LogManager.GetLogger "Incoming"

let AddSkill data (world: World) =
    let rec ParseSkill bytes skills =        
        match bytes with
        | [||] -> skills 
        | _ ->
            //TODO SkillRaw -> Skill
            let (skill, leftover) = MakePartialRecord<Skill> data [|24|]
            ParseSkill leftover (skill :: skills)
    setl World._Player
        {world.Player with Skills = ParseSkill data world.Player.Skills}
    <| world, [||]
    
let ConnectionAccepted (data: byte[]) (world: World) =
    let p = world.Player
    let (x, y, _) = UnpackPosition data.[4..]
    {world with
        TickOffset = data.[0..] |> ToUInt32 |> int64 |> (-) (Connection.Tick())
        Player = setl Player._Position (int x, int y) p
    }, [| ConnectionStatus Active |]
    
let WeightSoftCap (data: byte[]) (world: World) =
    world.Player.Inventory.WeightSoftCap <- ToInt32 data
    world, [||]
    
           
let ItemOnGroundAppear data (world: World) =
    let info = MakeRecord<GroundItemRaw> data
    {world
     with ItemsOnGround = {
        Id = info.Id
        NameId = info.NameId
        Identified = info.Identified > 0uy
        Position = (int info.PosX, int info.PosY)
        Amount = info.Amount} :: world.ItemsOnGround
    }, [||]
    
let ItemOnGroundDisappear data (world: World) =
    let id = ToInt32 data
    {world
      with ItemsOnGround = List.filter (fun i -> i.Id <> id) world.ItemsOnGround}, [||]
    
let WorldId w = w, [||]   
let PacketReceiver callback (packetType, (packetData: ReadOnlyMemory<byte>)) =
    let data = packetData.ToArray()
    callback <|
        match packetType with
        | 0x13aus -> ParameterChange Parameter.AttackRange data.[2..]
        | 0x00b0us -> ParameterChange (data.[2..] |> ToParameter) data.[4..] 
        | 0x0141us -> ParameterChange (data.[2..] |> ToParameter) data.[4..]
        | 0xacbus -> ParameterChange (data.[2..] |> ToParameter) data.[4..]        
        | 0xadeus -> WeightSoftCap data.[2..]
        | 0x9ffus -> NonPlayerSpawn data.[4..]
        | 0x9feus -> PlayerSpawn data.[4..]        
        | 0x9fdus -> WalkingUnitSpawn data.[4..]
        | 0x0080us -> UnitDisappear data.[2..]
        | 0x10fus -> AddSkill data.[4..]        
        | 0x0087us -> PlayerWalk data.[2..] callback
        | 0x0086us -> UnitWalk data.[2..] callback        
        | 0x07fbus -> SkillCast data.[2..] callback
        | 0x0088us -> MoveUnit data.[2..] callback
        | 0x0977us -> UpdateMonsterHP data.[2..]        
        | 0x008aus -> DamageDealt data.[2..] callback
        | 0x08c8us -> DamageDealt2 data.[2..] callback        
        | 0x0addus -> ItemOnGroundAppear data.[2..]
        | 0x00a1us -> ItemOnGroundDisappear data.[2..]        
        | 0x2ebus -> ConnectionAccepted data.[2..]
        | 0x0091us -> MapChange data.[2..]
        | 0x007fus -> WorldId //world.TickOffset <- Convert.ToInt64(ToUInt32 data.[2..]) - Connection.Tick(); world
        | 0x0adfus (* ZC_REQNAME_TITLE *) -> WorldId
        | 0x080eus (* ZC_NOTIFY_HP_TO_GROUPM_R2 *) -> WorldId
        | 0x0bdus (* ZC_STATUS *) -> WorldId
        | 0x121us (* cart info *) -> WorldId
        | 0xa0dus (* inventorylistequipType equipitem_info size 57*) -> WorldId
        | 0x0a9bus (* list of items in the equip switch window *) -> WorldId
        | 0x099bus (* ZC_MAPPROPERTY_R2 *) -> WorldId
        | 0x00b4us (* ZC_SAY_DIALOG *) -> WorldId
        | 0x00b5us (* ZC_WAIT_DIALOG *) -> WorldId
        | 0x00b7us (* ZC_MENU_LIST *) -> WorldId
        | 0x0a30us (* ZC_ACK_REQNAMEALL2 *) -> WorldId
        | 0x283us | 0x9e7us (* ZC_NOTIFY_UNREADMAIL *) | 0x1d7us (* ZC_SPRITE_CHANGE2 *)
        | 0x008eus (* ZC_NOTIFY_PLAYERCHAT *) | 0xa24us (* ZC_ACH_UPDATE *) | 0xa23us (* ZC_ALL_ACH_LIST *)
        | 0xa00us (* ZC_SHORTCUT_KEY_LIST_V3 *) | 0x2c9us (* ZC_PARTY_CONFIG *) | 0x02daus (* ZC_CONFIG_NOTIFY *)
        | 0x02d9us (* ZC_CONFIG *) | 0x00b6us (* ZC_CLOSE_DIALOG *) | 0x01b3us (* ZC_SHOW_IMAGE2 *)
        | 0x00c0us (* ZC_EMOTION *) -> WorldId
        | 0x0081us -> WorldId//Logger.Error ("Forced disconnect. Code %d", data.[2])
        | unknown -> Logger.Warn("Unhandled packet {packetType:X}", unknown, data.Length);
                        WorldId
