// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using FireBrowserWinUi3.Services.Contracts;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.System.Profile;


namespace FireBrowserWinUi3.Services
{
    public class GraphService : IGraphService
    {
        private IAuthenticationService _authenticationService;

        private User _user;
        private Stream _userPhoto;
        private TimeZoneInfo _userTimeZone;

        public GraphService()
        {
            _authenticationService = App.GetService<MsalAuthService>();
        }

        public Task CreateEventAsync(Event newEvent)
        {
            var graphClient = _authenticationService.GraphClient;

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
            var graphClient = _authenticationService.GraphClient;

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
            var graphClient = _authenticationService.GraphClient;

            if (_authenticationService.IsSignedIn)
            {
                if (_user == null)
                {
                    // Get the user, cache for subsequent calls
                    _user = await graphClient.Me.GetAsync(
                        requestConfiguration =>
                        {
                            requestConfiguration.QueryParameters.Select =
                                new[] { "displayName", "mail", "mailboxSettings", "userPrincipalName" };
                        });
                }
            }
            else
            {
                _user = null;
            }

            return _user;
        }

        public async Task<Stream> GetUserPhotoAsync()
        {
            var graphClient = _authenticationService.GraphClient;

            if (_authenticationService.IsSignedIn)
            {
                if (_userPhoto == null)
                {
                    // Get the user photo, cache for subsequent calls
                    _userPhoto = await graphClient.Me
                        .Photo
                        .Content
                        .GetAsync();
                }
            }
            else
            {
                _userPhoto = null;
            }

            return _userPhoto;
        }

       
    }
}
