using System;
namespace SAVWMS
{
    class Program
    {
       
        static void Main(string[] args)
        {
            Manager manager = new Manager();
            manager.netManager.userconnect();
            
            //manager.signalChangeToDo.ChangeDo.Play();


            Console.ReadLine();
        }
    }
}
