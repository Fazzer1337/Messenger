using messenger.Models;
using System.IO;
using System.Text.Json;

namespace messenger.Utils
{
    public static class UserProfileManager
    {
        private const string ProfileFile = "user.json";

        public static void SaveProfile(UserProfile profile)
        {
            var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ProfileFile, json);
        }

        public static UserProfile? LoadProfile()
        {
            if (!File.Exists(ProfileFile))
                return null;
            var json = File.ReadAllText(ProfileFile);
            return JsonSerializer.Deserialize<UserProfile>(json);
        }
    }
}
