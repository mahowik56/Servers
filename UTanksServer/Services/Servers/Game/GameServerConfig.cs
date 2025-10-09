using System.Net;

namespace UTanksServer.Services.Servers.Game {
  public class GameServerConfig {
    public IPAddress Address { get; set; } = IPAddress.Any;
    public IPAddress PublicAddress { get; set; } = IPAddress.Any;
    public int Port { get; set; } = 5050;
    public int Backlog { get; set; } = 128;

    public int TickRate { get; set; } = 60;
    public int MaxPlayers { get; set; } = 1024;

    public int PerClientSendQueueBytes { get; set; } = 64 * 1024;
    public int GlobalSendQueueBytes { get; set; } = 8 * 1024 * 1024;

    public int MaxBatchBytes { get; set; } = 1400;
    public int MaxCommandsPerBatch { get; set; } = 64;

    public float InterestCellSize { get; set; } = 20f;
    public float InterestRadius { get; set; } = 60f;
  }
}
