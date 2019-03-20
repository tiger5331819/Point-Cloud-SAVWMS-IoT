namespace SAVWMS
{
    public enum TypeNet
    {
        Device = 1,
        User = 20,
        CenterSever = 30
    }
    /// <summary>
    /// 指明接收到的包来自哪里，现在没有用到
    /// </summary>
    public enum Datatype
    {
        Device = 1,
        User = 2,

        CenterSever = 10,
        Sever = 11
    }
    /// <summary>
    /// 收到或发送的指令
    /// </summary>
    public enum Codemode
    {
        release = -1,
        stop = 0,
        play = 1,
        monitor = 2,
        sendvolume = 3,
        stopsendvolume = 4
    }


    /// <summary>
    /// package的数据说明
    /// </summary>
    public enum Messagetype
    {
        NULL = 0,
        ID = 1,//标记第一次来到，建立映射
        carinfomessage = 2,
        volumepackage = 3,
        order = 4,
        codeus = 5,
        package = 6,
        update = 7,
        barvolumepackage = 8
    }
    
}
