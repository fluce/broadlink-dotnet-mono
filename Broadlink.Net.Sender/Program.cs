using System;
using System.Threading;
using System.Threading.Tasks;
using Broadlink.NET;

namespace Broadlink.Net.Sender
{
	public static class RMDeviceExtensions
	{
		public static void SendCommand(this RMDevice sender, string command)
		{
			sender.SendRemoteCommandAsync(command.HexToBytes()).Wait();
		}

		public static RMDevice WaitUntilReady (this RMDevice device) 
		{
			if (!device.IsEventsReady)
			{
				bool ready = false;

				EventHandler handler = (sen, er) => {
					ready = true;
				};

				device.OnDeviceReady += handler;
				//e.OnTemperature += RMDevice_OnTemperature;
				//e.OnRawData += RMDevice_OnRawData;
				//e.OnRawRFDataFirst += RMDevice_OnRawRFDataFirst;
				//e.OnRawRFDataSecond += RMDevice_OnRawRFDataSecond;
				//e.OnSentDataCallback += RMDevice_OnSentDataCallback;
				device.IsEventsReady = true;
				device.AuthorizeAsync().Wait();

				for (int i = 0; i < 20 && !ready; i++)
				{
					Task.Delay(1000).Wait();
				}	

				device.OnDeviceReady -= handler;

				if (!ready) {
					Console.WriteLine("Device was not ready in the requested time");
				}
			}
			return device;
		}
	}

	class BroadLinkService : IDisposable
	{
		const int DefaultWaitTime = 25;

		Client discoverClient;
		RMDevice rmDevice;

		public BroadLinkService () 
		{
			discoverClient = new Client();
		}

		public RMDevice DiscoverDevice () 
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

		public async Task Search ()
		{
			EventHandler<BroadlinkDevice> handler = (s, e) => {
				rmDevice = e as RMDevice;
			};

			discoverClient.DeviceHandler += handler;

			rmDevice = null;
			discoverClient.DiscoverAsync().Wait();

			for (int i = 0; i < DefaultWaitTime && rmDevice == null; i++) {
				await Task.Delay(1000);
			}

			discoverClient.DeviceHandler -= handler;
		}
	}

	class MainClass
	{
		static string TV = "260014025012291215132813151229121512151328131512151315121500034c5112291215122912151328131512151328131512151215131500034c5013291215122912151328131512151229131512151215131500034e5012291215132912151229121513151229121512151315121500034e5112291215122912151328131512151328131512151215131500034e5013281315122912151229121513151229121513151215121500034f5012291215132813151229121512151328131512151315121500034e5013281315122912151328131512151229121513151215131400034f5012291215132813151229121513151229121512151315121500034e5013281315122912151328131512151229121513151215131500034e5012291215132813151229121513151229121512151315121500034f5012291215122912151329121512151328131512151215131500034e5012291315122912151229121513151229121513151215121500034e5112291215122913151229121512151328131512151215131500034e501328131512291215122913151215122912151315121512150003505013291215122912151328131512151229131512151215131500034c5013291215122912151328131512151229131512151215131500034c5013281315122912151328131512151229121513151215131500034c50132813151229121512291315121512291215131512151215000d051500034f";

		public static void Main(string[] args)
		{
			var command = TV.HexToBytes();

			BroadLinkService service = new BroadLinkService();

			var device = service.DiscoverDevice()
			                    .WaitUntilReady();
			device.SendCommand(TV);

			device.Dispose();
			service.Dispose();

			Console.WriteLine("Finshed!!!!");
			Console.ReadKey();
		}
	}
}
