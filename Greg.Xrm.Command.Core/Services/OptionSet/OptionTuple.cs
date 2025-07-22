using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Services.OptionSet
{
	class OptionTuple
	{


		public OptionTuple(string text)
		{
			var parts = text.Trim().Split("=:".ToCharArray()).Select(x => x.Trim()).ToArray();
			if (parts.Length == 0)
				throw new ArgumentException($"The option '{text}' is not valid. It must be in the format 'label:value' or just 'label'.", nameof(text));

			if (parts.Length > 2)
				throw new ArgumentException($"The option '{text}' is not valid. It must be in the format 'label:value' or just 'label'.", nameof(text));


			this.Label = parts[0];
			if (parts.Length == 2)
			{
				if (!int.TryParse(parts[1], out int value))
					throw new ArgumentException($"The option '{text}' is not valid. The value must be an integer.", nameof(text));

				this.Value = value;
				this.HasValue = true;
			}
		}

		public string Label { get; set; } = string.Empty;
		public int Value { get; private set; } = 0;

		public bool HasValue { get; private set; } = false;


		public bool TrySetValue(int value)
		{
			if (this.HasValue)
				return false;

			this.Value = value;
			this.HasValue = true;
			return true;
		}
	}
}
