using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
/// <summary>
/***
 *               ii.                                         ;9ABH,          
 *              SA391,                                    .r9GG35&G          
 *              &#ii13Gh;                               i3X31i;:,rB1         
 *              iMs,:,i5895,                         .5G91:,:;:s1:8A         
 *               33::::,,;5G5,                     ,58Si,,:::,sHX;iH1        
 *                Sr.,:;rs13BBX35hh11511h5Shhh5S3GAXS:.,,::,,1AG3i,GG        
 *                .G51S511sr;;iiiishS8G89Shsrrsh59S;.,,,,,..5A85Si,h8        
 *               :SB9s:,............................,,,.,,,SASh53h,1G.       
 *            .r18S;..,,,,,,,,,,,,,,,,,,,,,,,,,,,,,....,,.1H315199,rX,       
 *          ;S89s,..,,,,,,,,,,,,,,,,,,,,,,,....,,.......,,,;r1ShS8,;Xi       
 *        i55s:.........,,,,,,,,,,,,,,,,.,,,......,.....,,....r9&5.:X1       
 *       59;.....,.     .,,,,,,,,,,,...        .............,..:1;.:&s       
 *      s8,..;53S5S3s.   .,,,,,,,.,..      i15S5h1:.........,,,..,,:99       
 *      93.:39s:rSGB@A;  ..,,,,.....    .SG3hhh9G&BGi..,,,,,,,,,,,,.,83      
 *      G5.G8  9#@@@@@X. .,,,,,,.....  iA9,.S&B###@@Mr...,,,,,,,,..,.;Xh     
 *      Gs.X8 S@@@@@@@B:..,,,,,,,,,,. rA1 ,A@@@@@@@@@H:........,,,,,,.iX:    
 *     ;9. ,8A#@@@@@@#5,.,,,,,,,,,... 9A. 8@@@@@@@@@@M;    ....,,,,,,,,S8    
 *     X3    iS8XAHH8s.,,,,,,,,,,...,..58hH@@@@@@@@@Hs       ...,,,,,,,:Gs   
 *    r8,        ,,,...,,,,,,,,,,.....  ,h8XABMMHX3r.          .,,,,,,,.rX:  
 *   :9, .    .:,..,:;;;::,.,,,,,..          .,,.               ..,,,,,,.59  
 *  .Si      ,:.i8HBMMMMMB&5,....                    .            .,,,,,.sMr
 *  SS       :: h@@@@@@@@@@#; .                     ...  .         ..,,,,iM5
 *  91  .    ;:.,1&@@@@@@MXs.                            .          .,,:,:&S
 *  hS ....  .:;,,,i3MMS1;..,..... .  .     ...                     ..,:,.99
 *  ,8; ..... .,:,..,8Ms:;,,,...                                     .,::.83
 *   s&: ....  .sS553B@@HX3s;,.    .,;13h.                            .:::&1
 *    SXr  .  ...;s3G99XA&X88Shss11155hi.                             ,;:h&,
 *     iH8:  . ..   ,;iiii;,::,,,,,.                                 .;irHA  
 *      ,8X5;   .     .......                                       ,;iihS8Gi
 *         1831,                                                 .,;irrrrrs&@
 *           ;5A8r.                                            .:;iiiiirrss1H
 *             :X@H3s.......                                .,:;iii;iiiiirsrh
 *              r#h:;,...,,.. .,,:;;;;;:::,...              .:;;;;;;iiiirrss1
 *             ,M8 ..,....,.....,,::::::,,...         .     .,;;;iiiiiirss11h
 *             8B;.,,,,,,,.,.....          .           ..   .:;;;;iirrsss111h
 *            i@5,:::,,,,,,,,.... .                   . .:::;;;;;irrrss111111
 *            9Bi,:,,,,......                        ..r91;;;;;iirrsss1ss1111
 *            
 *            
 *            
 *            
 *            
 *刘增辉回去好好把客户端和子端的代码给我重构一遍！！！不能偷懒！！！！！！
 ***/
/// </summary>
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
                DeviceConnectControl d = GetDeviceC(did);
                DeviceTaskLake[Tasknum] = DeviceIoC.CreateDeviceTask(TaskCategory, Taskname,ref d, Tasknum);
                if (DeviceTaskLake[Tasknum] == null)
                { Console.WriteLine("DeviceTaskLake[Tasknum] == null : error TaskManager.SetDeviceTask"); return null; }
                Tasknum++;
                return DeviceTaskLake[Tasknum-1];
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
            Console.WriteLine("DeviceC is null!");
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

        public BVTask(int id,string tn,ref DeviceConnectControl dc)
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
                    if (DeviceC.data.barvolumedata.Getnum() == 19)
                    {
                        barvolumedata bv = new barvolumedata();
                        bv = DeviceC.data.barvolumedata.Clone();
                        DeviceC.barvolumedatas.Add(bv);
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
            bvdatalist = DeviceC.barvolumedatas;
            if (bvdatalist == null) Console.WriteLine("Task line:119");
        }

    }
}
