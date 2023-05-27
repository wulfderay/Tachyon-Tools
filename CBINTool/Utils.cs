using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CBINTool
{
    public static class Utils
    {
        public static byte[] XorDecrypt(byte[] data, byte[] key)
        {
            byte[] decryptedData = new byte[data.Length];

            for (int i = 0; i < data.Length; i++)
            {
                decryptedData[i] = (byte)(data[i] ^ key[i % key.Length]);
            }

            return decryptedData;
        }
        public static byte[] XorEncrypt(byte[] data, byte[] key)
        {
            // XOR is symmetrical, so encryption is the same as decryption.
            return XorDecrypt(data, key);
        }

        public static byte[] HexStringToByteArray(string hexString)
        {
            int length = hexString.Length;
            byte[] byteArray = new byte[length / 2];

            for (int i = 0; i < length; i += 2)
            {
                byteArray[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
            }

            return byteArray;
        }

        public static byte[] StructureToByteArray<T>(T structure)
        {
            int size = Marshal.SizeOf<T>();
            byte[] byteArray = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(structure, ptr, false);
            Marshal.Copy(ptr, byteArray, 0, size);
            Marshal.FreeHGlobal(ptr);

            return byteArray;
        }

    }
}
