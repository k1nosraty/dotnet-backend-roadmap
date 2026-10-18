using Microsoft.AspNetCore.Identity;
namespace SupportDesk.Accounts;

public static class AccountChecks
{
    public static void Check(bool value, string scenario)
    {
        if (!value) throw new InvalidOperationException(scenario);
    }
    public static string Password() => "Lab-" + Guid.NewGuid().ToString("N") + "!7aA";
    public static async Task RunAsync(IServiceProvider services)
    {
        var users = services.GetRequiredService<UserManager<IdentityUser>>();
        var signIn = services.GetRequiredService<SignInManager<IdentityUser>>();
        string email = "user-" + Guid.NewGuid().ToString("N") + "@example.test";
        string password = Password();
        var user = new IdentityUser { UserName = email, Email = email };
        Check((await users.CreateAsync(user, password)).Succeeded, "Account create.");
        Check(!await signIn.CanSignInAsync(user), "Unconfirmed email cannot sign in.");
        var duplicate = new IdentityUser { UserName = email.ToUpperInvariant(), Email = email.ToUpperInvariant() };
        Check(!(await users.CreateAsync(duplicate, password)).Succeeded, "Normalized duplicate rejected.");
        var weak = new IdentityUser { UserName = "weak-" + email, Email = "weak-" + email };
        Check(!(await users.CreateAsync(weak, "short")).Succeeded, "Weak password rejected.");
        Check(await users.FindByNameAsync(weak.UserName) is null, "Invalid account was not inserted.");
        string confirmation = await users.GenerateEmailConfirmationTokenAsync(user);
        Check(!(await users.ConfirmEmailAsync(user, "invalid-token")).Succeeded, "Tampered confirmation rejected.");
        Check((await users.ConfirmEmailAsync(user, confirmation)).Succeeded, "Confirmation succeeds.");
        Check(await signIn.CanSignInAsync(user), "Confirmed account may proceed.");
        for (int i = 0; i < 5; i++)
            Check(!(await signIn.CheckPasswordSignInAsync(user, Password(), lockoutOnFailure: true)).Succeeded, "Bad password rejected.");
        Check(await users.IsLockedOutAsync(user), "Repeated failure locks account.");
        Check((await signIn.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true)).IsLockedOut,
            "Correct password cannot bypass active lockout.");
        Check(user.PasswordHash is not null && user.PasswordHash != password && !user.PasswordHash.Contains(password, StringComparison.Ordinal),
            "No plaintext password in hash field.");
        Console.WriteLine("PASS: Identity account creation, normalization, confirmation and lockout");
    }
}
