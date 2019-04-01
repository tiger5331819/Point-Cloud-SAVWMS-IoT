using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace SAVWMS
{
    /// <summary>
    /// 所有传送的数据都打包成package的形式，然后往外发送
    /// </summary>
    [Serializable]
    public struct Package
    {
        public Messagetype message;
        public byte[] data;
    }
    /// <summary>
    /// 条码和体积收集的数据
    /// </summary>
    [Serializable]
    public struct bvdata
    {
        public string BarcodeInfmation;
        public DateTime BarcodeAcquisitionTime;
        public decimal? PackageVolume;
        public DateTime VolumeAcquisitionTime;
        public decimal? PackageWeight;
        public DateTime WeightAcquisitionTime;
    }
    /// <summary>
    /// 收集到的bvdata每十条上传一次：从文件中收集那些数据，所以需要文件操作的一个方法
    /// 发送数据最好用线程，每当有十个数据时，在上传数据的同时不影响从文件中正常接收数据
    /// </summary>
    [Serializable]
    public class barvolumedata
    {
        private int bvdatanum=0;
        public bvdata[] Bvdata=new bvdata[20];
        public barvolumedata()
        {
            bvdatanum = 0;
        }
        public bool PutBvdata(bvdata d)
        {
            if (bvdatanum < 20)
            {
                Bvdata[bvdatanum] = d;
                bvdatanum++;
                return true;
            }
            else return false;
        }
        public int Getnum() { return bvdatanum; }
        public void Setnum(int num) { bvdatanum = num; }
        public void show()
        {
            foreach(bvdata b in Bvdata)
            {
                Console.WriteLine(b.BarcodeInfmation + "  " + b.BarcodeAcquisitionTime.ToString());
                Console.WriteLine(b.PackageVolume.ToString() + "  " + b.VolumeAcquisitionTime.ToString());
            }
        }
    }
    /// <summary>
    /// tcp传输协议用到的ip和端口号
    /// </summary>
    [Serializable]
    public struct NetIP
    {
        public string IP { get; set; }
        public int Point { get; set; }
    }

    [Serializable]
    public class TypeData
    {
        public Datatype datatype;
        public string ID;
        public Socket socket;
        public TypeData()
        {
            datatype = Datatype.CenterSever;
            socket = null;
        }
    }
    public class CenterNet
    {
        public string ID;//设备编号
        public TypeNet typenet;

        public Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public static Package BytesToPackage(byte[] buffer)
        {
            
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(buffer, 0, buffer.Length);
                ms.Flush();
                ms.Position = 0;
                BinaryFormatter bf = new BinaryFormatter();
                Package package = (Package)bf.Deserialize(ms);
                return package;
            }
        }
        public static Package CreateOrderString(string order)
        {
            Package package = new Package();
            package.data = Encoding.UTF8.GetBytes(order);
            package.message = Messagetype.order;
            return package;
        }

        public Package DeviceDataToPackage(TypeData data, Messagetype messagetype = Messagetype.package)
        {
            Package package = new Package();
            package.data = null;
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    switch (messagetype)
                    {
                        //case Messagetype.carinfomessage: bf.Serialize(ms, data.volume); break;
                        //case Messagetype.volumepackage: bf.Serialize(ms, data.volume); break;
                        case Messagetype.package: bf.Serialize(ms, data); break;
                        default: bf.Serialize(ms, data); break;
                    }
                    ms.Flush();

                    package.message = messagetype;
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
        public Package UserDataToPackage(TypeData data, Messagetype messagetype = Messagetype.package)
        {
            Package package = new Package();
            package.data = null;
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    switch (messagetype)
                    {
                       
                        case Messagetype.package: bf.Serialize(ms, data); break;
                        default: bf.Serialize(ms, data); break;
                    }
                    ms.Flush();

                    package.message = messagetype;
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

        public delegate DeviceData PackageToDeviceData(Package package);
        public delegate UserData PackageToUserData(Package package);
    }
}
