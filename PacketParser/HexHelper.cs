using System;

namespace PacketParser
{//0xE30A2200000000005331303030000000000000000000000000000000746F6B656E00
    public static class HexHelper
    {
        public static byte[] HexStringToByteArray(string hex) {
            if (hex.StartsWith("0x"))
            {
                hex = hex.Substring(2);
            }
            if (hex.Length % 2 == 1)
                throw new Exception("The binary key cannot have an odd number of digits");
            byte[] arr = new byte[hex.Length >> 1];
            for (int i = 0; i < hex.Length >> 1; ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
            }
            return arr;
        }

        static int GetHexVal(char hex) {
            int val = hex;
            return val - (val < 58 ? 48 : 55);
        }
    }
}