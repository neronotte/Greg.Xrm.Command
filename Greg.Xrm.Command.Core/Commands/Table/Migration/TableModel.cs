using Greg.Xrm.Command.Services.Graphs;

namespace Greg.Xrm.Command.Commands.Table.Migration;

    public class TableModel : INodeContent
{
	private readonly string name;

	public TableModel(string name)
	{
		this.name = name.ToLowerInvariant();
	}

	public object Key => name;


	public override string ToString()
	{
		return name;
	}
}
