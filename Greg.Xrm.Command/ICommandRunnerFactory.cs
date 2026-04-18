namespace Greg.Xrm.Command
{
	interface ICommandRunnerFactory
	{
		ICommandRunner CreateCommandRunner();
	}
}
