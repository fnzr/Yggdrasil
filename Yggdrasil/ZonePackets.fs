module Yggdrasil.ZonePackets

let PacketLengthMap = Map.empty
                        .Add(0x0081us, 3)
                        .Add(0X283us, 6)
                        .Add(0X2EBus, 171)
                        .Add(0x00b0us, 256) //ZC_PAR_CHANGE
                        //.Add(0x00b0us, 14)
                        .Add(0x9e7us, 141)
                        .Add(0xadeus, 6) //WEIGHT SOFT CAP
                        .Add(0x1d7us, 15) //ZC_SPRITE_CHANGE2
