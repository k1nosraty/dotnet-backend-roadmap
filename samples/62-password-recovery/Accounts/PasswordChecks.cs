using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
namespace SupportDesk.Accounts;

public static class PasswordChecks
{
    public static async Task RunAsync(IServiceProvider services)
    {
        var users = services.GetRequiredService<UserManager<IdentityUser>>();
        var hasher = services.GetRequiredService<IPasswordHasher<IdentityUser>>();
        var user = new IdentityUser { UserName = "reset-" + Guid.NewGuid().ToString("N") + "@example.test" };
        user.Email = user.UserName;
        string original = AccountChecks.Password(), replacement = AccountChecks.Password();
        AccountChecks.Check((await users.CreateAsync(user, original)).Succeeded, "Reset account create.");
        string first = hasher.HashPassword(user, original), second = hasher.HashPassword(user, original);
        AccountChecks.Check(first != second, "Independent salts produce distinct hashes.");
        AccountChecks.Check(hasher.VerifyHashedPassword(user, first, original) != PasswordVerificationResult.Failed, "Correct hash verification.");
        AccountChecks.Check(hasher.VerifyHashedPassword(user, first, replacement) == PasswordVerificationResult.Failed, "Wrong password fails.");
        // Deliberately obsolete cost for the rehash test only. Never register this hasher for an account service.
        var legacy = new PasswordHasher<IdentityUser>(Options.Create(new PasswordHasherOptions { IterationCount = 1000 }));
        AccountChecks.Check(hasher.VerifyHashedPassword(user, legacy.HashPassword(user, original), original)
            == PasswordVerificationResult.SuccessRehashNeeded, "Upgrade legacy hash cost.");
        string token = await users.GeneratePasswordResetTokenAsync(user);
        var other = new IdentityUser { UserName = "other-" + user.UserName, Email = "other-" + user.Email };
        AccountChecks.Check((await users.CreateAsync(other, original)).Succeeded, "Other account create.");
        AccountChecks.Check(!(await users.ResetPasswordAsync(other, token, replacement)).Succeeded, "Token bound to account.");
        AccountChecks.Check(!(await users.ResetPasswordAsync(user, "tampered", replacement)).Succeeded, "Tampered reset fails.");
        string oldStamp = (await users.GetSecurityStampAsync(user))!;
        AccountChecks.Check((await users.ResetPasswordAsync(user, token, replacement)).Succeeded, "Valid reset succeeds.");
        AccountChecks.Check(await users.CheckPasswordAsync(user, replacement) && !await users.CheckPasswordAsync(user, original), "New password only.");
        AccountChecks.Check(oldStamp != await users.GetSecurityStampAsync(user), "Reset changes security stamp.");
        AccountChecks.Check(!(await users.ResetPasswordAsync(user, token, original)).Succeeded, "Reset token replay fails.");
        string pending = await users.GeneratePasswordResetTokenAsync(user);
        await users.UpdateSecurityStampAsync(user);
        AccountChecks.Check(!(await users.ResetPasswordAsync(user, pending, original)).Succeeded, "Stamp update invalidates pending reset.");
        Console.WriteLine("PASS: salted password hashes, rehash, reset binding, replay and security stamp");
    }
}
