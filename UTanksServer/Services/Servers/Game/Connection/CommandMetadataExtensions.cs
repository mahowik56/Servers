namespace UTanksServer.Services.Servers.Game.Connection {
  public static class CommandMetadataExtensions {
    public static CommandSendOptions GetSendOptions(this ICommand command) {
      if(command is ICommandMetadataProvider provider) {
        return provider.GetSendOptions();
      }

      return CommandSendOptions.Default;
    }
  }
}
