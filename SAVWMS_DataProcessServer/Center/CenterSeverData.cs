using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using static System.Console;

namespace SAVWMS
{
    [Serializable]
    public class DeviceData : TypeData
    {
        public DeviceData Newdata = null;
        public barvolumedata barvolumedata;
        public NetIP ip;
        public string IP;
        public Boolean Live = false;


        public DeviceData()
        {
            ip = new NetIP();
            barvolumedata = new barvolumedata();
        }
    }

    [Serializable]
    public class UserData : TypeData
    {
        public NetIP ip;
        public string IP;
        public string DeviceID;

        public Boolean Live = false;
        public UserData()
        {
            DeviceID = null;
            ip = new NetIP();
        }
    }

    public class CenterSeverData : TypeData
    {
        public NetIP ip;
        public DeviceData[] Devicedata;
        public UserData[] Userdata;

        public CenterSeverData(int Max)
        {
            ip = new NetIP();            
            Devicedata = new DeviceData[Max];            
            Userdata = new UserData[Max];
            for (int i = 0; i < Max-1; i++)
            {
                
                Devicedata[i] = null;
                Userdata[i] = null;
            }
        }
    }

    public class CenterServerNet : CenterNet
    {
        CenterSeverData Data;
        IPAddress ip;
        IPEndPoint point;
        CenterManager centerManager;

        public CenterServerNet(ref CenterSeverData data, ref CenterManager s)
        {
            typenet = TypeNet.CenterSever;
            ip = IPAddress.Parse(data.ip.IP);
            
            point = new IPEndPoint(ip, data.ip.Point);
            centerManager = s;
            Data = data;
        }

        /// <summary>
        /// 创建一个服务器socket对象，走到监听端口这一步，新建线程，并将服务器socket对象传递过去，
        /// 用于实时创建连接的客户端socket
        /// </summary>
        /// <returns></returns>
        public bool serverLink()
        {
            //创建监听用的Socket
            /*
               AddressFamily.InterNetWork：使用 IP4地址。
               SocketType.Stream：支持可靠、双向、基于连接的字节流，而不重复数据。
               此类型的 Socket 与单个对方主机进行通信，并且在通信开始之前需要远程主机连接。
               Stream 使用传输控制协议 (Tcp) ProtocolType 和 InterNetworkAddressFamily。
               ProtocolType.Tcp：使用传输控制协议。
             */
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.Bind(point);
                socket.Listen(10);
                Console.WriteLine("服务器开始监听");

                Thread Accept = new Thread(AcceptInfo);
                Accept.IsBackground = true;
                Accept.Start(socket);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return true;
        }

        /// <summary>
        ///每有一个客户端连接，就会创建一个socket对象用于保存客户端传过来的套接字信息
        /// </summary>
        /// <param name="o"></param>
        void AcceptInfo(object o)
        {
            Socket socket = o as Socket;
            while (true)
            {
                try
                {
                    //没有客户端连接时，accept会处于阻塞状态
                    Socket tSocket = socket.Accept();

                    string point = tSocket.RemoteEndPoint.ToString();
                    Console.WriteLine(point + "连接成功！");

                    Thread th = new Thread(ReceiveMsg);
                    th.IsBackground = true;
                    th.Start(tSocket);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    break;
                }
            }
        }
        void ReceiveMsg(object o)
        {
            Socket client = o as Socket;

            void ipinfo()
            {
                byte[] buf = new byte[1024 * 1024];
                client.Receive(buf);
                Package package = BytesToPackage(buf);
                if (package.message == Messagetype.codeus)
                {
                    PackageToUserData packageToUserData = new PackageToUserData(NewUser);
                    int i = 0;
                    foreach (IPList ip in centerManager.UserList)
                    {
                        if (ip.ID == null)
                        {
                            Data.Userdata[i] = packageToUserData(package);
                            Data.Userdata[i].IP = client.RemoteEndPoint.ToString();
                            Data.Userdata[i].Live = true;
                            Data.Userdata[i].socket = client;

                            centerManager.UserList[i].ID = Data.Userdata[i].ID;
                            centerManager.UserList[i].IP = client.RemoteEndPoint.ToString();
                            break;
                        }
                        i++;
                    }
                    //方法存留
                    Send(CreatIPListToPackage(Messagetype.codeus, centerManager.iplist), client);
                }
                else
                {
                    if (package.message == Messagetype.ID)
                    {
                        PackageToDeviceData packageToDeviceData = new PackageToDeviceData(NewDevice);

                        int i = 0;
                        foreach (IPList ip in centerManager.iplist)
                        {
                            if (ip.ID == null)
                            {
                                Data.Devicedata[i] = packageToDeviceData(package);
                                Data.Devicedata[i].IP = client.RemoteEndPoint.ToString();
                                Data.Devicedata[i].Live = true;
                                Data.Devicedata[i].socket = client;

                                centerManager.iplist[i].ID = Data.Devicedata[i].ID;
                                centerManager.iplist[i].IP = client.RemoteEndPoint.ToString();
                                break;
                            }
                            i++;
                        }
                    }
                }

            }
            ipinfo();
        }
       
        public bool Send(Package package, Socket s)
        {
            try
            {
                byte[] bytes = null;
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(ms, package);
                    ms.Flush();
                    bytes = ms.ToArray();
                }
                s.Send(bytes, bytes.Length, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            return true;
        }
        public static Package CreatIPListToPackage(Messagetype messagetype, IPList[] ipl)
        {
            Package package = new Package();
            package.message = messagetype;
            package.data = null;
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(ms, ipl);
                    ms.Flush();
                    package.data = ms.ToArray();
                }
                return package;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return package;
        }

        UserData NewUser(Package package)
        {
            UserData data = new UserData();
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(package.data, 0, package.data.Length);
                ms.Flush();
                ms.Position = 0;
                BinaryFormatter bf = new BinaryFormatter();
                data = (UserData)bf.Deserialize(ms);
            }
            return data;
        }
        DeviceData NewDevice(Package package)
        {
            DeviceData data = new DeviceData();
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(package.data, 0, package.data.Length);
                ms.Flush();
                ms.Position = 0;
                BinaryFormatter bf = new BinaryFormatter();
                data = (DeviceData)bf.Deserialize(ms);
            }
            return data;
        }

        void NewUserData(Package package, int UserID)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(package.data, 0, package.data.Length);
                    ms.Flush();
                    ms.Position = 0;
                    BinaryFormatter bf = new BinaryFormatter();

                    Data.Userdata[UserID] = (UserData)bf.Deserialize(ms);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }
       
        public Package CreatCodeToPackage(Codemode codemode)
        {
            Package package = new Package();
            package.message = Messagetype.order;
            switch (codemode)
            {
                case Codemode.stop: package.data = Encoding.UTF8.GetBytes("0"); break;
                case Codemode.play: package.data = Encoding.UTF8.GetBytes("1"); break;
            }
            return package;
        }
    }

}
