using System;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Threading;


namespace SAVWMS
{
    public class SignalChangeDo
    {
        public Manager manager;
        //判断是否需要进行线程
        public bool sendBVFlag;
        //判断跟之前的数据是否一致
        public string nowDataVol;
        public string nowDataBar;
        //判断数据是否被上传
        public int sendBarFlag;
        //两个文件的名字
        public string filePathBar;
        public string filePathVol;
        //原路径和目标路径
        public string srcPath;//源路径
        public string destPath;//目标路径

        public SignalChangeDo(ref Manager manag)
        {
            sendBVFlag = false;
            filePathVol = "VolumeMeasuringSystem//now_volume.txt";
            nowDataVol = "";
            nowDataBar = "";
            sendBarFlag = 0;
            //给源路径和目标路径赋值
            srcPath = "BarCodeScanningSystem//Csv";
            destPath = "BarCodeScanningSystem//CsvOld";
            manager = manag;

        }
        public void Play()
        {
            //这个地方先把以前的条码数据文件全部移到另一个文件夹下
            //1.判断两个文件夹路径是否都存在
            if (!Directory.Exists(destPath)) 
            {
                Console.WriteLine("新建文件");
                Directory.CreateDirectory(destPath);
            }
            if (!Directory.Exists(srcPath))
            {
                Console.WriteLine("没有找到目标文件夹");
            }

            //2将源路径中的数据文件复制到目标路径中
            CopyDirectory(srcPath, destPath);

            //3将源路径中的数据文件全部删除
            //DelectDir(srcPath);

            //这个地方给进程赋值后再使用
            //manager.process1.Start();
           // manager.process2.Start();

            Thread.Sleep(1000);
            //获取条码数据文件名
            GetFileName();
            Console.WriteLine("开始测试");
            //Console.WriteLine(manager.process1.StartInfo.FileName + "  play");
            //Console.WriteLine(manager.process2.StartInfo.FileName + "  play");
            sendBVFlag = true;
          
            //开始从文本中获取数据
            Thread getBData = new Thread(getBarDataFromFile);
            getBData.IsBackground = true;
            getBData.Start();
            Thread getVData = new Thread(getVolumeDataFromFile);
            getVData.IsBackground = true;
            getVData.Start();

            //数据上传到数据处理服务武器
            Thread sendData = new Thread(sendDataToService);
            sendData.IsBackground = true;
            sendData.Start();

            //数据上传到数据库服务器
            //Thread sendDataToSQL = new Thread(sendDataToSQLSever);
            //sendDataToSQL.IsBackground = true;
            //sendDataToSQL.Start();

        }
        /// <summary>
        /// 将源路径中的所有文件复制到目标路径中
        /// </summary>
        /// <param name="srcPath">源路径</param>
        /// <param name="destPath">目标路径</param>
        public void CopyDirectory(string srcPath,string destPath)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(srcPath);
                FileSystemInfo[] fileinfo = dir.GetFileSystemInfos();  //获取目录下（不包含子目录）的文件和子目录
                foreach (FileSystemInfo i in fileinfo)
                {
                    if (i is DirectoryInfo)     //判断是否文件夹
                    {
                        if (!Directory.Exists(destPath + "\\" + i.Name))
                        {
                            Directory.CreateDirectory(destPath + "\\" + i.Name);   //目标目录下不存在此文件夹即创建子文件夹
                        }
                        CopyDirectory(i.FullName, destPath + "\\" + i.Name);    //递归调用复制子文件夹
                    }
                    else
                    {
                        File.Copy(i.FullName, destPath + "\\" + i.Name, true);      //不是文件夹即复制文件，true表示可以覆盖同名文件
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("复制出错");
                throw;
            }
        }

        /// <summary>
        /// 将源路径中的数据全部删除
        /// </summary>
        /// <param name="srcPath"></param>
        public void DelectDir(string srcPath)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(srcPath);
                FileSystemInfo[] fileinfo = dir.GetFileSystemInfos();  //返回目录中所有文件和子目录
                foreach (FileSystemInfo i in fileinfo)
                {
                    if (i is DirectoryInfo)            //判断是否文件夹
                    {
                        DirectoryInfo subdir = new DirectoryInfo(i.FullName);
                        subdir.Delete(true);          //删除子目录和文件
                    }
                    else
                    {
                        File.Delete(i.FullName);      //删除指定文件
                    }
                }
            }
            catch (Exception e)
            {
                throw;
            }
        }

        /// <summary>
        /// 获取文件名
        /// </summary>
        public void GetFileName()
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(srcPath);
                FileSystemInfo[] fileinfo = dir.GetFileSystemInfos();  //获取目录下（不包含子目录）的文件和子目录
                foreach(FileSystemInfo i in fileinfo)
                {
                    if(i is DirectoryInfo)
                    {
                        Console.WriteLine("没有找到数据文件");
                    }
                    else
                    {
                        filePathBar = srcPath + "//" + i.Name;
                    }
                }

                Console.WriteLine(filePathBar);
            }
            catch(Exception ex)
            {
                
            }
        }

        public void Stop()
        {
            sendBVFlag = false;
            //manager.process1.Kill();
            //Console.WriteLine("Kill program" + "体积计算 " + manager.Volumeversion + " successfully");
            //manager.process2.Kill();
            //Console.WriteLine("Kill program" + manager.BarcodeScanname + " successfully");

            //体积数据发送到数据处理服务器，由数据处理服务器上传至服务器.
        }
        /// <summary>
        /// 从文件中获取体积数据并给到结构体数组中
        /// </summary>
        public void getVolumeDataFromFile()
        {
            //打开文件时保证文件中没有数据
            R: FileStream fsC = null;
            try
            {
                fsC = new FileStream(filePathVol, FileMode.Truncate, FileAccess.ReadWrite);

            }
            catch (Exception ex)
            {
                Console.WriteLine("删除数据失败");
                goto R;
            }
            finally
            {
                fsC.Close();
            }
            
            try
            {
                string strData = "0";
                
                while (true)
                {
                    if(!sendBVFlag)
                    {
                        continue;
                    }
                    try
                    {
                        FileStream fs = new FileStream(filePathVol, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        long n = fs.Length;
                        byte[] b = new byte[n];

                        int cnt, m;
                        m = 0;
                        cnt = fs.ReadByte();
                        while (cnt != -1)
                        {
                            b[m++] = Convert.ToByte(cnt);
                            cnt = fs.ReadByte();
                        }
                        strData = Encoding.Default.GetString(b);

                        

                        if (strData != nowDataVol && strData != "")
                        {
                            nowDataVol = strData;
                            
                            //添加数据
                            manager.Data.barvolumedata.addVolumeData(strData);
                            Console.WriteLine("vol添加数据成功");
                            Console.WriteLine(strData);
                            fs.Close();
                            fs.Dispose();
                        }
                        else
                        {
                            fs.Close();
                            fs.Dispose();
                        }
                    }
                    catch (Exception e)
                    {
                        // 向用户显示出错消息
                        Console.WriteLine("The volfile could not be read:");
                        Console.WriteLine(e.Message);
                        
                    }
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 从文件中获取条码数据并给到结构体数组中
        /// </summary>
        public void getBarDataFromFile()
        {
            string strData = "";
            string str = "";
            int location = 0;

            while (true)
            {
                if (!sendBVFlag)
                {
                    continue;
                }
                try
                {
                    //为了让文件避免因其他进程占用而不能读取；
                    FileStream fs = new FileStream(filePathBar, System.IO.FileMode.Open, System.IO.FileAccess.Read, FileShare.ReadWrite);
                    StreamReader sr = new StreamReader(fs);
                   
                    while ((str = sr.ReadLine()) != null)
                    {
                        strData = str;
                       
                    }

                    //找到第一个逗号的位置
                    location = strData.IndexOf(',', 0, strData.Length);
                    //截取字符串
                    strData = strData.Substring(0, location);
                    
                    if (strData != nowDataBar && strData != "")
                    {
                        nowDataBar = strData;

                        //添加数据
                        manager.Data.barvolumedata.addBarData(strData);
                        Console.WriteLine("bar添加数据成功");
                        Console.WriteLine(nowDataBar);
                        fs.Close();
                        sr.Close();
                        fs.Dispose();
                        sr.Dispose();
                    }
                    else
                    {
                        fs.Close();
                        sr.Close();
                        fs.Dispose();
                        sr.Dispose();
                    }
                }
                catch (Exception e)
                {
                    //Console.WriteLine("The barfile could not be read:");
                    //Console.WriteLine(e.Message);
                    //Console.WriteLine("正在尝试重新读取");
                }
            }



        }
        
        /// <summary>
        /// 向数据处理服务器发送数据
        /// </summary>
        public void sendDataToService()
        {
            while(true)
            {
                if(sendBarFlag != manager.Data.barvolumedata.barpointer)
                {
                    sendBarFlag = manager.Data.barvolumedata.barpointer;
                    manager.netManager.Send(manager.netManager.DeviceDataToPackageReal(manager.Data, Messagetype.barvolumepackage));
                }
            }
        }

        /// <summary>
        /// 直接向数据库发送数据
        /// </summary>
        public void sendDataToSQLSever()
        {
            while(true)
            {
                if(sendBarFlag != manager.Data.barvolumedata.barpointer)
                {
                    sendBarFlag = manager.Data.barvolumedata.barpointer;

                    int arrayFlag = sendBarFlag == 0 ? 19 : sendBarFlag - 1;
                    //向数据库发送消息

                    //数据库连接改到配置文件中
                    string connectString = "Data Source=127.0.0.1;Initial Catalog=SAVWMS;Integrated Security=True;user id=sa;password=12345678910ding";

                    SqlConnection sqlCnt = new SqlConnection(connectString);

                    sqlCnt.Open();
                    if (sqlCnt.State == System.Data.ConnectionState.Open)
                    {
                        Console.WriteLine("连接成功");
                    }

                    SqlCommand command = sqlCnt.CreateCommand();
                    Console.WriteLine(manager.Data.barvolumedata.Bvdata[manager.Data.barvolumedata.barpointer].BarcodeAcquisitionTime);
                   
                    string instertstr = "INSERT INTO BVWdatatable (TypeOfBarcode,BarcodeInformation,BarcodeAcquisitionTime,PackageVolume,VolumeAcquisitionTime,PackageWeight,WeightAcquisitionTime,TimeDifference1,TimeDifference2,note) ";
                    instertstr += "values('barcode','" + manager.Data.barvolumedata.Bvdata[arrayFlag].BarcodeInfmation + "','"  +manager.Data.barvolumedata.Bvdata[arrayFlag].BarcodeAcquisitionTime +"','" + manager.Data.barvolumedata.Bvdata[arrayFlag].PackageVolume + "','" +manager.Data.barvolumedata.Bvdata[arrayFlag].VolumeAcquisitionTime +"','0','" +DateTime.Now+" ','5','5','ceshi')";
                    
                    SqlCommand cmd = new SqlCommand(instertstr, sqlCnt);
                    cmd.ExecuteNonQuery();
                    Console.WriteLine("插入成功");
                    sqlCnt.Close();
                }
            }
        }
    }
}
