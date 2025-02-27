using Microsoft.EntityFrameworkCore;
using Stl.Fusion.EntityFramework;
using Stl.Fusion.Tests.Model;
using Stl.Reflection;
using Stl.RegisterAttributes;

namespace Stl.Fusion.Tests.Services;

public interface IUserService : IComputeService
{
    [DataContract]
    public record AddCommand(
        [property: DataMember] User User,
        [property: DataMember] bool OrUpdate = false
        ) : ICommand<Unit>
    {
        public AddCommand() : this(null!, false) { }
    }

    [DataContract]
    public record UpdateCommand(
        [property: DataMember] User User
        ) : ICommand<Unit>
    {
        public UpdateCommand() : this(default(User)!) { }
    }

    [DataContract]
    public record DeleteCommand(
        [property: DataMember] User User
        ) : ICommand<bool>
    {
        public DeleteCommand() : this(default(User)!) { }
    }

    [CommandHandler]
    Task Create(AddCommand command, CancellationToken cancellationToken = default);
    [CommandHandler]
    Task Update(UpdateCommand command, CancellationToken cancellationToken = default);
    [CommandHandler]
    Task<bool> Delete(DeleteCommand command, CancellationToken cancellationToken = default);

    [ComputeMethod(MinCacheDuration = 60)]
    Task<User?> Get(long userId, CancellationToken cancellationToken = default);
    [ComputeMethod(MinCacheDuration = 60)]
    Task<long> Count(CancellationToken cancellationToken = default);

    Task UpdateDirectly(UpdateCommand command, CancellationToken cancellationToken = default);
    Task Invalidate();
}

[RegisterComputeService(typeof(IUserService), Scope = ServiceScope.Services)] // Fusion version
[RegisterService] // "No Fusion" version
public class UserService : DbServiceBase<TestDbContext>, IUserService
{
    public bool IsProxy { get; }

    public UserService(IServiceProvider services) : base(services)
    {
        var type = GetType();
        IsProxy = type != type.NonProxyType();
    }

    public virtual async Task Create(IUserService.AddCommand command, CancellationToken cancellationToken = default)
    {
        var (user, orUpdate) = command;
        var existingUser = (User?) null;
        var context = CommandContext.GetCurrent();
        if (Computed.IsInvalidating()) {
            _ = Get(user.Id, default).AssertCompleted();
            existingUser = context.Operation().Items.Get<User>();
            if (existingUser == null)
                _ = Count(default).AssertCompleted();
            return;
        }

        var dbContext = await CreateCommandDbContext(cancellationToken).ConfigureAwait(false);
        await using var _1 = dbContext.ConfigureAwait(false);
        dbContext.DisableChangeTracking();

        var userId = user.Id;
        if (orUpdate) {
            existingUser = await dbContext.Users.FindAsync(DbKey.Compose(userId), cancellationToken);
            context.Operation().Items.Set(existingUser);
            if (existingUser != null!)
                dbContext.Users.Update(user);
        }
        if (existingUser == null)
            dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task Update(IUserService.UpdateCommand command, CancellationToken cancellationToken = default)
    {
        var user = command.User;
        if (Computed.IsInvalidating()) {
            _ = Get(user.Id, default).AssertCompleted();
            return;
        }

        var dbContext = await CreateCommandDbContext(cancellationToken).ConfigureAwait(false);
        await using var _1 = dbContext.ConfigureAwait(false);

        dbContext.Users.Update(user);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateDirectly(IUserService.UpdateCommand command, CancellationToken cancellationToken = default)
    {
        var user = command.User;
        await using (var dbContext = CreateDbContext(true)) {
            dbContext.Users.Update(user);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        if (Computed.IsInvalidating())
            _ = Get(user.Id, default).AssertCompleted();
    }

    public virtual async Task<bool> Delete(IUserService.DeleteCommand command, CancellationToken cancellationToken = default)
    {
        var user = command.User;
        var context = CommandContext.GetCurrent();
        if (Computed.IsInvalidating()) {
            var success = context.Operation().Items.GetOrDefault<bool>();
            if (success) {
                _ = Get(user.Id, default).AssertCompleted();
                _ = Count(default).AssertCompleted();
            }
            return false;
        }

        var dbContext = await CreateCommandDbContext(cancellationToken).ConfigureAwait(false);
        await using var _1 = dbContext.ConfigureAwait(false);

        dbContext.Users.Remove(user);
        try {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            context.Operation().Items.Set(true);
            return true;
        }
        catch (DbUpdateConcurrencyException) {
            return false;
        }
    }

    public virtual async Task<User?> Get(long userId, CancellationToken cancellationToken = default)
    {
        // Debug.WriteLine($"Get {userId}");
        await Everything().ConfigureAwait(false);

        var dbContext = CreateDbContext();
        await using var _ = dbContext.ConfigureAwait(false);

        var user = await dbContext.Users
            .FindAsync(new[] {(object) userId}, cancellationToken)
            .ConfigureAwait(false);
        return user;
    }

    public virtual async Task<long> Count(CancellationToken cancellationToken = default)
    {
        await Everything().ConfigureAwait(false);

        var dbContext = CreateDbContext();
        await using var _ = dbContext.ConfigureAwait(false);

        var count = await dbContext.Users.AsQueryable()
            .LongCountAsync(cancellationToken)
            .ConfigureAwait(false);
        // _log.LogDebug($"Users.Count query: {count}");
        return count;
    }

    public virtual Task Invalidate()
    {
        if (!IsProxy)
            return Task.CompletedTask;

        using (Computed.Invalidate())
            _ = Everything().AssertCompleted();

        return Task.CompletedTask;
    }

    // Protected & private methods

    [ComputeMethod]
    protected virtual Task<Unit> Everything() => TaskExt.UnitTask;

    private new Task<TestDbContext> CreateCommandDbContext(CancellationToken cancellationToken = default)
    {
        if (IsProxy)
            return base.CreateCommandDbContext(cancellationToken);
        return Task.FromResult(CreateDbContext().ReadWrite());
    }
}
