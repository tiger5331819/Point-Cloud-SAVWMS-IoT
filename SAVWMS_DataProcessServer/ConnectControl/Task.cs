using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SAVWMS.ConnectControl
{
    class TaskManager
    {
        DeviceTask[] DeviceTaskLake;
        int TasknumMax = 10;
        int Tasknum;
        ControlCenter CC;

        public TaskManager(ref ControlCenter cc)
        {
            DeviceTaskLake= new DeviceTask[TasknumMax];
            Tasknum = 0;
            CC = cc;
        }
        public DeviceTask GetDeviceTask(int ID)
        {
            if (DeviceTaskLake[ID] == null) return null;
            return DeviceTaskLake[ID];
        }
        public DeviceTask SetDeviceTask(string TaskCategory, string Taskname,int did)
        {
            if(Tasknum<TasknumMax)
            {
                DeviceTaskLake[Tasknum] = DeviceIoC.CreateDeviceTask(TaskCategory, Taskname,GetDeviceC(did), Tasknum);
                if (DeviceTaskLake[Tasknum] == null) return null;
                Tasknum++;
                return DeviceTaskLake[Tasknum];
            }
            else
            {
                Console.WriteLine("DeviceTaskLake is full!");
                return null;
            }
        }
        DeviceConnectControl GetDeviceC(int did)
        {
            foreach(DeviceList id in CC.deviceList)
            {
                if (id.ID == did)
                    return CC.DeviceC[did];                  
            }
            Console.WriteLine("Device is null!");
            return null;
        }
    }
    public interface DeviceTask
    {
        void TaskRemote(int flag);
        void GetTaskConfig(out int TaskID, out string TaskName);
        void GetResults(out object Results);
    }
    class BVTask:DeviceTask
    {
        int TaskID;
        string TaskName;
        DeviceConnectControl DeviceC;
        int Remoteflag=-1;
        bool Taskflag=false;

        public BVTask(int id,string tn,DeviceConnectControl dc)
        {
            TaskID = id;
            TaskName = tn;
            DeviceC = dc;

            Thread TaskDO = new Thread(TaskControl);
            TaskDO.IsBackground = true;
            TaskDO.Start();
        }
        async void TaskControl()
        {
            while(true)
            {
                switch (Remoteflag)
                {
                    case 0:DeviceC.Send("stop"); Remoteflag = -1; return;
                    case 1: if (Taskflag != true) { Taskflag = true; await Run(); } Remoteflag = -1; break;
                    default:Thread.Sleep(10); break;
                }
            }
        }

        Task Run()
        {
            return Task.Run(() =>
            {
                DeviceC.Send("play");
                while(Taskflag)
                {
                    if (DeviceC.data.barvolumedata.Getnum() == 20)
                    {
                        DeviceC.data.barvolumedatas.Add(DeviceC.data.barvolumedata);
                        DeviceC.data.barvolumedata.Setnum(0);
                    }
                }
            });
        }

        public void TaskRemote(int flag)
        {
            Remoteflag = flag;
        }
        public void GetTaskConfig(out int TaskID, out string TaskName)
        {
            TaskID = this.TaskID;
            TaskName = this.TaskName;
        }
        public void GetResults(out object bvdatalist)
        {
            bvdatalist = DeviceC.data.barvolumedatas;
        }

    }
}
