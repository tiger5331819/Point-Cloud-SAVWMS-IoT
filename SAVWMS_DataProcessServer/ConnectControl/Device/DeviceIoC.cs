using System;
using System.Collections.Generic;
using System.Text;

namespace SAVWMS.ConnectControl
{
    public class DeviceIoC
    {       
        public static DeviceTask CreateDeviceTask(string TaskCategory,string Taskname,ref DeviceConnectControl dc,int id)
        {
            DeviceTask task;
            if (dc == null) { Console.WriteLine("DeviceConnectControl is null :error DeviceIoC.CreateDeviceTask"); return null; }
            switch(TaskCategory)
            {
                case "BVTask":task=new BVTask(id,Taskname,ref dc); return task;
                default:Console.WriteLine("error"); return null;
            }
        }
    }
}
