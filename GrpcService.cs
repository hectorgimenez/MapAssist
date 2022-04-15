using System;
using System.Collections.Generic;
using Grpc.Core;
using System.Threading.Tasks;
using MapAssist.Helpers;
using MapAssist.Types;

namespace MapAssist
{
    internal class GrpcService: IDisposable
    {
        const int Port = 50051;

        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();

        private GameDataReader _gameDataReader;
        private GameData _gameData;
        private AreaData _areaData;
        private List<PointOfInterest> _pointsOfInterest;
        //private Compositor _compositor;
        private static readonly object _lock = new object();
        private Server _server;

        public void runServer()
        {
            _server = new Server
            {
                Services = { koolo.mapassist.api.MapAssistApi.BindService(new GrpcServer()) },
                Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) }
            };
            _server.Start();

            Console.WriteLine("Listening for connections on " + Port);
        }

        ~GrpcService() => Dispose();

        private bool disposed = false;

        public void Dispose()
        {
            // Close the listener
            //Console.WriteLine("Shutting down gRPC Server");
            //_server.ShutdownAsync().Wait();
            lock (_lock)
            {
                if (!disposed)
                {
                    disposed = true; // This first to let GraphicsWindow.DrawGraphics know to return instantly
                    //if (_compositor != null) _compositor.Dispose(); // This last so it's disposed after GraphicsWindow stops using it
                }
            }
        }
    }
}
