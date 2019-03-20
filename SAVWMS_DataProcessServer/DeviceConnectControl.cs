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
        public Queue<Codemode> order = new Queue<Codemode>();

        public DeviceConnectControl(ref DeviceData d, ref CenterManager c)
        {
            data = d;
            centerManager = c;
            user = null;

            Thread check = new Thread(CreateThreadToCheckData);
            check.IsBackground = true;
            check.Start();
        }

        void CreateThreadToCheckData()
        {
            //int sum = 0;
            while (data.Live)
            {
                if (data.newdatachange())
                {
                    switch (data.messagetype)
                    {
                        case Messagetype.carinfomessage: ChangeCarMessage(); break;
                        case Messagetype.volumepackage: ChangeCarMessage(); break;
                        case Messagetype.package: ChangeCarMessage(); break;
                        
                    }
                    data.flag = false;
                }
                //else { Thread.Sleep(100); sum++; }
                //if (sum == 100) { SendCode(Codemode.monitor); sum = 0; }


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

        

        void ChangeCarMessage()
        {
            //data.volume = data.newvolumecontrol;
            data.flag = false;

            Console.WriteLine(data.ID);
            //Console.WriteLine(data.volume.carName);
            //Console.WriteLine(data.volume.carVolume);
            //Console.WriteLine(data.volume.volume);
            if (user != null)
            {
                //user.UpdateVolume(data.volume);
            }
        }
        public bool adduser(ref ClientConnectControl d, string devicename)
        {
            foreach (IPList ip in centerManager.iplist)
            {
                if (ip.ID == devicename)
                {
                    user = d;
                    Console.WriteLine(user.data.ID);
                }
            }
            return false;
        }
        public bool removeuser()
        {
            try
            {
                user = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
            return true;
        }
        //public void updatemessage(volumecontrol newvolume, configtimexml[] newconfig)
        //{
        //    data.volume.carName = newvolume.carName; data.configtime = newconfig;
        //    data.volume.carVolume = newvolume.carVolume;
        //    SendUpdate();
        //}
        public bool SendMessage(Messagetype messagetype)
        {
            if (centerManager.centerNetManager.Send(centerManager.centerNetManager.DeviceDataToPackage(data, messagetype), data.IP)) return true;
            else return false;
        }
        public bool SendCode(Codemode code)
        {
            if (centerManager.centerNetManager.Send(centerManager.centerNetManager.CreatCodeToPackage(code), data.IP)) return true;
            else return false;
        }
        bool SendUpdate(Messagetype messagetype = Messagetype.update)
        {
            Codemode code;
            if (order.TryDequeue(out code))
            {
                Console.WriteLine("Orderqueue is not null.");
                return false;
            }
            if (centerManager.centerNetManager.Send(centerManager.centerNetManager.DeviceDataToPackage(data, messagetype), data.IP)) return true;
            else return false;
        }
    }
}
