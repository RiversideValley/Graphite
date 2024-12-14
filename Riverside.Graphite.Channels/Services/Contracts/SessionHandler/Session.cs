// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FireCore.Services.Contracts.SessionHandler
{
	public class Session
	{
		public string SessionId { get; }

		public Session(string sessionId)
		{
			SessionId = sessionId;
		}
	}
}
