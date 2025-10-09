using System;

namespace UTanksServer.Services.Servers.Game.Connection {
  public readonly struct CommandSendOptions {
    public static readonly CommandSendOptions Default = new CommandSendOptions(CommandPriority.Normal, reliable: false, estimatedSize: 128);

    public CommandSendOptions(CommandPriority priority, bool reliable, int estimatedSize) {
      Priority = priority;
      Reliable = reliable;
      EstimatedSize = Math.Max(0, estimatedSize);
    }

    public CommandPriority Priority { get; }
    public bool Reliable { get; }
    public int EstimatedSize { get; }

    public CommandSendOptions WithEstimatedSize(int size) => new CommandSendOptions(Priority, Reliable, size);

    public CommandSendOptions Promote(CommandPriority priority) {
      CommandPriority effective = priority < Priority ? priority : Priority;
      return new CommandSendOptions(effective, Reliable, EstimatedSize);
    }

    public CommandSendOptions RequireReliability() => new CommandSendOptions(Priority, reliable: true, EstimatedSize);
  }
}
