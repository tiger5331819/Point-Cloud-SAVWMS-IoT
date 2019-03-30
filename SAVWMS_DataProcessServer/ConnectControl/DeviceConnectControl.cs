using SAVWMS.ConnectControl;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SAVWMS
{
    class DeviceConnectControl
    {
        CenterManager centerManager;
        DeviceData data;
        ClientConnectControl user;
        MailBox mailBox;
        int DeviceID;
        public Queue<Codemode> order = new Queue<Codemode>();

        public DeviceConnectControl(ref DeviceData d, ref CenterManager c,int i)
        {
            data = d;
            centerManager = c;
            user = null;
            DeviceID = i;
            mailBox = new DeviceMailBox(ref d);

            Thread check = new Thread(CreateThreadToCheckData);
            check.IsBackground = true;
            check.Start();
        }

        void CreateThreadToCheckData()
        {
            int sum = 0;
            async void Receive()
            {
                while (data.Live)
                    if (await mailBox.DOReceive())
                    {
                        switch (data.messagetype)
                        {
                            case Messagetype.carinfomessage: ChangeCarMessage(); break;
                            case Messagetype.volumepackage: ChangeCarMessage(); break;
                            case Messagetype.package: ChangeCarMessage(); break;
                        }
                    }
                    else { Thread.Sleep(100); sum++; }
            }
            Receive();
            while (data.Live)
            {
                if (!data.Live) centerManager.Data.iplist[DeviceID].ID = null;

                if (sum == 100) { SendCode(Codemode.monitor); sum = 0; }

                Codemode code;
                if (order.TryDequeue(out code))
                {
                    SendCode(code);
                }
            }
        }

        public string ID()
        {
            return data.ID;
        }

    }
}
