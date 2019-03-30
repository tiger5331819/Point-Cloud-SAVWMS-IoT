using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace SAVWMS
{
    [Serializable]
    public struct IPList
    {
        public string ID;
        public string IP;
    }

    public class CenterManager
    {
        public CenterManager centerManager;
        public CenterServerNet centerNetManager;
        public CenterSeverData Data;

        int MaxIP = 20;
        public int Max => MaxIP;
        public IPList[] iplist;
        public IPList[] UserList;

        public string SAVWMSversion { get; set; }//界面版本号
        public string Volumeversion { get; set; }//体积计算版本号
        public string BarcodeScanname { get; set; }//条码扫描使用官方程序，知道名就可以


        public CenterManager()
        {
            Data = new CenterSeverData(MaxIP);
            centerManager = this;
            loadxml();           
            iplist= new IPList[MaxIP];
            UserList = new IPList[MaxIP];
            for(int i = 0; i < MaxIP; i++)
            {
                iplist[i].ID = null;
                iplist[i].IP = null;
            }
            centerNetManager = new CenterServerNet(ref Data, ref centerManager);         
        }
        public void loadxml()
        {
            //将XML文件加载进来
            XDocument document = XDocument.Load("config.xml");
            //获取到XML的根元素进行操作
            XElement root = document.Root;
            XElement Device = root.Element("Server");
            XElement ID = Device.Element("ID");
            Data.ID = ID.Value;
            
            XElement version = Device.Element("SANWMSversion");
            SAVWMSversion = version.Value;

            XElement NetLink = root.Element("NetLink");
            XElement IP = NetLink.Element("IP");

            XElement server = IP.Element("server");
            Data.ip.IP = server.Value;
            XElement Point = IP.Element("serverpoint");
            Data.ip.Point = int.Parse(Point.Value);
     
        }
        public void writexml()
        {
            //获取根节点对象
            XDocument document = new XDocument();
            XElement root = new XElement("SAVWMS");
            XElement Device = new XElement("Device");
            XElement ID = new XElement("ID");
            ID.Value = Data.ID;
            XElement EVCSv = new XElement("EVCSversion");
            EVCSv.Value = SAVWMSversion;
            Device.Add(EVCSv);
            XElement version = new XElement("Volumeversion");
            version.Value = Volumeversion;
            Device.Add(version);
            root.Add(Device);
            XElement NetLink = new XElement("NetLink");
            XElement IP = new XElement("IP");
            IP.SetElementValue("server", Data.ip.IP);
            IP.SetElementValue("serverpoint", Data.ip.Point);
            NetLink.Add(IP);
            root.Add(NetLink);
            root.Save("config.xml");
        }
    }
}
