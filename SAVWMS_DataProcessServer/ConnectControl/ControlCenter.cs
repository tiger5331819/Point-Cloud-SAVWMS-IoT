using SAVWMS.ConnectControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using static System.Console;
namespace SAVWMS
{
    public struct DeviceList
    {
        public bool Live;
        public int ID;
        public string Name;
    }
    class ControlCenter
    {
        CenterManager centerManager;
        public DeviceConnectControl[] DeviceC;
        public DeviceList[] deviceList;
        ClientConnectControl[] UserC;
        public TaskManager taskManager;

        ControlCenter cc;
        string article;
        int Max;
        public int GetMax() { return Max; }
        public IPList[] GetIPList() { return centerManager.iplist; } 

        public ControlCenter(ref CenterManager c,int M)
        {
            centerManager = c;
            Max = M;
            DeviceC=new DeviceConnectControl[Max];
            UserC= new ClientConnectControl[Max];
            for (int i = 0; i < Max; i++)
            {
                DeviceC[i] = null;
                UserC[i] = null;
            }
            cc = this;
            Thread check = new Thread(CreateThreadToCheckData);
            check.IsBackground = true;
            check.Start();
            taskManager = new TaskManager(ref cc);
            shell();
        }
        void CreateThreadToCheckData()
        {
            deviceList= new DeviceList[Max];
            bool[] UserList = new bool[Max];

            for (int i = 0; i < Max; i++)
            {
                deviceList[i].Live = false;
                UserList[i] = false;
            }

            while (true)
            {
                for (int i = 0; i < Max; i++)
                {
                    IPList ip = centerManager.iplist[i];
                    if (ip.ID != null)
                    {
                        if (!deviceList[i].Live)
                        {
                            deviceList[i].Live = true;
                            deviceList[i].ID = i;
                            deviceList[i].Name = centerManager.Data.Devicedata[i].IP;
                            DeviceC[i] = new DeviceConnectControl(ref centerManager.Data.Devicedata[i], ref centerManager,Max);                        
                        }
                    }
                    else if (deviceList[i].Live) { deviceList[i].Live = false; Console.WriteLine(centerManager.Data.Devicedata[i].ID); }

                    ip = centerManager.UserList[i];
                    if (ip.ID != null)
                    {
                        if (!UserList[i])
                        {
                            UserList[i] = true;
                            UserC[i] = new ClientConnectControl(ref centerManager.Data.Userdata[i], ref centerManager, ref cc,Max);
                        }
                    }
                    else if (UserList[i]) UserList[i] = false;
                }
                Thread.Sleep(100);
            }

        }

        public void shell()
        {
            var Devicelist = from r in centerManager.iplist where r.ID != null orderby r.ID descending select r;
            var Userlist = from r in centerManager.UserList where r.ID != null orderby r.ID descending select r;
            
            while (true)
            {
                try
                {
                    
                    article = Console.ReadLine();
                    switch (article)
                    {
                        case "DeviceList": foreach (IPList r in Devicelist) WriteLine(r.ID + " " + r.IP); break;
                        case "UserList": foreach (IPList r in Userlist) WriteLine(r.ID + " " + r.IP); break;
                        case "Select": Select(); break;
                        case "Data": Console.WriteLine(centerManager.Data.Devicedata[0].ID); break;
                        default: break;
                    }
                    article = null;
                }
                catch (Exception e)
                {
                    WriteLine(e.ToString());
                }

            }
        }

        void Select()
        {

            article = null;
            article = ReadLine();

            int flag = Convert.ToInt32(article);
            ClientConnectControl d = UserC[flag];
            WriteLine(d.ID());

            while (true)
            {
                article = null;
                article = ReadLine();
                switch (article)
                {
                    case "back": return;
                    case "play": d.codemode(Codemode.play); break;
                    case "show": d.codemode(Codemode.sendvolume); break;
                    case "stop": d.codemode(Codemode.stop); break;
                }
            }
        }

    }
}
