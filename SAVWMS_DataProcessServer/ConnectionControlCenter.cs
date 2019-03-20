using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using static System.Console;
namespace SAVWMS
{
    class ConnectionControlCenter
    {
        CenterManager centerManager;
        public DeviceConnectControl[] DeviceC = new DeviceConnectControl[200];
        ClientConnectControl[] UserC = new ClientConnectControl[200];

        ConnectionControlCenter cc;
        string article;

        public ConnectionControlCenter(ref CenterManager c)
        {
            centerManager = c;
            for (int i = 0; i < 200; i++)
            {
                DeviceC[i] = null;
                UserC[i] = null;
            }
            cc = this;
            Thread check = new Thread(CreateThreadToCheckData);
            check.IsBackground = true;
            check.Start();

            shell();

        }
        void CreateThreadToCheckData()
        {
            bool[] DeviceList = new bool[200];
            bool[] UserList = new bool[200];

            for (int i = 0; i < 200; i++)
            {
                DeviceList[i] = false;
                UserList[i] = false;
            }

            while (true)
            {
                for (int i = 0; i < 200; i++)
                {
                    IPList ip = centerManager.iplist[i];
                    if (ip.ID != null)
                    {
                        if (!DeviceList[i])
                        {
                            DeviceList[i] = true;

                            DeviceC[i] = new DeviceConnectControl(ref centerManager.Data.Devicedata[i], ref centerManager);
                        }
                    }
                    else if (DeviceList[i]) { DeviceList[i] = false; Console.WriteLine(centerManager.Data.Devicedata[i].ID); }

                    ip = centerManager.UserList[i];
                    if (ip.ID != null)
                    {
                        if (!UserList[i])
                        {
                            UserList[i] = true;
                            UserC[i] = new ClientConnectControl(ref centerManager.Data.Userdata[i], ref centerManager, ref cc);
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
                catch (Exception) { }

            }
        }

        void Select()
        {

            article = null;
            article = ReadLine();

            int flag = Convert.ToInt32(article);
            DeviceConnectControl d = DeviceC[flag];
            WriteLine(d.ID());

            while (true)
            {
                article = null;
                article = ReadLine();
                switch (article)
                {
                    case "back": return;
                    case "play": TODO(d, Codemode.play); break;
                    case "monitor": TODO(d, Codemode.monitor); break;
                    case "sendvolume": TODO(d, Codemode.sendvolume); break;
                    case "stopsendvolume": TODO(d, Codemode.stopsendvolume); break;
                    case "stop": TODO(d, Codemode.stop); break;
                }
            }

        }
        void TODO(DeviceConnectControl Device, Codemode codemode)
        {
            Device.SendCode(codemode);
        }
    }
}
