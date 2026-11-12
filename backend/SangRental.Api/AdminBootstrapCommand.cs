using SangRental.Infrastructure.Security;

namespace SangRental.Api;

internal static class AdminBootstrapCommand
{
    public static async Task RunAsync(IServiceProvider services, bool testDatabase)
    {
        if (Console.IsInputRedirected) throw new InvalidOperationException("Use an interactive terminal for administrator provisioning.");
        Console.Write("Tên đăng nhập Admin nội bộ: ");
        var username = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(username)) throw new InvalidOperationException("Username is required.");
        Console.Write("Mật khẩu (tối thiểu 12 ký tự, hoa/thường/số/ký hiệu): ");
        var password = ReadPassword();
        Console.Write("Nhập lại mật khẩu: ");
        if (password != ReadPassword()) throw new InvalidOperationException("Passwords do not match.");
        using var scope = services.CreateScope();
        var provisioning = scope.ServiceProvider.GetRequiredService<AdminProvisioningService>();
        if (testDatabase) await provisioning.ProvisionLocalTestAdminAsync(username, password);
        else await provisioning.ProvisionFirstAdminAsync(username, password);
        Console.WriteLine(testDatabase ? "Đã tạo Admin riêng trong SangRentalTestDb."
            : "Đã tạo Admin đầu tiên. Bootstrap sẽ từ chối nếu đã có Admin.");
    }

    private static string ReadPassword()
    {
        var characters = new List<char>();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); return new string(characters.ToArray()); }
            if (key.Key == ConsoleKey.Backspace) { if (characters.Count > 0) characters.RemoveAt(characters.Count - 1); }
            else if (!char.IsControl(key.KeyChar) && characters.Count < 256) characters.Add(key.KeyChar);
        }
    }
}
