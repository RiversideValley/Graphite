// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using FireBrowserWinUi3.Services.Contracts;
using Fire.Core.Exceptions;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.Security;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.System.Profile;


namespace FireBrowserWinUi3.Services
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
            var graphClient = AppService.MsalService.GraphClient;

            return graphClient.Me.Events.PostAsync(newEvent);
        }

        public class DeviceInfo
        {
            public static string GetDevicePlatformAsync()
            {
                var analyticsInfo = AnalyticsInfo.VersionInfo.DeviceFamily;
                var devicePlatform = analyticsInfo == "Windows.Desktop" ? "Desktop" :
                                    analyticsInfo == "Windows.Mobile" ? "Mobile" :
                                    analyticsInfo == "Windows.Team" ? "HoloLens" :
                                    analyticsInfo == "Windows.IoT" ? "IoT" :
                                    "Unknown";

                return devicePlatform;
            }
        }
        public Task<EventCollectionResponse> GetCalendarForDateTimeRangeAsync(DateTime start, DateTime end, TimeZoneInfo timeZone)
        {
            var graphClient = AppService.MsalService.GraphClient;

            var timeZoneString = DeviceInfo.GetDevicePlatformAsync() == "Desktop" ?
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
                    if (_user == null)
                    {
                        // Get the user, cache for subsequent calls
                        _user = await AppService.MsalService.GraphClient.Me.GetAsync();
                    }
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
            var graphClient = AppService.MsalService.GraphClient;
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
                        var memoryStream = new MemoryStream();
                        await _userPhoto.CopyToAsync(memoryStream);
                        memoryStream.Position = 0;
                        var bitmapImage = new BitmapImage();
                        await bitmapImage.SetSourceAsync(memoryStream.AsRandomAccessStream());
                        ProfileMicrosoft = bitmapImage;

                    }
                    return _userPhoto;
                }
                else
                {
                    var memoryStream = new MemoryStream();
                    using (FileStream fileStream = new FileStream("ms-appx:///Assets/Microsoft.png", FileMode.Open, FileAccess.Read))
                    {
                        await fileStream.CopyToAsync(memoryStream);
                        memoryStream.Position = 0;
                        var bitmapImage = new BitmapImage();
                        await bitmapImage.SetSourceAsync(memoryStream.AsRandomAccessStream());
                        ProfileMicrosoft = bitmapImage;
                    }

                }
            }

            return null;
        }


    }
}
