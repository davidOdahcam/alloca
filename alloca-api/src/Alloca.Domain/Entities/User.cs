using Alloca.Domain.Common;
using Alloca.Domain.Enums;

namespace Alloca.Domain.Entities;

public class User : Entity
{
    public string Email { get; private set; } = default!;
    public string FullName { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; } = true;

    private readonly List<PavilionManager> _managedPavilions = [];
    public IReadOnlyCollection<PavilionManager> ManagedPavilions => _managedPavilions.AsReadOnly();

    private User() { }

    public User(string email, string fullName, string passwordHash, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new DomainException("Email is required.");
        if (string.IsNullOrWhiteSpace(fullName)) throw new DomainException("Name is required.");
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new DomainException("Password is required.");

        Email = email.Trim().ToLowerInvariant();
        FullName = fullName.Trim();
        PasswordHash = passwordHash;
        Role = role;
    }

    public void UpdatePasswordHash(string newHash)
    {
        if (string.IsNullOrWhiteSpace(newHash)) throw new DomainException("Password hash invalid.");
        PasswordHash = newHash;
        Touch();
    }

    public void Deactivate() { IsActive = false; Touch(); }
    public void Activate() { IsActive = true; Touch(); }
    public void PromoteTo(UserRole role) { Role = role; Touch(); }
}
