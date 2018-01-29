using System;
using System.Threading.Tasks;

namespace Broadlink.NET
{
	public class BroadLinkService : IDisposable
	{
		const int DefaultWaitTime = 25;

		Client discoverClient;
		RMDevice rmDevice;

		public BroadLinkService()
		{
			discoverClient = new Client();
		}

		public RMDevice DiscoverDevice()
		{
			RMDevice discovered = null;
			EventHandler<BroadlinkDevice> handler = (s, e) =>
			{
				discovered = e as RMDevice;
			};

			discoverClient.DeviceHandler += handler;

			discoverClient.DiscoverAsync().Wait();

			for (int i = 0; i < DefaultWaitTime; i++)
			{
				if (discovered != null)
					return discovered;
				Task.Delay(1000).Wait();
			}

			discoverClient.DeviceHandler -= handler;

			return discovered;
		}

		public void Dispose()
		{
			discoverClient.Dispose();
		}

		public async Task Search()
		{
			EventHandler<BroadlinkDevice> handler = (s, e) =>
			{
				rmDevice = e as RMDevice;
			};

			discoverClient.DeviceHandler += handler;

			rmDevice = null;
			discoverClient.DiscoverAsync().Wait();

			for (int i = 0; i < DefaultWaitTime && rmDevice == null; i++)
			{
				await Task.Delay(1000);
			}

			discoverClient.DeviceHandler -= handler;
		}
	}
}
