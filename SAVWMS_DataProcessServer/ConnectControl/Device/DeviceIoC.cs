using System;
using System.Collections.Generic;
using System.Text;

namespace SAVWMS.ConnectControl
{
    public class DeviceIoC
    {       
        public static DeviceTask CreateDeviceTask(string TaskCategory,string Taskname,DeviceConnectControl dc,int id)
        {
            DeviceTask task;
            if (dc == null) return null;
            switch(TaskCategory)
            {
                case "BVTask":task=new BVTask(id,Taskname,dc); return task;
                default:return null;
            }
        }
    }
}
