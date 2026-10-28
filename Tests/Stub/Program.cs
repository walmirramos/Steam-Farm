using System;
using System.Threading;
class Program
{
    static int Main(string[] args)
    {
        if (args[0] == "42") return 7;
        if (args[0] == "43") return 0;
        Console.Error.WriteLine("Informational diagnostic, not an error.");
        Thread.Sleep(60000);
        return 0;
    }
}
