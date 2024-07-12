using Acceptor;
using QuickFix;

namespace Executor
{
    class Program
    {
        private const string HttpServerPrefix = "http://127.0.0.1:5080/";

        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                SessionSettings settings = new SessionSettings("acceptor.cfg");
                IApplication executorApp = new Acceptor();
                IMessageStoreFactory storeFactory = new FileStoreFactory(settings);
                ILogFactory logFactory = new ScreenLogFactory(settings);
                ThreadedSocketAcceptor acceptor = new ThreadedSocketAcceptor(executorApp, storeFactory, settings, logFactory);
                HttpServer srv = new HttpServer(HttpServerPrefix, settings);

                acceptor.Start();
                srv.Start();

                Console.WriteLine("View Acceptor status: " + HttpServerPrefix);
                Console.WriteLine("press <enter> to quit");
                Console.Read();

                srv.Stop();
                acceptor.Stop();
            }
            catch (Exception e)
            {
                Console.WriteLine("==FATAL ERROR==");
                Console.WriteLine(e.ToString());
            }
        }
    }
}
