namespace SampleApp.Rule9;

public sealed record Customer(Guid Id, string Name);

public sealed class CustomerRepository
{
    private readonly Dictionary<Guid, Customer> _customers = [];

    public Task<Customer?> GetAsync(Guid id) => Task.FromResult(_customers.GetValueOrDefault(id));

    public Task SaveAsync(Customer customer)
    {
        _customers[customer.Id] = customer;
        return Task.CompletedTask;
    }
}

// ❌ PT0009A: one interface, one implementation, no second one in sight
public interface ICustomerService
{
    Task<Customer?> GetAsync(Guid id);
    Task SaveAsync(Customer customer);
}

public sealed class CustomerService(CustomerRepository repository) : ICustomerService
{
    // ❌ PT0009B: a layer that adds nothing but a hop
    public Task<Customer?> GetAsync(Guid id) => repository.GetAsync(id);

    // ❌ PT0009B: same thing, with async/await ceremony
    public async Task SaveAsync(Customer customer)
    {
        await repository.SaveAsync(customer);
    }
}

// ✅ an interface with two real implementations earns its keep
public interface IPaymentGateway
{
    Task<bool> ChargeAsync(Guid customerId, decimal amount);
}

public sealed class CardGateway : IPaymentGateway
{
    public Task<bool> ChargeAsync(Guid customerId, decimal amount) => Task.FromResult(amount > 0);
}

public sealed class InvoiceGateway : IPaymentGateway
{
    public Task<bool> ChargeAsync(Guid customerId, decimal amount) => Task.FromResult(amount < 50_000);
}

// ✅ a wrapper that adds behavior (validation) isn't a pass-through
public sealed class ValidatingCustomerService(CustomerRepository repository)
{
    public Task SaveAsync(Customer customer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customer.Name);
        return repository.SaveAsync(customer);
    }
}
