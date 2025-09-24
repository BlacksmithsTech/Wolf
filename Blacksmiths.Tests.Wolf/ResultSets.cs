using Blacksmiths.Utils.Wolf;
using Blacksmiths.Utils.Wolf.Attribution;
using System;
using System.Data.Common;

namespace Blacksmiths.Tests.Wolf
{
    [Collection(Collections.Constants.WolfTestDatabase)]
    public class ResultSets
    {
        [Procedure(Name = "[dbo].[spStoredProcedure]")]
        private class spStoredProcedure_MissingParams : StoredProcedure
        {
        }

        [Procedure(Name = "[dbo].[spStoredProcedure]")]
        private class spStoredProcedure_Untargeted : StoredProcedure
        {
            public string ValueInStr { get; set; }
            public DateTimeOffset? ValueInDt { get; set; }
            public bool? ValueInBit { get; set; }
            public string OptionalValueStr { get; set; }

            [Parameter(Direction = System.Data.ParameterDirection.InputOutput)]
            public string ValueOutStr { get; set; }

            [Parameter(Direction = System.Data.ParameterDirection.InputOutput)]
            public DateTimeOffset? ValueOutDt { get; set; }
        }

        [Procedure(Name = "[dbo].[spStoredProcedure]")]
        private class spStoredProcedure_Targeted : StoredProcedure, IDataRequestCommand<spStoredProcedureResult>
        {
            public string ValueInStr { get; set; }
            public DateTimeOffset? ValueInDt { get; set; }
            public bool? ValueInBit { get; set; }
            public string OptionalValueStr { get; set; }

            [Parameter(Direction = System.Data.ParameterDirection.InputOutput)]
            public string ValueOutStr { get; set; }

            [Parameter(Direction = System.Data.ParameterDirection.InputOutput)]
            public DateTimeOffset? ValueOutDt { get; set; }
        }

        [Procedure(Name = "[dbo].[spMultiResult]")]
        private class spMultiResult : StoredProcedure
        {
        }

        private class spStoredProcedureResult
        {
            public string ValueInStr { get; set; }
            public DateTimeOffset? ValueInDt { get; set; }
            public bool? ValueInBit { get; set; }
            public string OptionalValueStr { get; set; }
            public string FixedValue { get; set; }
        }

        private class spMultiResult1
        {
            public string Column1 { get; set; }
            public int Column2 { get; set; }
        }

        private class spMultiResult2
        {
            public string Column1 { get; set; }
            public DateTimeOffset Column2 { get; set; }
            public int Column3 { get; set; }
        }

        [Procedure(Name = "[dbo].[spGetCustomers]")]
        private class spGetCustomers : StoredProcedure, IDataRequestCommand<Customer>
        {
        }

        [Procedure(Name = "[dbo].[spGetOrders]")]
        private class spGetOrders: StoredProcedure, IDataRequestCommand<Order>
        {
        }

        public class Customer
        {
            public int CustomerID { get; set; }
            public string CustomerName { get; set; }
            public string Email { get; set; }

            [Relation(nameof(CustomerID))]
            public Order[] Orders { get; set; }
        }

        public class Order
        {
            public int OrderID { get; set; }
            public int CustomerID { get; set; }
            public DateTime OrderDate { get; set; }
            public decimal OrderAmount { get; set; }
        }

        public class CustomerColumnMapped
        {
            public int CustomerID { get; set; }
            public string CustomerName { get; set; }
            public string Email { get; set; }

            [Relation(nameof(CustomerID))]
            public OrderColumnMapped[] Orders { get; set; }
        }

        public class OrderColumnMapped
        {
            public int OrderID { get; set; }
            public int CustomerID { get; set; }
            public DateTime OrderDate { get; set; }

            [Column("OrderAmount")]
            public decimal Amount { get; set; }
        }

        private readonly IDataConnection connection;
        public ResultSets(Fixtures.WolfTestDatabaseSqlServerConnection wolfTestDatabaseSqlServerConnection)
        {
            this.connection = wolfTestDatabaseSqlServerConnection.Connection;
        }

        [Fact]
        public async Task Execute_MissingParams()
        {
            await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(async () => {
                await this.connection.NewRequest()
                    .Add(new spStoredProcedure_MissingParams())
                    .ExecuteAsync();
            });
        }

        [Fact]
        public async Task Execute_StoredProcedure()
        {
            var now = DateTimeOffset.UtcNow;
            var sproc = new spStoredProcedure_Untargeted() { ValueInStr = "Hello world", ValueInDt = now };
            var resultCode = await this.connection.NewRequest()
                .Add(sproc)
                .ExecuteAsync();

            Assert.Equal(99, resultCode);
            Assert.Equal("Hello world output", sproc.ValueOutStr);
            Assert.Equal(now.AddDays(1), sproc.ValueOutDt);
        }

        [Fact]
        public async Task Query_StoredProcedure_Untargeted()
        {
            var now = DateTimeOffset.UtcNow;
            var sproc = new spStoredProcedure_Untargeted() { ValueInStr = "Hello world", ValueInDt = now, ValueInBit = true, OptionalValueStr = "Test" };
            var rows = await this.connection.NewRequest()
                .Add(sproc)
                .QueryAsync<spStoredProcedureResult>();

            Assert.Equal(99, sproc.ReturnValue);
            Assert.Equal("Hello world output", sproc.ValueOutStr);
            Assert.Equal(now.AddDays(1), sproc.ValueOutDt);
            Assert.Single(rows);

            var row = rows[0];
            Assert.Equal("Hello world", row.ValueInStr);
            Assert.Equal(now, row.ValueInDt);
            Assert.Equal("Test", row.OptionalValueStr);
            Assert.True(row.ValueInBit);
            Assert.Equal("Fixed", row.FixedValue);
        }

        [Fact]
        public async Task Query_StoredProcedure_Targeted()
        {
            var now = DateTimeOffset.UtcNow;
            var sproc = new spStoredProcedure_Targeted() { ValueInStr = "Hello world", ValueInDt = now, ValueInBit = true, OptionalValueStr = "Test" };
            var rows = await this.connection.NewRequest()
                .Add(sproc)
                .QueryAsync();

            Assert.Equal(99, ((IStoredProcedure)sproc).ReturnValue);
            Assert.Equal("Hello world output", sproc.ValueOutStr);
            Assert.Equal(now.AddDays(1), sproc.ValueOutDt);
            Assert.Single(rows);

            var row = rows[0];
            Assert.Equal("Hello world", row.ValueInStr);
            Assert.Equal(now, row.ValueInDt);
            Assert.Equal("Test", row.OptionalValueStr);
            Assert.True(row.ValueInBit);
            Assert.Equal("Fixed", row.FixedValue);
        }

        [Fact]
        public async Task Query_StoredProcedure_Targeted_Single()
        {
            var now = DateTimeOffset.UtcNow;
            var sproc = new spStoredProcedure_Targeted() { ValueInStr = "Hello world", ValueInDt = now, ValueInBit = true };
            var row = await this.connection.NewRequest()
                .Add(sproc)
                .QuerySingleAsync();

            Assert.Equal(99, ((IStoredProcedure)sproc).ReturnValue);
            Assert.Equal("Hello world output", sproc.ValueOutStr);
            Assert.Equal(now.AddDays(1), sproc.ValueOutDt);

            Assert.Equal("Hello world", row.ValueInStr);
            Assert.Equal(now, row.ValueInDt);
            Assert.Null(row.OptionalValueStr);
            Assert.True(row.ValueInBit);
            Assert.Equal("Fixed", row.FixedValue);
        }

        [Fact]
        public async Task Map_StoredProcedure_Targeted()
        {
            var now = DateTimeOffset.UtcNow;
            var sproc = new spStoredProcedure_Targeted() { ValueInStr = "Hello world", ValueInDt = now, ValueInBit = true };
            var rows = await this.connection.NewRequest()
                .Add(sproc)
                .MapAsync();

            Assert.Equal(99, ((IStoredProcedure)sproc).ReturnValue);
            Assert.Equal("Hello world output", sproc.ValueOutStr);
            Assert.Equal(now.AddDays(1), sproc.ValueOutDt);
            Assert.Single(rows);

            var row = rows[0];
            Assert.Equal("Hello world", row.ValueInStr);
            Assert.Equal(now, row.ValueInDt);
            Assert.Null(row.OptionalValueStr);
            Assert.True(row.ValueInBit);
            Assert.Equal("Fixed", row.FixedValue);
        }

        [Fact]
        public async Task Map_StoredProcedure_Tuple()
        {
            var now = DateTimeOffset.UtcNow;
            var sproc = new spStoredProcedure_Targeted() { ValueInStr = "Hello world", ValueInDt = now, ValueInBit = true };
            var tuple = await this.connection.NewRequest()
                .Add(sproc)
                .MapSingleAsync<(spStoredProcedureResult result1, spStoredProcedureResult result2)>();

            Assert.Equal(99, ((IStoredProcedure)sproc).ReturnValue);
            Assert.Equal("Hello world output", sproc.ValueOutStr);
            Assert.Equal(now.AddDays(1), sproc.ValueOutDt);

            var row = tuple.result1;

            Assert.Equal("Hello world", row.ValueInStr);
            Assert.Equal(now, row.ValueInDt);
            Assert.Null(row.OptionalValueStr);
            Assert.True(row.ValueInBit);
            Assert.Equal("Fixed", row.FixedValue);
        }

        [Fact]
        public async Task Map_StoredProcedure_Graph_Tuple()
        {
            var now = DateTimeOffset.UtcNow;
            var sproc = new spStoredProcedure_Untargeted() { ValueInStr = "Hello world", ValueInDt = now };

            var tuple = await this.connection.NewRequest()
                .Add(new spGetCustomers())
                .Add(new spGetOrders())
                .Add<spStoredProcedureResult>(sproc)
                .MapSingleAsync <(Customer[] customers, spStoredProcedureResult result)>();

            Assert.Equal(3, tuple.customers.Length);
            Assert.Equal(2, tuple.customers[0].Orders.Length);
            Assert.Equal(1, tuple.customers[1].Orders.Length);
            Assert.Equal(1, tuple.customers[2].Orders.Length);
            Assert.Equal("Hello world output", sproc.ValueOutStr);
        }

        [Fact]
        public async Task Map_StoredProcedure_Graph_Targeted()
        {
            var customers = await this.connection.NewRequest()
                .Add(new spGetCustomers())
                .Add(new spGetOrders())
                .MapAsync();

            Assert.Equal(3, customers.Count);
            Assert.Equal(2, customers[0].Orders.Length);
            Assert.Equal(1, customers[1].Orders.Length);
            Assert.Equal(1, customers[2].Orders.Length);
        }

        [Fact]
        public async Task Map_StoredProcedure_Graph_ColumnMapped()
        {
            ColumnAttributeTypeMap.SetTypeMap<OrderColumnMapped>();

            var customers = await this.connection.NewRequest()
                .Add<CustomerColumnMapped>(new spGetCustomers())
                .Add<OrderColumnMapped>(new spGetOrders())
                .MapAsync();

            Assert.Equal(3, customers.Count);
            Assert.Equal(2, customers[0].Orders.Length);
            Assert.Equal(1, customers[1].Orders.Length);
            Assert.Equal(1, customers[2].Orders.Length);
            Assert.Equal(250, customers[0].Orders[0].Amount);
        }


        [Fact]
        public async Task Multi_Stream()
        {
            using var connection = this.connection.NewRequest()
                .Add(new spMultiResult())
                .OpenConnection();
            var r1 = (await connection.StreamNextResultSetAsync<spMultiResult1>()).ToList();
            var r2 = (await connection.StreamNextResultSetAsync<spMultiResult2>()).ToList();

            Assert.Single(r1);
            Assert.Single(r2);
        }

        [Fact]
        public async Task Multi_Map()
        {
            var result = await this.connection.NewRequest()
                .Add(new spMultiResult(), typeof(spMultiResult1), typeof(spMultiResult2))
                .MapSingleAsync<(spMultiResult1[] result1, spMultiResult2[] result2)>();

            Assert.Single(result.result1);
            Assert.Single(result.result2);
        }
    }
}
