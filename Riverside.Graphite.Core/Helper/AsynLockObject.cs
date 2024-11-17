using System;
using System.Threading;
using System.Threading.Tasks;

namespace Riverside.Graphite.Core.Helper
{
	public class AsyncLockObject
	{
		private readonly SemaphoreSlim _semaphore = new(1, 1);

		public async Task<IDisposable> LockAsync()
		{
			await _semaphore.WaitAsync();
			return new Releaser(_semaphore);
		}

		private class Releaser : IDisposable
		{
			private readonly SemaphoreSlim _semaphore;

			public Releaser(SemaphoreSlim semaphore)
			{
				_semaphore = semaphore;
			}

			public void Dispose()
			{
				_ = _semaphore.Release();
			}
		}
	}
}
