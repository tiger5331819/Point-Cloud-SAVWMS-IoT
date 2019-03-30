using SAVWMS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SAVWMS.ConnectControl
{
    public interface MailBox
    {
        Task<bool> DOReceive();
        bool Send(Package package);
    }

    public class DeviceMailBox:MailBox
    {
        Socket socket=null;
        DeviceData Data = null;

        delegate void PackageToData(Package package);
        public DeviceMailBox(ref DeviceData d)
        {
            socket = d.socket;
            Data = d;
        }

        public bool Send(Package package)
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
                socket.Send(bytes, bytes.Length, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            return true;
        }

        public Task<bool>DOReceive()
        {
            return Task.Run<bool>(() => { return Receive(); });
        }


        /// <summary>
        /// 设备信息接收
        /// </summary>
        /// <param name="o"></param>
        bool Receive()
        {
            PackageToData packageToData = new PackageToData(Newbarvolumedata);
            //接受设备数据
                try
                {
                    byte[] buffer = new byte[1024 * 1024];
                    int n = socket.Receive(buffer);
                    Package package = CenterServerNet.BytesToPackage(buffer);

                    switch (package.message)
                    {
                        case Messagetype.package: NewDeviceData(package);return true;
                        //case Messagetype.carinfomessage: packageToData(package); return true;
                        case Messagetype.volumepackage: packageToData(package); return true;
                        case Messagetype.barvolumepackage: NewBarVolData(package); return true;
                        default:return false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Data.Live = false;
                    return false;
                }
        }

        void NewDeviceData(Package package)
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
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }

        void NewBarVolData(Package package)
        {
            bvdata theSendData = new bvdata();
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(package.data, 0, package.data.Length);
                ms.Flush();
                ms.Position = 0;
                BinaryFormatter bf = new BinaryFormatter();
                theSendData = (bvdata)bf.Deserialize(ms);
            }
        }
        /// <summary>
        /// 更改体积等信息
        /// </summary>
        /// <param name="package"></param>
        void Newbarvolumedata(Package package)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(package.data, 0, package.data.Length);
                    ms.Flush();
                    ms.Position = 0;
                    BinaryFormatter bf = new BinaryFormatter();

                    Data.barvolumedata = (barvolumedata)bf.Deserialize(ms);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }
    }

    public class UserMailBox : MailBox
    {
        Socket socket = null;
        UserData Data = null;
        public IPList[] iplist;

        public UserMailBox(ref UserData d, IPList[] ip)
        {
            iplist = ip;
            socket = d.socket;
            Data = d;
        }
        public delegate void PackageToData(Package package,Messagetype messagetype);

        public bool Send(Package package)
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
                socket.Send(bytes, bytes.Length, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            return true;
        }

        public Task<bool> DOReceive()
        {
            return Task.Run<bool>(() => { return Receive(); });
        }
        /// <summary>
        /// 用户信息接收
        /// </summary>
        /// <param name="o"></param>
        public bool Receive()
        {
            PackageToData packageToData = new PackageToData(NewUserData);
            //接受用户数据
            try
            {
                byte[] buffer = new byte[1024 * 1024];
                int n = socket.Receive(buffer);
                Package package = CenterServerNet.BytesToPackage(buffer);

                if (package.message == Messagetype.codeus)
                {
                    string receive = Encoding.UTF8.GetString(package.data, 0, package.data.Length);
                    if (receive == "-1")
                    {
                        Send(CenterServerNet.CreatIPListToPackage(Messagetype.codeus, iplist));
                        Console.WriteLine("updatelist");
                        return false;
                    }
                    else { Data.DeviceID = receive; return false; }
                }
                else
                    switch (package.message)
                    {
                        case Messagetype.package:packageToData(package,package.message); return true;
                        case Messagetype.order:NewCode(package); return true;
                        case Messagetype.update:packageToData(package, package.message); return true;
                        default:return false;
                    }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Data.Live = false;
                return false;
            }
        }

        void NewUserData(Package package,Messagetype messagetype)
        {
            try
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
                switch(messagetype)
                {
                    case Messagetype.package:Data = data;break;
                    case Messagetype.update:
                        Data.messagetype = package.message;
                        Data.configtime = data.configtime;
                        Data.volume = data.volume;break;
                    default:Console.WriteLine("Func:NewUserData.messagetype is null"); break;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Func(NewUserData) error:" + ex.ToString());
            }

        }

        void NewCode(Package package)
        {
            Data.codemode = (Codemode)Convert.ToInt32(Encoding.UTF8.GetString(package.data, 0, package.data.Length));
            Data.messagetype = package.message;
            Console.WriteLine(Data.codemode);
        }
    }
}
