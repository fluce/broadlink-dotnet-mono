using Sockets.Plugin;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Broadlink.NET
{
	public class Client : IDisposable
	{
		public event EventHandler<BroadlinkDevice> DeviceHandler;

		private MyUdpSocketReceiver ClientSocket;
		private IPEndPoint BroadcastIPEndPoint;
		private List<Network> Networks;

		public Client()
		{
			BroadcastIPEndPoint = new IPEndPoint(IPAddress.Broadcast, 80);
			Networks = new List<Network>();
			Networks.AddRange(Network.GetNetworks());
		}

		private async Task SendDiscoveryPacketAsync()
		{
			var ports = new List<int> { 6775, 6776, 6777 }; // Network.GetAvailablePort(1000, Networks.Count);
			if (ports.Count == 0 || ports.Count < Networks.Count)
			{
				//HelperMy.Notification(Color.Red, "Kullanılabilir port bulunamadığı için işlem iptal edildi.");
				await ClientSocket.StopListeningAsync();
				ClientSocket.Dispose();
				ClientSocket = null;
				return;
			}
			for (int i = 0; i < Networks.Count; i++)
			{
				var item = Networks[i];
				var data = PacketGenerator.GenerateDiscoveryPacket(item.LocalIPAddress, (short)ports[i]);
				await ClientSocket.SendToAsync(data, new IPEndPoint(item.BroadcastIPAddress, 80));
			}
		}

		private void ClientSocket_MessageReceived(object sender, UdpReceiveResult e)
		{
			var response = e.Buffer;
			if (response != null)
			{
				var discoveredDevice = CreateBroadlinkDevice(BitConverter.ToInt16(response, 0x34));
				discoveredDevice.EndPoint = e.RemoteEndPoint;
				discoveredDevice.MacAddress = response.Slice(0x3a, 0x40);
				DeviceHandler?.Invoke(this, discoveredDevice);
			}
		}

		private BroadlinkDevice CreateBroadlinkDevice(short deviceType)
		{
			BroadlinkDevice device;
			switch (deviceType)
			{
				case 0x2712: // RM2
				case 0x2737: // RM Mini
				case 0x273d: // RM Pro Phicomm
				case 0x2783: // RM2 Home Plus
				case 0x277c: // RM2 Home Plus GDT
				case 0x272a: // RM2 Pro Plus
				case 0x2787: // RM2 Pro Plus2
				case 0x278b: // RM2 Pro Plus BL
				case 0x278f: // RM Mini Shate
					device = new RMDevice();
					break;
				default:
					device = new BroadlinkDevice();
					break;
			}
			device.DeviceType = deviceType;
			return device;
		}

		/// <summary>
		/// Discover Broadlink Device
		/// </summary>
		/// <returns>Triggered Event <see cref="DeviceHandler" /></returns>
		public async Task DiscoverAsync()
		{
			try
			{
				if (ClientSocket == null)
				{
					ClientSocket = new MyUdpSocketReceiver();
					ClientSocket.MessageReceived += new EventHandler<UdpReceiveResult>(ClientSocket_MessageReceived);
					await ClientSocket.StartListeningAsync();
				}
				await SendDiscoveryPacketAsync();
			}
			catch (Exception)
			{

			}
		}
		public void Dispose()
		{
			try
			{
				ClientSocket.StopListeningAsync().Wait();
				ClientSocket.Dispose();
				ClientSocket = null;
			}
			catch (Exception)
			{

			}
			GC.SuppressFinalize(this);
		}
	}
}
