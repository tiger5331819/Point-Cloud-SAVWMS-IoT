namespace SAVWMS
{
    class Program
    {
        static void Main(string[] args)
        {
            CenterManager centerManager = new CenterManager();
            centerManager.centerNetManager.serverLink();

            ConnectionControlCenter connectionControlCenter = new ConnectionControlCenter(ref centerManager);
        }
    }
}
