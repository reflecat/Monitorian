﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Display;
using Windows.Devices.Enumeration;

namespace Monitorian.Supplement
{
	/// <summary>
	/// DisplayMonitor functions
	/// </summary>
	/// <remarks>
	/// This class wraps <see cref="Windows.Devices.Display.DisplayMonitor"/> class which is only available
	/// on Windows 10 (Redstone 4, 1803 = version 10.0.17134.0) or newer.
	/// </remarks>
	public class DisplayInformation
	{
		#region Type

		/// <summary>
		/// Display monitor information
		/// </summary>
		[DataContract]
		public class DisplayItem
		{
			/// <summary>
			/// Device ID (Not device interface ID)
			/// </summary>
			[DataMember(Order = 0)]
			public string DeviceInstanceId { get; private set; }

			/// <summary>
			/// Display name
			/// </summary>
			[DataMember(Order = 1)]
			public string DisplayName { get; private set; }

			/// <summary>
			/// Whether the display is connected internally.
			/// </summary>
			[DataMember(Order = 2)]
			public bool IsInternal { get; private set; }

			/// <summary>
			/// Connection description
			/// </summary>
			[DataMember(Order = 3)]
			public string ConnectionDescription { get; private set; }

			internal DisplayItem(
				string deviceInstanceId,
				string displayName,
				bool isInternal,
				string connectionDescription = null)
			{
				this.DeviceInstanceId = deviceInstanceId;
				this.DisplayName = displayName;
				this.IsInternal = isInternal;
				this.ConnectionDescription = connectionDescription;
			}
		}

		#endregion

		// System error code: 0x80070057 = 0x57 = ERROR_INVALID_PARAMETER
		// Error message: The parameter is incorrect.
		private const uint ERROR_INVALID_PARAMETER = 0x80070057;

		// System error code: 0x8007001F = 0x1F = ERROR_GEN_FAILURE
		// Error message: A device attached to the system is not functioning.
		private const uint ERROR_GEN_FAILURE = 0x8007001F;

		/// <summary>
		/// Gets display monitor information.
		/// </summary>
		/// <returns>Array of display monitor information</returns>
		public static async Task<DisplayItem[]> GetDisplayMonitorsAsync()
		{
			const string deviceInstanceIdKey = "System.Devices.DeviceInstanceId";

			try
			{
				var devices = await DeviceInformation.FindAllAsync(DisplayMonitor.GetDeviceSelector(), new[] { deviceInstanceIdKey });
				if (devices?.Any() == true)
				{
					var items = new List<DisplayItem>(devices.Count);
					foreach (var device in devices)
					{
						if (!device.Properties.TryGetValue(deviceInstanceIdKey, out object value))
							continue;

						var deviceInstanceId = value as string;
						if (string.IsNullOrWhiteSpace(deviceInstanceId))
							continue;

						var displayMonitor = await DisplayMonitor.FromInterfaceIdAsync(device.Id);
						//var displayMonitor = await DisplayMonitor.FromIdAsync(deviceInstanceId);

						//Debug.WriteLine($"DeviceInstanceId: {deviceInstanceId}");
						//Debug.WriteLine($"DeviceName: {device.Name}");
						//Debug.WriteLine($"DisplayName: {displayMonitor.DisplayName}");
						//Debug.WriteLine($"ConnectionKind: {displayMonitor.ConnectionKind}");
						//Debug.WriteLine($"PhysicalConnector: {displayMonitor.PhysicalConnector}");

						items.Add(new DisplayItem(
							deviceInstanceId: deviceInstanceId,
							displayName: displayMonitor.DisplayName,
							isInternal: (displayMonitor.ConnectionKind == DisplayMonitorConnectionKind.Internal),
							connectionDescription: GetConnectionDescription(displayMonitor.ConnectionKind, displayMonitor.PhysicalConnector)));
					}
					return items.ToArray();
				}
			}
			catch (ArgumentException ax) when ((uint)ax.HResult == ERROR_INVALID_PARAMETER)
			{
			}
			catch (Exception ex) when ((uint)ex.HResult == ERROR_GEN_FAILURE)
			{
			}
			return Array.Empty<DisplayItem>();
		}

		private static string GetConnectionDescription(DisplayMonitorConnectionKind connectionKind, DisplayMonitorPhysicalConnectorKind connectorKind)
		{
			switch (connectionKind)
			{
				case DisplayMonitorConnectionKind.Internal:
				case DisplayMonitorConnectionKind.Virtual:
				case DisplayMonitorConnectionKind.Wireless:
					return connectionKind.ToString();

				case DisplayMonitorConnectionKind.Wired:
					switch (connectorKind)
					{
						case DisplayMonitorPhysicalConnectorKind.AnalogTV:
						case DisplayMonitorPhysicalConnectorKind.DisplayPort:
							return connectorKind.ToString();

						case DisplayMonitorPhysicalConnectorKind.Dvi:
						case DisplayMonitorPhysicalConnectorKind.Hdmi:
						case DisplayMonitorPhysicalConnectorKind.Lvds:
						case DisplayMonitorPhysicalConnectorKind.Sdi:
							return connectorKind.ToString().ToUpper();

						case DisplayMonitorPhysicalConnectorKind.HD15:
							return "VGA";
					}
					break;
			}
			return null;
		}
	}
}
