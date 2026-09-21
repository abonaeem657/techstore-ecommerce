using Microsoft.AspNetCore.Identity;
using techstore.Models;
namespace techstore.Services;

// Single-process file storage, preserving accounts without a database migration.
public sealed class UserFileStore
{
    private const string HashFormat = "passwordhasher-v1";
    private static readonly object FileLock = new();
    private readonly string _path;
    private readonly IPasswordHasher<Users> _hasher;
    public UserFileStore(IWebHostEnvironment environment, IConfiguration configuration, IPasswordHasher<Users> hasher)
    {
        _path = Path.GetFullPath(configuration["Store:UsersFile"]
            ?? Path.Combine(environment.ContentRootPath, "App_Data", "users.txt"));
        _hasher = hasher;
    }
    public bool Create(Users user)
    {
        lock (FileLock)
        {
            var users = ReadAndUpgrade();
            if (users.Any(u => string.Equals(u.Email, user.Email.Trim(), StringComparison.OrdinalIgnoreCase)))
                return false;
            var stored = new Users
            {
                Id = users.Count == 0 ? 1 : checked(users.Max(u => u.Id) + 1),
                Name = user.Name.Trim(), Email = user.Email.Trim()
            };
            stored.PasswordHash = _hasher.HashPassword(stored, user.Password);
            users.Add(stored);
            Save(users);
            return true;
        }
    }
    public Users? Verify(string email, string password)
    {
        lock (FileLock)
        {
            var users = ReadAndUpgrade();
            var user = users.FirstOrDefault(u => string.Equals(u.Email, email.Trim(), StringComparison.OrdinalIgnoreCase));
            if (user == null) return null;
            PasswordVerificationResult result;
            try { result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password); }
            catch (FormatException) { return null; }
            if (result == PasswordVerificationResult.Failed) return null;
            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _hasher.HashPassword(user, password);
                Save(users);
            }
            return user;
        }
    }
    private List<Users> ReadAndUpgrade()
    {
        var users = new List<Users>();
        if (!File.Exists(_path)) return users;
        var upgrade = false;
        foreach (var line in File.ReadLines(_path))
        {
            var fields = line.Split(';');
            if ((fields.Length != 4 && fields.Length != 5)
                || !int.TryParse(fields[0], out var id)
                || (fields.Length == 5 && fields[4] != HashFormat))
                throw new InvalidDataException("The account file contains an unsupported record.");
            var user = new Users { Id = id, Name = fields[1], Email = fields[2] };
            if (fields.Length == 4)
            {
                // An explicit legacy format avoids mistaking a plaintext password for a hash.
                user.PasswordHash = _hasher.HashPassword(user, fields[3]);
                upgrade = true;
            }
            else user.PasswordHash = fields[3];
            users.Add(user);
        }
        // Validate every record first. A malformed file is never partially rewritten.
        if (upgrade) Save(users);
        return users;
    }
    private void Save(List<Users> users)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var temporaryPath = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllLines(temporaryPath, users.Select(u =>
                $"{u.Id};{u.Name};{u.Email};{u.PasswordHash};{HashFormat}"));
            File.Move(temporaryPath, _path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }
}

