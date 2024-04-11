using Ozon.Route256.Practice.CustomerService.Repository.Dto;

namespace Ozon.Route256.Practice.CustomerService.Repository;

public interface ICustomerRepository
{
    Task<CustomerDto[]> GetAll(CancellationToken token);

    Task<CustomerDto?> Find(int id, CancellationToken token);
}