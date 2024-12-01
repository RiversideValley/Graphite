
using FireCore.Data.Database;
using FireCore.Data.Models;
using Microsoft.Data.Sqlite;
using System.Linq;

using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace FireCore.Services
{
    public interface IHubCommander
    {
        Task<bool> WriteAsync(SessionDbEntity entity);
        Task<string> GetSessionIdAsync(Tuple<string, string> key);
        Task<bool> DeleteAsync(Tuple<string, string> key);

        Task<bool> DeleteAllSessionsByUser(string connectionId); 
        ConcurrentDictionary<Tuple<string, string>, string> ActiveGroups { get; }
        List<User>? MsalCurrentUsers { get; set; }

    }
    public sealed class HubCommander : IHubCommander
    {
        private readonly ConcurrentDictionary<Tuple<string, string>, string>? _inMemoryStorage;
        public ConcurrentDictionary<Tuple<string, string>, string>? ActiveGroups { get; }

        public List<User>? MsalCurrentUsers { get; set; }

        public HubCommander()
        {
            ActiveGroups = _inMemoryStorage = new();
        }

        public HubCommander(List<User> msalCurrentUsers)
        {
            MsalCurrentUsers = msalCurrentUsers;
        }

        public async Task<bool> DeleteAllSessionsByUser(string connectionId) {

            var success = false;

            try
            {
                var filterList = _inMemoryStorage;
                 
                foreach (Tuple<string, string> key in filterList!.Keys)
                {
                    var item = key;
                    if (item.Item1 == connectionId)
                    {
                        success = await DeleteAsync(item);
                    }
                        
                }
                     
            }
            catch (Exception)
            {

                throw;
            }
            return (success); 
        }
        public Task<bool> DeleteAsync(Tuple<string, string> key)
        {
            try
            {
                bool? result = _inMemoryStorage?.Remove(new Tuple<string, string>(key.Item1, key.Item2), out _);
                return Task.FromResult((bool)result!);
            }
            catch (Exception)
            {
                throw;
            }

        }

        public Task<string> GetSessionIdAsync(Tuple<string, string> key)
        {
            try
            {
                string answer = string.Empty;
                var result = _inMemoryStorage?.TryGetValue(new Tuple<string, string>(key.Item1, key.Item2), out answer!);
                return Task.FromResult(answer);
            }
            catch (Exception)
            {
                throw;
            }

        }

        public Task<bool> WriteAsync(SessionDbEntity entity)
        {
            try
            {

                var result = _inMemoryStorage?.TryAdd(new Tuple<string, string>(entity.ConnectionId, entity.PartnerName), entity.SessionId);
                return Task.FromResult((bool)result!);

            }
            catch (Exception ex)
            {
                Console.Write(ex?.Message);
                throw;
            }
        }

    }
}