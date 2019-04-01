using SAVWMS.ConnectControl;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SAVWMS
{
    class ClientConnectControl
    {
        ControlCenter cc;
        public UserData data;
        CenterManager centerManager;
        MailBox mailBox;
        int UserID;
        DeviceTask task;

        public ClientConnectControl(ref UserData d, ref CenterManager cm, ref ControlCenter ccc,int i)
        {
            data = d;
            centerManager = cm;
            cc = ccc;
            UserID = i;
            mailBox=new UserMailBox(ref d,cm.iplist);

            Thread check = new Thread(CreateThreadToCheckData);
            check.IsBackground = true;
            check.Start();
        }

        void CreateThreadToCheckData()
        {
            async void Receive()
            {
                while (data.Live)
                    if (await mailBox.DOReceive())
                    {
                        switch (data.messagetype)
                        {
                            case Messagetype.order: orderTODO(); break;
                            //case Messagetype.update: updateTODO(); break;
                        }
                    }

            }
            Receive();
            while (data.Live)
            {
                if (!data.Live) centerManager.iplist[UserID].ID = null;
                Thread.Sleep(400);
            }
        }

        void orderTODO()
        {
            switch (data.codemode)
            {
                case Codemode.monitor: monitor(data.codemode); break;
                default: codemode(data.codemode); break;
            }
        }

        void monitor(Codemode codemode)
        {
            for (int i = 0; i < centerManager.Max; i++)
            {
                IPList ip = centerManager.iplist[i];
                if (ip.IP == data.DeviceID)
                {
                    task=cc.taskManager.GetDeviceTask(i);
                    if (task == null)
                    {
                        string TaskCategory = "BVTask";
                        string Taskname = "test";
                        task = cc.taskManager.SetDeviceTask(TaskCategory,Taskname,i);
                    }
                }
            }

        }
        void codemode(Codemode codemode)
        {
            object o = new object();
            switch(codemode)
            {
                case Codemode.play:task.TaskRemote(1);break;
                case Codemode.stop:task.TaskRemote(0);break;
                case Codemode.sendvolume:task.GetResults(out o); show(o); break;
            }
        }
        void show(object o)
        {
            List<barvolumedata> barvolumedatas = o as List<barvolumedata>;
            foreach(barvolumedata a in barvolumedatas)
            {
                a.show();
            }
        }
    }
}
