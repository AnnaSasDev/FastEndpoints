using FakeItEasy;
using System.Globalization;
using System.Net;
using Create = Customers.Create;
using List = Customers.List;
using Orders = Sales.Orders;
using Update = Customers.Update;
using UpdateWithHdr = Customers.UpdateWithHeader;

namespace Web;

[ClassDataSource<Sut>]
public class CustomersTests(Sut App) : TestBase
{
    [Test]
    public async Task ListRecentCustomers()
    {
        var (_, res) = await App.AdminClient.GETAsync<List.Recent.Endpoint, List.Recent.Response>();

        await Assert.That(res.Customers).IsNotNull();
        await Assert.That(res.Customers!).HasCount().EqualTo(3);
        await Assert.That(res.Customers!.First().Key).IsEqualTo("ryan gunner");
        await Assert.That(res.Customers!.Last().Key).IsEqualTo("ryan reynolds");
    }

    [Test]
    public async Task ListRecentCustomersCookieScheme()
    {
        var (rsp, _) = await App.AdminClient.GETAsync<List.Recent.Endpoint_V1, List.Recent.Response>();

        await Assert.That(rsp.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task CreateNewCustomer()
    {
        var (rsp, res) = await App.AdminClient.POSTAsync<Create.Endpoint, Create.Request, string>(
                             new()
                             {
                                 CreatedBy = "this should be replaced by claim",
                                 CustomerName = "test customer",
                                 PhoneNumbers = new[] { "123", "456" }
                             });

        await Assert.That(rsp.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(res).IsEqualTo("Email was not sent during testing! admin");
    }

    [Test]
    public async Task CustomerUpdateByCustomer()
    {
        var (_, res) = await App.CustomerClient.PUTAsync<Update.Endpoint, Update.Request, string>(
                           new()
                           {
                               CustomerID = "this will be auto bound from claim",
                               Address = "address",
                               Age = 123,
                               Name = "test customer"
                           });

        await Assert.That(res).IsEqualTo("CST001");
    }

    [Test]
    public async Task CustomerUpdateAdmin()
    {
        var (_, res) = await App.AdminClient.PUTAsync<Update.Endpoint, Update.Request, string>(
                           new()
                           {
                               CustomerID = "customer id set by admin user",
                               Address = "address",
                               Age = 123,
                               Name = "test customer"
                           });

        await Assert.That(res).IsEqualTo("CST001");
    }

    [Test]
    public async Task CreateOrderByCustomer()
    {
        var (rsp, res) = await App.CustomerClient.POSTAsync<Orders.Create.Endpoint, Orders.Create.Request, Orders.Create.Response>(
                             new()
                             {
                                 CustomerID = 12345,
                                 ProductID = 100,
                                 Quantity = 23
                             });

        await Assert.That(rsp.IsSuccessStatusCode).IsTrue();
        await Assert.That(res.OrderID).IsEqualTo(54321);
        await Assert.That(res.AnotherMsg).IsEqualTo("Email was not sent during testing!");
        await Assert.That(res.Event.One).IsEqualTo(100);
        await Assert.That(res.Event.Two).IsEqualTo(200);

        await Assert.That(res.Header1).IsEqualTo(0);
        await Assert.That(res.Header2).IsEqualTo(default);
        var enumerable = await Assert.That(rsp.Headers.GetValues("x-header-one")).HasSingleItem();
        await Assert.That(enumerable!.Single()).IsEqualTo("12345");
        
        var date = DateOnly.Parse(rsp.Headers.GetValues("Header2").Single(), CultureInfo.InvariantCulture);
        await Assert.That(date).IsEquivalentTo(new DateOnly(2020, 11, 12));
    }

    [Test]
    public async Task CreateOrderByCustomerGuidTest()
    {
        var guid = Guid.NewGuid();

        var (rsp, res) = await App.CustomerClient.POSTAsync<Orders.Create.Request, Orders.Create.Response>(
                             $"api/sales/orders/create/{guid}",
                             new()
                             {
                                 CustomerID = 12345,
                                 ProductID = 100,
                                 Quantity = 23,
                                 GuidTest = Guid.NewGuid()
                             });

        await Assert.That(rsp.IsSuccessStatusCode).IsTrue();
        await Assert.That(res.OrderID).IsEqualTo(54321);
        await Assert.That(res.GuidTest).IsEqualTo(guid);
    }

    [Test]
    public async Task CustomerUpdateByCustomerWithTenantIdInHeader()
    {
        var (_, res) = await App.CustomerClient.PUTAsync<UpdateWithHdr.Endpoint, UpdateWithHdr.Request, string>(
                           new(
                               CustomerID: 10,
                               TenantID: "this will be set to qwerty from header",
                               Name: "test customer",
                               Age: 123,
                               Address: "address"));

        var results = res.Split('|');
        await Assert.That(results).HasCount().GreaterThanOrEqualTo(2);
        await Assert.That(results[0]).IsEqualTo("CST001");
        await Assert.That(results[1]).IsEqualTo("this will be set to qwerty from header");
    }
}