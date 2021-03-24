using MythServer;
using System.Threading;
using System;
using Console = Colorful.Console;
using WatsonWebsocket;
using System.Text;

class Program
{

    [STAThread]
    static void Main(string[] args)
    {


        Thread t = new Thread(delegate ()
        {
            Server myserver = new Server("*", 4466);
        });

        t.Start();

        Console.WriteAscii("MYTH SERVER", System.Drawing.Color.AntiqueWhite);

    }

}