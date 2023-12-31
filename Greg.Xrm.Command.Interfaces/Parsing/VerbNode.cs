﻿using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Parsing
{
	/// <summary>
	/// Represents a node in the verb tree.
	/// </summary>
	public class VerbNode
	{
		private readonly INamespaceHelper helper;

		/// <summary>
		/// Creates a new instance of <see cref="VerbNode"/>.
		/// </summary>
		/// <param name="verb">The current level of the verb</param>
		/// <param name="parent">The parent node (optional)</param>
		public VerbNode(string verb, VerbNode? parent = null, INamespaceHelper? helper = null)
		{
			this.Verb = verb;
			this.Parent = parent;
			this.helper = helper ?? NamespaceHelper.Empty;
		}

		/// <summary>
		/// Gets the parent node.
		/// </summary>
		public VerbNode? Parent { get; }

		/// <summary>
		///	Gets the current verb.
		/// </summary>
		public string Verb { get; }

		/// <summary>
		///	Gets the help for the current verb, if provided.
		/// </summary>
		public string Help => this.helper.GetHelp();

		/// <summary>
		/// Gets the list of children of the current verb.
		/// </summary>
		public List<VerbNode> Children { get; } = new List<VerbNode>();


		/// <summary>
		/// Gets or sets the command associated with the current verb.
		/// Valid only for leaf nodes.
		/// </summary>
		public CommandDefinition? Command { get; set; }
		

		/// <summary>
		/// Indicates whether the current verb should be hidden from the help.
		/// </summary>
		public bool IsHidden 
		{
			get 
			{
				if (this.Command is not null)
					return this.Command.Hidden;

				return this.Children.TrueForAll(_ => _.IsHidden);
			}
		}

		public void WriteNamespaceHelp(MarkdownWriter writer)
		{
			this.helper.WriteHelp(writer);
		}


		public override string ToString()
		{
			if (this.Parent != null)
				return $"{this.Parent} {this.Verb}";

			return this.Verb;
		}
	}
}
