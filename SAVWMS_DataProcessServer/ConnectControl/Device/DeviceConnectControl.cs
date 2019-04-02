using SAVWMS.ConnectControl;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SAVWMS
{
    public class DeviceConnectControl
    {
        CenterManager centerManager;
        public List<barvolumedata> barvolumedatas;
        public DeviceData data;
        MailBox mailBox;
        Queue<string> order = new Queue<string>(10);

        int DeviceID;

        string Order;

        public DeviceConnectControl()
        {
            Console.WriteLine("The DeviceConnectControl is null");
        }
        public DeviceConnectControl(ref DeviceData d, ref CenterManager c,int i)
        {
            data = d;
            centerManager = c;
            DeviceID = i;
            mailBox = new DeviceMailBox(ref d);
            barvolumedatas = new List<barvolumedata>(100);

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
                        //switch (data.messagetype)
                        //{
                        //    case Messagetype.carinfomessage: ChangeCarMessage(); break;
                        //    case Messagetype.volumepackage: ChangeCarMessage(); break;
                        //    case Messagetype.package: ChangeCarMessage(); break;
                        //}
                    }
                    else { Thread.Sleep(100); sum++; }
            }
            Receive();
            while (data.Live)
            {
                if (!data.Live) centerManager.iplist[DeviceID].ID = null;

                if (sum == 100) { mailBox.Send(CenterNet.CreateOrderString("monitor")); sum = 0; }
                if(order.TryDequeue(out Order))Send(Order);
            }
        }
        public void Send(string order)
        {
            mailBox.Send(CenterNet.CreateOrderString(order));
        }
        public string ID()
        {
            return data.ID;
        }
    }
}
