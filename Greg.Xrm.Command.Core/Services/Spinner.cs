namespace Greg.Xrm.Command.Services
{
	public class Spinner
	{
		const string Sequence = @"|/-\";
		int currentIndex = 0;
		string currentValue = string.Empty;


		public string Spin()
		{
			var result = Sequence[currentIndex].ToString();
			currentIndex = (currentIndex + 1) % Sequence.Length;
			currentValue = result;
			return result;
		}

		public void Reset()
		{
			currentIndex = 0;
			currentValue = string.Empty;
		}

		public override string ToString()
		{
			return this.currentValue;
		}
	}
}
