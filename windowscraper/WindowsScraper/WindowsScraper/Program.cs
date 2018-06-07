using WindowsServer;

namespace WindowsScraperTest
{
    class Program {
        static int Main(string[] args) {
            SinterServer.StartServer();
            SinterServer.StopServer();
            return 0;
        }
    }
}
