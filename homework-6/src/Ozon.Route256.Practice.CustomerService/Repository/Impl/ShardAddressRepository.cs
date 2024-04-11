﻿using Dapper;
using Ozon.Route256.Practice.CustomerService.Dal.Common.Shard;
using Ozon.Route256.Practice.CustomerService.Repository.Dto;

namespace Ozon.Route256.Practice.CustomerService.Repository.Impl;

public class ShardAddressRepository: BaseShardRepository, IAddressRepository
{
    private const string Fields = "id, customer_id as customerId, is_default as isDefault, region, city, street, building, apartment, latitude, longitude";
    private const string FieldsForInsert = "customer_id, is_default, region, city, street, building, apartment, latitude, longitude";
    private const string Table = $"{Shards.BucketPlaceholder}.addresses";
    
    public ShardAddressRepository(
        IShardConnectionFactory connectionFactory,
        IShardingRule<long> longShardingRule,
        IShardingRule<string> stringShardingRule): base(connectionFactory, longShardingRule, stringShardingRule)
    {
    }

    public async Task<AddressDto?> FindDefaultForCustomer(
        int customerId,
        CancellationToken token)
    {
        const string sql = @$"
            select {Fields}
            from {Table}
            where customer_id = :customerId
                and is_default = true;
        ";

        await using var connection = GetConnectionByShardKey(customerId);
        var result = await connection.QueryFirstOrDefaultAsync<AddressDto>(sql, new { customerId});
        return result;
    }

    public async Task<AddressDto[]> FindAllForCustomer(
        int customerId,
        CancellationToken token)
    {
        const string sql = @$"
            select {Fields}
            from {Table}
            where customer_id = :customerId;
        ";

        await using var connection = GetConnectionByShardKey(customerId);
        var result = await connection.QueryAsync<AddressDto>(sql, new { customerId});
        return result.ToArray();
    }

    public async Task<AddressDto[]> GetAll(
        CancellationToken token)
    {
        var result = new List<AddressDto>();

        foreach (var bucketId in _connectionFactory.GetAllBuckets())
        {
            const string sql = @$"
                select {Fields}
                from {Table};
            ";
        
            await using var connection = await GetConnectionByBucket(bucketId, token);
            var customers = await connection.QueryAsync<AddressDto>(sql);
            result.AddRange(customers);
        }

        return result.ToArray();
    }

    public async Task Create(
        int customerId, 
        AddressDto[] addresses,
        CancellationToken token) 
    {
        if (addresses.Any(x => x.CustomerId != customerId))
        {
            throw new ArgumentException("All addresses should have the same customer");
        }
        
        const string sql = @$"
            insert into {Table} ({FieldsForInsert})
            select {FieldsForInsert}
            from unnest(:Models);
        ";
        
        await using var connection = GetConnectionByShardKey(customerId);
        var param = new DynamicParameters();
        param.Add("Models", addresses);

        await connection.ExecuteAsync(sql, param);
    }
}