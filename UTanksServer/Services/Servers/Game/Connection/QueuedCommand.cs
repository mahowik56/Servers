using System;

namespace UTanksServer.Services.Servers.Game.Connection {
  public readonly struct QueuedCommand {
    public QueuedCommand(ICommand command, CommandSendOptions options, int ticket) {
      Command = command;
      Options = options;
      Ticket = ticket;
      EnqueueTime = DateTime.UtcNow;
    }

    public ICommand Command { get; }
    public CommandSendOptions Options { get; }
    public int Ticket { get; }
    public DateTime EnqueueTime { get; }
  }
}
