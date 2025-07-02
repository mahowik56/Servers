using Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UTanksServer.ECS.ECSCore
{
    public static class ManagerScope
    {
        public static ECSSystemManager systemManager;
        public static ECSEntityManager entityManager;
        public static ECSComponentManager componentManager;
        public static ECSEventManager eventManager;

        private static CancellationTokenSource? _cts;

        public static void InitManagerScope()
        {
            entityManager = new ECSEntityManager();
            componentManager = new ECSComponentManager();
            ECSComponentManager.IdStaticCache();
            eventManager = new ECSEventManager();
            eventManager.IdStaticCache();
            systemManager = new ECSSystemManager();
            systemManager.InitializeSystems();
            eventManager.InitializeEventManager();
            _cts = new CancellationTokenSource();
            _ = Task.Run(async () =>
            {
                while(!_cts.IsCancellationRequested)
                {
                    systemManager.RunSystems();
                    await Task.Delay(5, _cts.Token);
                }
            });
            Logger.Log("ECS managers initialized");
        }

        public static void StopManagerScope()
        {
            _cts?.Cancel();
        }
    }
}
