namespace SAVWMS
{
    class Program
    {
        static void Main(string[] args)
        {
            CenterManager centerManager = new CenterManager();
            centerManager.centerNetManager.serverLink();

            ControlCenter connectionControlCenter = new ControlCenter(ref centerManager);
        }
    }
}
