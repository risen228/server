namespace Argon.Grains;

using Shared.Servers;

public class ServerInviteGrain(ILogger<IServerInvitesGrain> logger,
    [PersistentState("server-invites-store", IServerInvitesGrain.StorageId)]
    IPersistentState<ServerInvitesStorage> state) : Grain, IServerInvitesGrain
{
    private IGrainReminder? _reminder;

    public async override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await state.ReadStateAsync();
        if (_reminder is not null)
            return;
        _reminder = await this.RegisterOrUpdateReminder($"server-invites-{this.GetPrimaryKey()}", 
            TimeSpan.FromDays(1), 
            TimeSpan.FromDays(30));
    }

    public async override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        await state.WriteStateAsync();
        if (_reminder is not null)
            await this.UnregisterReminder(_reminder);
    }

    public async Task<InviteCode> CreateInviteLinkAsync(Guid issuer, TimeSpan expiration)
    {
        var inviteCode  = GenerateInviteCode();
        var inviteGrain = base.GrainFactory.GetGrain<IInviteGrain>(inviteCode);
        if (await inviteGrain.HasCreatedAsync())
            throw new InvalidOperationException($"InviteCode already created");
        var code = await inviteGrain.EnsureAsync(this.GetPrimaryKey(), issuer, expiration);
        state.State.Entities.Add(inviteCode, new InviteCodeEntity(code, this.GetPrimaryKey(), issuer, DateTime.UtcNow + expiration, 0));
        await state.WriteStateAsync();
        return code;
    }

    public async Task InviteUsed(Guid userId, InviteCode code)
    {
        if (!state.State.Entities.TryGetValue(code.inviteCode, out var result))
            return;
        state.State.Entities[code.inviteCode] = result with { used = result.used++ };
    }

    public async Task<List<InviteCodeEntity>> GetInviteCodes()
        => state.State.Entities.Values.ToList();


    private unsafe static string GenerateInviteCode(int length = 6)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        Span<byte>   bytes = stackalloc byte[length];
        using (var rng = RandomNumberGenerator.Create())
            rng.GetBytes(bytes);

        var result = stackalloc char[length];
        for (var i = 0; i < length; i++)
            result[i] = chars[bytes[i] % chars.Length];

        return new string(result);
    }

    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        foreach (var (code, _) in state.State.Entities.Where(x => x.Value.HasExpired()))
        {
            logger.LogInformation("Remove expired invite code '{code}' from '{serverId}' server", code, this.GetPrimaryKey());
            state.State.Entities.Remove(code);
            await GrainFactory.GetGrain<IInviteGrain>(code).DropInviteCodeAsync();
        }
    }
}