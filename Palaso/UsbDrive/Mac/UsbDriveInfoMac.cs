#if __MonoCS__
using System;
using System.IO;
using System.Collections.Generic;
using MonoMac.Foundation;

namespace Palaso.UsbDrive.Mac
{
	internal class UsbDriveInfoMac : UsbDriveInfo
	{
		private static readonly NSString NSURLIsVolumeKey = new NSString("NSURLIsVolumeKey");
		private static readonly NSString NSURLIsWritableKey = new NSString("NSURLIsWritableKey");
		private static readonly NSString NSURLVolumeLocalizedNameKey = new NSString("NSURLVolumeLocalizedNameKey");
		private static readonly NSString NSURLVolumeAvailableCapacityKey = new NSString("NSURLVolumeAvailableCapacityKey");
		private static readonly NSString NSURLVolumeTotalCapacityKey = new NSString("NSURLVolumeTotalCapacityKey");
		private static readonly NSString NSURLVolumeURLKey = new NSString("NSURLVolumeURLKey");
		private static readonly NSString NSURLVolumeIsRemovableKey = new NSString("NSURLVolumeIsRemovableKey");
		private static readonly NSString NSURLPathKey = new NSString("_NSURLPathKey");

		private NSDictionary resourceValues;
		private NSUrl url;

		private UsbDriveInfoMac()
		{
		}

		public override bool IsReady
		{
			get { return true; }
		}

		public override DirectoryInfo RootDirectory
		{
			get { return new DirectoryInfo(resourceValues[NSURLPathKey].ToString()); }
		}

		public override string VolumeLabel
		{
			get { return resourceValues[NSURLVolumeLocalizedNameKey].ToString(); }
		}

		public override ulong TotalSize
		{
			get { return (ulong) (NSNumber) resourceValues[NSURLVolumeTotalCapacityKey]; }
		}

		public override ulong AvailableFreeSpace
		{
			get { return (ulong) (NSNumber) resourceValues[NSURLVolumeAvailableCapacityKey]; }
		}

		public new static List<IUsbDriveInfo> GetDrives()
		{
			var drives = new List<IUsbDriveInfo>();

			NSString[] keys = new NSString[] {
				NSURLIsVolumeKey,
				NSURLVolumeLocalizedNameKey,
				NSURLVolumeAvailableCapacityKey,
				NSURLVolumeTotalCapacityKey,
				NSURLVolumeIsRemovableKey,
				NSURLPathKey
			};

			NSFileManager fm = NSFileManager.DefaultManager;
			var volumes = fm.GetMountedVolumes(NSArray.FromObjects(keys), NSVolumeEnumerationOptions.SkipHiddenVolumes);
			foreach (var url in volumes)
			{
				NSError error;
				var values = url.GetResourceValues(keys, out error);
				if ((bool) (NSNumber) values[NSURLVolumeIsRemovableKey])
				{
					var driveInfo = new UsbDriveInfoMac();
					driveInfo.resourceValues = values;
					driveInfo.url = url;
					drives.Add(driveInfo);
				}
			}

			return drives;
		}
	}
}
#endif
