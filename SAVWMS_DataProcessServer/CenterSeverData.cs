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
        public bool newdatachange()
        {
            if (flag) return true;
            else return false;
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
        public bool newdatachange()
        {
            if (flag)
                return true;
            else return false;
        }
    }

    public class CenterSeverData : TypeData
    {
        public NetIP ip;

        public CenterSeverData()
        {
            ip = new NetIP();
            Devicedata = new DeviceData[100];
            Userdata = new UserData[100];
            for (int i = 0; i < 100; i++)
            {
                Devicedata[i] = null;
                Userdata[i] = null;
            }
        }
        public DeviceData[] Devicedata;
        public UserData[] Userdata;
    }

    public class CenterServerNet : CenterNetData
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

                //这个线程用于实例化socket，每当一个子端connect时，new一个socket对象并保存到相关数据集合
                Thread thread = new Thread(AcceptInfo);
                thread.IsBackground = true;
                thread.Start(socket);
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
                    dic.Add(point, tSocket);

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
                Console.WriteLine("正在等待消息");
                client.Receive(buf);
                Console.WriteLine("接收到了");


                Package package = BytesToPackage(buf);


                Console.WriteLine(package.data);

                if (package.message == Messagetype.codeus)
                {
                    Console.WriteLine("处理数据中");
                    PackageToUserData packageToUserData = new PackageToUserData(NewUser);
                    int i = 0;
                    foreach (IPList ip in centerManager.UserList)
                    {
                        if (ip.ID == null)
                        {
                            Data.Userdata[i] = packageToUserData(package);
                            Data.Userdata[i].IP = client.RemoteEndPoint.ToString();
                            Data.Userdata[i].Live = true;

                            centerManager.UserList[i].ID = Data.Userdata[i].ID;
                            centerManager.UserList[i].IP = client.RemoteEndPoint.ToString();
                            Thread thread = new Thread(UserReceive);
                            thread.IsBackground = true;
                            thread.Start(client);
                            break;
                        }
                        i++;
                    }
                    Console.WriteLine("已发送子端数据");
                    Send(CreatIPListToPackage(Messagetype.codeus, centerManager.iplist), client.RemoteEndPoint.ToString());
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

                                centerManager.iplist[i].ID = Data.Devicedata[i].ID;
                                centerManager.iplist[i].IP = client.RemoteEndPoint.ToString();

                                Thread thread = new Thread(DeviceReceive);
                                thread.IsBackground = true;
                                thread.Start(client);
                                break;
                            }
                            i++;
                        }
                    }
                }

            }
            ipinfo();
            //int flagtest = 0;
            //foreach(IPList ip in centerManager.iplist)
            //{
            //    if(ip.ID!=null)
            //    {
            //        Console.WriteLine(centerManager.iplist[flagtest].ID);
            //        WriteLine(centerManager.iplist[flagtest].IP);
            //        WriteLine(Data.Devicedata[flagtest].ID);
            //        flagtest++;
            //    }

            //}
        }
        /// <summary>
        /// 用户信息接收
        /// </summary>
        /// <param name="o"></param>
        void UserReceive(object o)
        {
            Socket client = o as Socket;
            //User定位
            int UserID = 0;
            foreach (IPList ip in centerManager.UserList)
            {
                if (ip.IP == client.RemoteEndPoint.ToString()) break;
                UserID++;
            }
            //接受用户数据
            while (true)
            {
                try
                {
                    byte[] buffer = new byte[1024 * 1024];
                    int n = client.Receive(buffer);
                    Package package = BytesToPackage(buffer);

                    if (package.message == Messagetype.codeus)
                    {
                        string receive = Encoding.UTF8.GetString(package.data, 0, package.data.Length);
                        if (receive == "-1")
                        {
                            Send(CreatIPListToPackage(Messagetype.codeus, centerManager.iplist), client.RemoteEndPoint.ToString());
                            Console.WriteLine("updatelist");
                        }
                        else Data.Userdata[UserID].DeviceID = receive;
                    }
                    else
                        switch (package.message)
                        {
                            case Messagetype.package:
                                PackageToData packageToData = new PackageToData(NewUserData);
                                packageToData(package, UserID); break;
                            case Messagetype.order:
                                PackageToData carinfomessageToData = new PackageToData(NewCode);
                                carinfomessageToData(package, UserID); break;
                            case Messagetype.update:
                                PackageToData updatemessage = new PackageToData(UpdateMessage);
                                updatemessage(package, UserID); break;
                            
                        }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Data.Userdata[UserID].Live = false;
                    centerManager.UserList[UserID].ID = null;
                    break;
                }
            }
        }
        /// <summary>
        /// 设备信息接收
        /// </summary>
        /// <param name="o"></param>
        void DeviceReceive(object o)
        {
            Socket client = o as Socket;
            //Device定位
            int DeviceID = 0;
            foreach (IPList ip in centerManager.iplist)
            {
                if (ip.IP == client.RemoteEndPoint.ToString()) break;
                DeviceID++;
            }
            //接受设备数据
            while (true)
            {
                try
                {
                    byte[] buffer = new byte[1024 * 1024];
                    int n = client.Receive(buffer);
                    Package package = BytesToPackage(buffer);

                    switch (package.message)
                    {
                        case Messagetype.package:
                            PackageToData2 packageToData = new PackageToData2(NewDeviceData);
                            packageToData(package, DeviceID, client); break;
                        case Messagetype.carinfomessage:
                            PackageToData carinfomessageToData = new PackageToData(NewvolumeData);
                            carinfomessageToData(package, DeviceID); break;
                        case Messagetype.volumepackage:
                            PackageToData volumepackageToData = new PackageToData(NewvolumeData);
                            volumepackageToData(package, DeviceID); break;
                            //这个比较特殊，就算客户端断开也需要继续工作，所以不用flag;
                        case Messagetype.barvolumepackage:
                            PackageToData barVolToData = new PackageToData(NewBarVolData);
                            NewBarVolData(package, DeviceID); break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Data.Devicedata[DeviceID].Live = false;
                    centerManager.iplist[DeviceID].ID = null;
                    break;
                }
            }
        }

        public override bool Send(Package package, string ip)
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
                dic[ip].Send(bytes, bytes.Length, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            return true;
        }


        public void NewBarVolData(Package package, int DeviceID)
        {
            bvdata theSendData = new bvdata();
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(package.data, 0, package.data.Length);
                ms.Flush();
                ms.Position = 0;
                BinaryFormatter bf = new BinaryFormatter();
                theSendData = (bvdata)bf.Deserialize(ms);
                Console.WriteLine(theSendData.BarcodeInfmation);
                //Data.Devicedata[DeviceID].volume = data.volume;
                //Data.Devicedata[DeviceID].messagetype = package.message;
                //Data.Devicedata[DeviceID].flag = true;
                //Data.Devicedata[DeviceID].IP = o.RemoteEndPoint.ToString();
                //Data.Devicedata[DeviceID].Live = true;
            }
        }


        public Package CreatIPListToPackage(Messagetype messagetype, IPList[] ipl)
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

        void NewCode(Package package, int UserID)
        {
            Data.Userdata[UserID].codemode = (Codemode)Convert.ToInt32(Encoding.UTF8.GetString(package.data, 0, package.data.Length));
            Data.Userdata[UserID].messagetype = package.message;
            Data.Userdata[UserID].flag = true;
            Console.WriteLine(Data.Userdata[UserID].codemode);
        }
        void UpdateMessage(Package package, int UserID)
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
            Data.Userdata[UserID].messagetype = package.message;
            Data.Userdata[UserID].configtime = data.configtime;
            //Data.Userdata[UserID].volume = data.volume;
            Data.Userdata[UserID].flag = true;
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

        void NewDeviceData(Package package, int DeviceID, Socket o)
        {
            try
            {
                DeviceData data = new DeviceData();
                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(package.data, 0, package.data.Length);
                    ms.Flush();
                    ms.Position = 0;
                    BinaryFormatter bf = new BinaryFormatter();
                    data = (DeviceData)bf.Deserialize(ms);
                    //Data.Devicedata[DeviceID].volume = data.volume;
                    Data.Devicedata[DeviceID].messagetype = package.message;
                    Data.Devicedata[DeviceID].flag = true;
                    Data.Devicedata[DeviceID].IP = o.RemoteEndPoint.ToString();
                    Data.Devicedata[DeviceID].Live = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

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
        /// <summary>
        /// 更改体积等信息
        /// </summary>
        /// <param name="package"></param>
        void NewvolumeData(Package package, int DeviceID)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(package.data, 0, package.data.Length);
                    ms.Flush();
                    ms.Position = 0;
                    BinaryFormatter bf = new BinaryFormatter();

                    //Data.Devicedata[DeviceID].newvolumecontrol = (volumecontrol)bf.Deserialize(ms);

                    Data.Devicedata[DeviceID].messagetype = package.message;
                    Data.Devicedata[DeviceID].flag = true;
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
                //case Codemode.monitor: package.data = Encoding.UTF8.GetBytes("2"); break;
                //case Codemode.sendvolume: package.data = Encoding.UTF8.GetBytes("3"); break;
                //case Codemode.stopsendvolume: package.data = Encoding.UTF8.GetBytes("4"); break;
            }
            return package;
        }
    }

}
