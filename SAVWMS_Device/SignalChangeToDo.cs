using System;
using System.Threading;

namespace SAVWMS
{
    public class SignalChangeToDo
    {
        
        DeviceData Data;
        public Manager manager;
        public SignalChangeDo ChangeDo;

        /// <summary>
        /// 新建信号检查线程
        /// </summary>
        /// <param name="data"></param>
        /// <param name="m"></param>
        public SignalChangeToDo(ref DeviceData data,ref Manager m)
        {
            Data = data;
            manager = m;
            ChangeDo = new SignalChangeDo(ref m);
            Thread check = new Thread(CreateThreadToCheckData);
            check.IsBackground = true;
            check.Start();
        }
        /// <summary>
        /// 信号检查线程函数
        /// </summary>
        void CreateThreadToCheckData()
        {
            try
            {
                while (true)
                {
                    if (Data.newdatachange())
                    {

                        switch (Data.messagetype)
                        {
                            //case Messagetype.carinfomessage: ChangeCarinfoMessage(); break;
                            case Messagetype.order: OrderTODO(); break;
                            case Messagetype.update: updateTODO(); break;
                        }
                        Data.flag = false;
                    }
                    else Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void OrderTODO()
        {
            switch (Data.codemode)
            {
                
                case "play": test();break;//ChangeDo.Play(); break;
                case "stop": ChangeDo.Stop(); break;
            }
        }

        private void updateTODO()
        {
            manager.netManager.Send(manager.netManager.DeviceDataToPackage(manager.Data, Messagetype.package));
            Console.WriteLine("succeed");
        }
        void test()
        {
            string vol;
            string bar;
            int volume = 0;
            int barn = 0;
            int flag = 0;
            while(true)
            {
                if(flag>=20)
                {
                    Console.ReadLine();
                    flag = 0;
                }
                vol = volume.ToString();
                Console.WriteLine("vol success");
                bar = barn.ToString();
                Console.WriteLine("bar success");
                manager.Data.barvolumedata.addVolumeData(vol);
                manager.Data.barvolumedata.addBarData(bar);
                Console.WriteLine("bar and vol success");
                manager.netManager.Send(manager.netManager.DeviceDataToPackageReal(manager.Data, Messagetype.barvolumepackage));
                volume++;
                barn++;
                flag++;
                Thread.Sleep(50);
            }
        }
    }
}
