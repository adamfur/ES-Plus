using System;
using System.Threading;

namespace ESPlus.Misc
{
	public class SessionScope : IDisposable
	{
		private sealed class AmbientData : IDisposable
		{
			public IDisposable Session { get; set; }

			public void Dispose()
			{
				Session?.Dispose();
			}
		}

		private static readonly AsyncLocal<AmbientData> AsyncLocal = new AsyncLocal<AmbientData>();

		public SessionScope()
		{
			AsyncLocal.Value = new AmbientData();
		}

		public static IDisposable Get() => AsyncLocal.Value?.Session;

		public static void Set(IDisposable value)
		{
			if (AsyncLocal.Value is AmbientData data)
			{
				data.Session = value;
			}
		}

		public void Dispose()
		{
			AsyncLocal.Value?.Dispose();
		}
	}
}