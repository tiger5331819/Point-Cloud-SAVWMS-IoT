using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;

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

        public void getbvdata()
        {
            BarcodeInfmation = "";
            BarcodeAcquisitionTime = DateTime.Now;
            PackageVolume = 0;
            VolumeAcquisitionTime = DateTime.Now;
            PackageWeight = 0;
            WeightAcquisitionTime = DateTime.Now;
        }
    }
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class barvolumedata
    {
        public barvolumedata()
        {
            barnum = 0;
            volnum = 0;
            barpointer = 0;
            volpointer = 0;
            Bvdata = new bvdata[20];
            for (int i = 0; i < 19; i++) 
            {
                Bvdata[i].getbvdata();
            }
            timeDifferenceThreshold = 5000;
        }
        public long barnum;
        public long volnum;
        public int barpointer;
        public int volpointer;
        public bvdata[] Bvdata;

        public int timeDifferenceThreshold;

        /// <summary>
        /// 添加体积数据
        /// </summary>
        /// <param name="volumedata"></param>
        public void addVolumeData(string volumedata)
        {
            Bvdata[volpointer].VolumeAcquisitionTime = DateTime.Now;
            Bvdata[volpointer].PackageVolume = Convert.ToDecimal(volumedata);
            volnum++;
            Console.WriteLine("bar当前体积数量：" + volnum);
            Console.WriteLine("bar当前条码数量：" + barnum);
            volpointer = volpointer == 19 ? 0 : barpointer + 1;
        }
        /// <summary>
        /// 添加条码数据
        /// </summary>
        /// <param name="bardata"></param>
        public void addBarData(string bardata)
        {
            if (barnum > 1000000)
            {
                barnum -= 1000000;
                volnum -= 1000000;
            }

            A: if (barnum < volnum)
            {
                TimeSpan timeSpan = DateTime.Now - Bvdata[barpointer].VolumeAcquisitionTime;
                int timespan = timeSpan.Seconds;
                while (timespan > timeDifferenceThreshold)
                {
                    Bvdata[barpointer].BarcodeInfmation = "error bar info";
                    barnum++;
                    barpointer = barpointer == 19 ? 0 : barpointer + 1;
                    if (barnum < volnum)
                    {
                        timeSpan = DateTime.Now - Bvdata[barpointer].VolumeAcquisitionTime;
                    }
                    else
                    {
                        goto A;
                    }
                }
                Bvdata[barpointer].BarcodeInfmation = bardata;
                //datatime类型包含毫秒
                Bvdata[volpointer].BarcodeAcquisitionTime = DateTime.Now;
                
                barnum++;
                Console.WriteLine("vol当前条码数量：" + barnum);
                Console.WriteLine("vol当前体积数量：" + volnum);
                barpointer = barpointer == 19 ? 0 : barpointer + 1;

            }
            else
            {
                addVolumeData("-1");
                Bvdata[barpointer].BarcodeInfmation = bardata;
                Bvdata[barpointer].BarcodeAcquisitionTime = DateTime.Now;
                barnum++;
                Console.WriteLine("vol当前条码数量：" + barnum);
                Console.WriteLine("vol当前体积数量：" + volnum);
                barpointer = barpointer == 19 ? 0 : barpointer + 1;
            }
        }
    }



    /// <summary>
    /// 更改自动控制的时间数据
    /// </summary>
    [Serializable]
    public struct configtimexml
    {
        public string time;
        public string beginhour;
        public string beginminute;
        public string endminute;
        public string endhour;
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
        public configtimexml[] configtime;
        public bool flag;//这个是判断是否更新了信号
        public Messagetype messagetype;
        public Codemode codemode;
        public TypeData()
        {
            datatype = new Datatype();
            configtime = new configtimexml[3];
        }
    }

    [Serializable]
    public class NetData
    {
        public string ID;//设备编号
        public TypeNet typenet;
        public Socket socket;
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
        /// <summary>
        /// 包转数据委托
        /// </summary>
        /// <param name="package"></param>
        public delegate void PackageToData(Package package);

        public Package BytesToPackage(byte[] buffer)
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
        /// <summary>
        /// 这个方法需要重写，根据需要制定新的case
        /// </summary>
        /// <param name="data"></param>
        /// <param name="messagetype"></param>
        /// <returns></returns>
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
                        case Messagetype.ID: bf.Serialize(ms, data); break;
                        case Messagetype.barvolumepackage:bf.Serialize(ms, data);break;
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

        /// <summary>
        /// 整合数据的对象在devidce中传递的device对象
        /// </summary>
        /// <param name="data"></param>
        /// <param name="messagetype"></param>
        /// <returns></returns>
        public Package DeviceDataToPackageReal(DeviceData data, Messagetype messagetype = Messagetype.package)
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
                        case Messagetype.barvolumepackage: bf.Serialize(ms, data.barvolumedata.Bvdata[data.barvolumedata.volpointer-1]); break;
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
    }
}
