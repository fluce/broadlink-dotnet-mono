using System;
using System.Threading.Tasks;

namespace Broadlink.NET
{
	public static class RMDeviceExtensions
	{
		public static void SendCommand(this RMDevice sender, string command)
		{
			sender.SendRemoteCommandAsync(command.HexToBytes()).Wait();
		}

		public static RMDevice WaitUntilReady(this RMDevice device)
		{
			if (!device.IsEventsReady)
			{
				bool ready = false;

				EventHandler handler = (sen, er) =>
				{
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

				if (!ready)
				{
					Console.WriteLine("Device was not ready in the requested time");
				}
			}
			return device;
		}
	}
}
