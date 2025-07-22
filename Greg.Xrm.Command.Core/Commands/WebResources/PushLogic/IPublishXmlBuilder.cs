﻿using Microsoft.Crm.Sdk.Messages;

namespace Greg.Xrm.Command.Commands.WebResources.PushLogic
{
	public interface IPublishXmlBuilder
	{
		void Clear();

		void AddWebResource(Guid id);
		void AddTable(string tableName);
		void AddGlobalOptionSet(string name);

		PublishXmlRequest? Build();
	}
}