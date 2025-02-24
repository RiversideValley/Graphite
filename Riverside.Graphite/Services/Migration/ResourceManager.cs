using System;
using System.Collections.Generic;
using System.Threading;
using Windows.Networking.Sockets;
using System.Net.Sockets;

namespace Riverside.Graphite.Services.Migration
{
	public class ResourceManager : IDisposable
	{
		private readonly List<IDisposable> _resources;
		private readonly List<CancellationTokenSource> _cancellationSources;
		private readonly object _lock = new object();
		private bool _isDisposed;

		public ResourceManager()
		{
			_resources = new List<IDisposable>();
			_cancellationSources = new List<CancellationTokenSource>();
		}

		public void AddResource(IDisposable resource)
		{
			if (_isDisposed)
				throw new ObjectDisposedException(nameof(ResourceManager));

			lock (_lock)
			{
				_resources.Add(resource);
			}
		}

		public void AddCancellationSource(CancellationTokenSource cts)
		{
			if (_isDisposed)
				throw new ObjectDisposedException(nameof(ResourceManager));

			lock (_lock)
			{
				_cancellationSources.Add(cts);
			}
		}

		public void RemoveResource(IDisposable resource)
		{
			if (_isDisposed)
				throw new ObjectDisposedException(nameof(ResourceManager));

			lock (_lock)
			{
				_resources.Remove(resource);
			}
		}

		public void SafeCleanup(IDisposable resource)
		{
			try
			{
				if (resource is StreamSocket streamSocket)
				{
					streamSocket.Dispose();
				}
				else if (resource is StreamSocketListener listener)
				{
					listener.Dispose();
				}
				else if (resource is UdpClient udpClient)
				{
					udpClient.Close();
					udpClient.Dispose();
				}
				else
				{
					resource?.Dispose();
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Error during resource cleanup: {ex.Message}");
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				if (disposing)
				{
					lock (_lock)
					{
						foreach (var cts in _cancellationSources)
						{
							try
							{
								cts.Cancel();
								cts.Dispose();
							}
							catch { }
						}
						_cancellationSources.Clear();

						foreach (var resource in _resources)
						{
							SafeCleanup(resource);
						}
						_resources.Clear();
					}
				}

				_isDisposed = true;
			}
		}
	}
}

