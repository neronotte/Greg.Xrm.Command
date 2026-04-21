namespace Greg.Xrm.Command
{
	public interface ICommandRunnerFactory
	{
		ICommandRunner CreateCommandRunner();
	}
}
