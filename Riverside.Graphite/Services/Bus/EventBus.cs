using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riverside.Graphite.Services.Bus
{
	public class EventBus
	{
		// to be used when IReciptant it's in effect, and good for function calling
		private EventBus() { }
		private static EventBus _Instance;
		public static EventBus Instance
		{
			get
			{
				if (_Instance == null)
					_Instance = new EventBus();
				return _Instance;
			}
		}

		private Dictionary<Type, List<Action<string, object>>> _Registrants
			= new Dictionary<Type, List<Action<string, object>>>();
		public void Register<T>(Action<string, object> action) where T : class
		{
			List<Action<string, object>> list;
			if (!_Registrants.ContainsKey(typeof(T)))
				_Registrants[typeof(T)] = new List<Action<string, object>>();
			list = _Registrants[typeof(T)];
			list.Add(action);
		}

		public void Unregister<T>() where T : class
		{
			if (_Registrants.ContainsKey(typeof(T)))
				_Registrants.Remove(typeof(T));
		}

		public void Send(string message, object payload = null)
		{
			foreach (var item in _Registrants.SelectMany(x => x.Value))
				item(message, payload);
		}
	}
}
