using System.Collections.Generic;

namespace Riverside.Graphite.IdentityClient.Helpers;

public class VerificationWindow
{
	private readonly int _previous;
	private readonly int _future;

	/// <param name="previous">The number of previous frames to accept</param>
	/// <param name="future">The number of future frames to accept</param>
	public VerificationWindow(int previous = 0, int future = 0)
	{
		_previous = previous;
		_future = future;
	}

	/// <param name="initialFrame">The initial frame to validate</param>
	/// <returns>Enumerable of all possible frames that need to be validated</returns>
	public IEnumerable<long> ValidationCandidates(long initialFrame)
	{
		yield return initialFrame;
		for (var i = 1; i <= _previous; i++)
		{
			var val = initialFrame - i;
			if (val < 0)
			{
				break;
			}
			yield return val;
		}

		for (var i = 1; i <= _future; i++)
		{
			yield return initialFrame + i;
		}
	}

	/// <summary>
	/// The verification window that accommodates network delay that is recommended in the RFC
	/// </summary>
	public static readonly VerificationWindow RfcSpecifiedNetworkDelay =
		new(previous: 1, future: 1);
}