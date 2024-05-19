﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace FireBrowserWinUi3MultiCore
{
    public class AuthService
    {
        public static readonly string UserDataFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FireBrowserUserCore", "UsrCore.json");
        public static List<User> users = LoadUsersFromJson();

        private static List<User> LoadUsersFromJson()
        {
            try
            {
                return File.Exists(UserDataFilePath) ? JsonSerializer.Deserialize<List<User>>(File.ReadAllText(UserDataFilePath)) ?? new List<User>() : new List<User>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading user data: {ex.Message}");
                return new List<User>();
            }
        }

        public static User CurrentUser { get; private set; }

        public static bool IsUserAuthenticated => CurrentUser != null;

        public static bool SwitchUser(string username) => (CurrentUser = users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase))) != null;

        public static bool Authenticate(string username) => SwitchUser(username);

        public static void AddUser(User newUser)
        {
            if (!users.Any(u => u.Username.Equals(newUser.Username, StringComparison.OrdinalIgnoreCase)))
            {
                users.Add(newUser);
                SaveUsers();
            }
        }

        private static void SaveUsers()
        {
            try
            {
                File.WriteAllText(UserDataFilePath, JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving user data: {ex.Message}");
            }
        }

        public static List<string> GetAllUsernames() => users.Select(u => u.Username).ToList();

        public static void Logout() => CurrentUser = null;

        public static bool ChangeUsername(string oldUsername, string newUsername)
        {
            User userToChange = users.FirstOrDefault(u => u.Username.Equals(oldUsername, StringComparison.OrdinalIgnoreCase));
            if (userToChange == null || users.Any(u => u.Username.Equals(newUsername, StringComparison.OrdinalIgnoreCase)))
                return false;

            userToChange.Username = newUsername;

            SaveUsers();

            if (CurrentUser != null && CurrentUser.Username.Equals(oldUsername, StringComparison.OrdinalIgnoreCase))
                CurrentUser = userToChange;

            return true;
        }
    }
}