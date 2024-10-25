// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Graph.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FireBrowserWinUi3.Services.Contracts
{
    public interface IGraphService
    {
        public Task<User> GetUserInfoAsync();

        public Task<Stream> GetUserPhotoAsync();

        public Task<EventCollectionResponse> GetCalendarForDateTimeRangeAsync(DateTime start, DateTime end, TimeZoneInfo timeZone);

        public Task CreateEventAsync(Event newEvent);
    }
}
