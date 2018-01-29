using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
namespace Broadlink.NET
{
	public class Network
	{
		public IPAddress BroadcastIPAddress { get; set; }
		public IPAddress LocalIPAddress { get; set; }

		public static IPAddress GetLocalIPAddress()
		{
			var result = new List<IPAddress>();
			IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
			foreach (IPAddress addr in localIPs)
			{
				if (addr.AddressFamily == AddressFamily.InterNetwork)
				{
					return addr;
				}
			}
			return null;
		}

		public static IEnumerable<Network> GetNetworks()
		{
			var localIPAddress = GetLocalIPAddress();
			if (localIPAddress == null) {
				throw new Exception("No local Ip address detected");
			}

			foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
			{
				if (adapter.Supports(NetworkInterfaceComponent.IPv4) && adapter.OperationalStatus == OperationalStatus.Up)
				{

					foreach (var unicast in adapter.GetIPProperties().UnicastAddresses)
					{
						Network model = null;
						try
						{
							var address = unicast.Address;

							if (localIPAddress.ToString() != address.ToString())
							{
								continue;
							}

							var mask = unicast.IPv4Mask;
							var addressInt = BitConverter.ToInt32(address.GetAddressBytes(), 0);
							if (mask == null) continue;
							var maskInt = BitConverter.ToInt32(mask.GetAddressBytes(), 0);
							var broadcastInt = addressInt | ~maskInt;
							model = new Network
							{
								LocalIPAddress = unicast.Address,
								BroadcastIPAddress = new IPAddress(BitConverter.GetBytes(broadcastInt))
							};
						}
						catch (Exception ex)
						{
							Console.WriteLine(ex);
						}
						if (model != null)
						{
							yield return model;
						}
					}
				}
			}
		}
	}
}
