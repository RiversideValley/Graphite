// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Graph.Models;
using Microsoft.UI.Xaml.Media.Imaging;
using Riverside.Graphite.Runtime.Helpers.Logging;
using Riverside.Graphite.Services.Contracts;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.System.Profile;


namespace Riverside.Graphite.Services
{
	public class GraphService : IGraphService
	{
		private User _user;
		private TimeZoneInfo _userTimeZone;
		public BitmapImage ProfileMicrosoft { get; set; }

		public GraphService()
		{
		}

		public Task CreateEventAsync(Event newEvent)
		{
			Microsoft.Graph.GraphServiceClient graphClient = AppService.MsalService.GraphClient;

			return graphClient.Me.Events.PostAsync(newEvent);
		}

		public class DeviceInfo
		{
			public static string GetDevicePlatformAsync()
			{
				string analyticsInfo = AnalyticsInfo.VersionInfo.DeviceFamily;
				string devicePlatform = analyticsInfo == "Windows.Desktop" ? "Desktop" :
									analyticsInfo == "Windows.Mobile" ? "Mobile" :
									analyticsInfo == "Windows.Team" ? "HoloLens" :
									analyticsInfo == "Windows.IoT" ? "IoT" :
									"Unknown";

				return devicePlatform;
			}
		}
		public Task<EventCollectionResponse> GetCalendarForDateTimeRangeAsync(DateTime start, DateTime end, TimeZoneInfo timeZone)
		{
			Microsoft.Graph.GraphServiceClient graphClient = AppService.MsalService.GraphClient;

			string timeZoneString = DeviceInfo.GetDevicePlatformAsync() == "Desktop" ?
				timeZone.StandardName : timeZone.Id;

			return graphClient.Me
				.CalendarView
				.GetAsync(requestConfiguration =>
				{
					requestConfiguration.Headers.Add("Prefer",
						$"outlook.timezone=\"{timeZoneString}\"");
					// Calendar view API sets the time period using query parameters
					// ?startDatetime={start}&endDateTime={end}
					requestConfiguration.QueryParameters.StartDateTime =
						start.ToString("o");
					requestConfiguration.QueryParameters.EndDateTime =
						end.ToString("o");
					requestConfiguration.QueryParameters.Select =
						new[] { "subject", "organizer", "start", "end" };
					requestConfiguration.QueryParameters.Orderby =
						new[] { "start/DateTime" };
					requestConfiguration.QueryParameters.Top = 50;
				});
		}

		public async Task<User> GetUserInfoAsync()
		{
			try
			{
				if (AppService.MsalService.IsSignedIn)
				{
					// Get the user, cache for subsequent calls
					_user ??= await AppService.MsalService.GraphClient.Me.GetAsync();
				}
				else
				{
					_user = null;
				}

				return _user;
			}
			catch (Exception e)
			{
				ExceptionLogger.LogException(e);
			}

			return null;
		}

		public async Task<Stream> GetUserPhotoAsync()
		{
			Microsoft.Graph.GraphServiceClient graphClient = AppService.MsalService.GraphClient;
			Stream _userPhoto;

			if (AppService.MsalService.IsSignedIn)
			{
				// Get the user photo, cache for subsequent calls
				_userPhoto = await graphClient.Me
					.Photo
					.Content
					.GetAsync();


				if (_userPhoto is not null)
				{
					if (ProfileMicrosoft is null)
					{
						MemoryStream memoryStream = new();
						await _userPhoto.CopyToAsync(memoryStream);
						memoryStream.Position = 0;
						BitmapImage bitmapImage = new();
						await bitmapImage.SetSourceAsync(memoryStream.AsRandomAccessStream());
						ProfileMicrosoft = bitmapImage;
					}
					return _userPhoto;
				}
				else
				{
					MemoryStream memoryStream = new();
					using FileStream fileStream = new("ms-appx:///Assets/Microsoft.png", FileMode.Open, FileAccess.Read);
					await fileStream.CopyToAsync(memoryStream);
					memoryStream.Position = 0;
					BitmapImage bitmapImage = new();
					await bitmapImage.SetSourceAsync(memoryStream.AsRandomAccessStream());
					ProfileMicrosoft = bitmapImage;
				}
			}

			return null;
		}
	}
}
