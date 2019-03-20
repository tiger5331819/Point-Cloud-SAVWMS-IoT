using System;
using System.Threading;

namespace SAVWMS
{
    class ClientConnectControl
    {
        ConnectionControlCenter cc;
        public UserData data;
        CenterManager centerManager;
        DeviceConnectControl deviceC;
        ClientConnectControl userC;

        public ClientConnectControl(ref UserData d, ref CenterManager cm, ref ConnectionControlCenter ccc)
        {
            data = d;
            centerManager = cm;
            cc = ccc;
            deviceC = null;
            userC = this;

            Thread check = new Thread(CreateThreadToCheckData);
            check.IsBackground = true;
            check.Start();
        }

        void CreateThreadToCheckData()
        {
            while (data.Live)
            {
                if (data.newdatachange())
                {
                    switch (data.messagetype)
                    {
                        case Messagetype.order: orderTODO(); break;
                        //case Messagetype.update: updateTODO(); break;
                    }
                    data.flag = false;
                }
                else Thread.Sleep(100);

            }
        }

        void orderTODO()
        {
            switch (data.codemode)
            {
                case Codemode.monitor: monitor(data.codemode); break;
                case Codemode.release: release(); break;
                default: codemode(data.codemode); break;
            }
        }

        void monitor(Codemode codemode)
        {
            for (int i = 0; i < 200; i++)
            {
                IPList ip = centerManager.iplist[i];
                if (ip.IP == data.DeviceID)
                {
                    deviceC = cc.DeviceC[i];
                    deviceC.adduser(ref userC, ip.ID);
                    deviceC.order.Enqueue(codemode);
                }
            }

        }
        void release()
        {
            if (deviceC.removeuser()) { data.DeviceID = null; deviceC = null; }
            else Console.WriteLine("user release error");
        }
        void codemode(Codemode codemode)
        {
            if (deviceC == null) { return; }
            deviceC.order.Enqueue(codemode);
        }

        //void updateTODO()
        //{
        //    deviceC.updatemessage(data.volume, data.configtime);
        //}




        //public void UpdateVolume(volumecontrol v)
        //{
        //    data.volume = v;

        //    if (SendMessage(Messagetype.volumepackage)) Console.WriteLine("发送成功给用户！");
        //    else Console.WriteLine("error！");
        //}

        public bool SendMessage(Messagetype messagetype)
        {
            if (centerManager.centerNetManager.Send(centerManager.centerNetManager.UserDataToPackage(data, messagetype), data.IP)) return true;
            else return false;
        }
    }
}
