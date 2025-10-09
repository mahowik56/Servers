using UTanksServer.Core.Protocol.Commands;

namespace UTanksServer.Core.Protocol {
  public static class CommandCodecBootstrapper {
    private static bool _initialized;
    private static readonly object Sync = new object();

    public static void EnsureInitialized() {
      if(_initialized) {
        return;
      }

      lock(Sync) {
        if(_initialized) {
          return;
        }

        CommandCodecRegistry.Register(new HeartbeatCodec());
        _initialized = true;
      }
    }
  }
}
