module Yggdrasil.Messenger

open NLog

type Player = {
    AccountId: int
    StatusMap: Map<string, int>
}
let Logger = LogManager.GetCurrentClassLogger()

type Messenger = MailboxProcessor<Player -> Player>

let StatusUpdate name value =
    fun (state: Player) -> {state with StatusMap = state.StatusMap.Add(name, value)}

        
let CreatePlayerMessageHandler accountId =
    MailboxProcessor.Start(
        fun (inbox: MailboxProcessor<Player -> Player>) ->
        let rec loop (state: Player) = async {
            let! event = inbox.Receive()
            Logger.Warn("State: {state}", state)
            return! loop (event state)
        }
        loop {AccountId = 0; StatusMap = Map.empty}
    )
