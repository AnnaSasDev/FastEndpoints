using FakeItEasy;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using TestCases.CustomRequestBinder;
using TestCases.FormBindingComplexDtos;
using Assert=TUnit.Assertions.Assert;
using ByteEnum = TestCases.QueryObjectBindingTest.ByteEnum;
using Endpoint = TestCases.JsonArrayBindingToListOfModels.Endpoint;
using Person = TestCases.RouteBindingTest.Person;
using Request = TestCases.RouteBindingTest.Request;
using Response = TestCases.RouteBindingInEpWithoutReq.Response;

namespace Binding;

[ClassDataSource<Sut>]
public class BindingTests(Sut App) : TestBase<Sut>
{
    [Test]
    public async Task RouteValueReadingInEndpointWithoutRequest()
    {
        var (rsp, res) = await App.Client.GETAsync<
                             EmptyRequest,
                             Response>("/api/test-cases/ep-witout-req-route-binding-test/09809/12", new());

        await Assert.That(rsp.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(res.CustomerID).IsEqualTo(09809);
        await Assert.That(res.OtherID).IsEqualTo(12);
    }

    [Test]
    public async Task RouteValueReadingIsRequired()
    {
        var (rsp, res) = await App.Client.GETAsync<
                             EmptyRequest,
                             ErrorResponse>("/api/test-cases/ep-witout-req-route-binding-test/09809/lkjhlkjh", new());

        await Assert.That(rsp.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(res.Errors).IsNotNull()
                    .And.ContainsKey("otherId");
    }

    [Test]
    public async Task RouteValueBinding()
    {
        var (rsp, res) = await App.Client
                                  .POSTAsync<Request, TestCases.RouteBindingTest.Response>(
                                      "api/test-cases/route-binding-test/something/true/99/483752874564876/2232.12/123.45?Url=https://test.com&Custom=12&CustomList=1;2",
                                      new()
                                      {
                                          Bool = false,
                                          DecimalNumber = 1,
                                          Double = 1,
                                          FromBody = "from body value",
                                          Int = 1,
                                          Long = 1,
                                          String = "nothing",
                                          Custom = new() { Value = 11111 },
                                          CustomList = [0]
                                      });

        await Assert.That(rsp.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(res.String).IsEqualTo("something");
        await Assert.That(res.Bool).IsEqualTo(true);
        await Assert.That(res.Int).IsEqualTo(99);
        await Assert.That(res.Long).IsEqualTo(483752874564876);
        await Assert.That(res.Double).IsEqualTo(2232.12);
        await Assert.That(res.FromBody).IsEqualTo("from body value");
        await Assert.That(res.Decimal).IsEqualTo(123.45m);
        await Assert.That(res.Url).IsEqualTo("https://test.com/");
        await Assert.That(res.Custom.Value).IsEqualTo(12);
        await Assert.That(res.CustomList).IsEqualTo([1, 2]);
    }

    [Test]
    public async Task RouteValueBindingFromQueryParams()
    {
        var (rsp, res) = await App.Client
                                  .POSTAsync<Request, TestCases.RouteBindingTest.Response>(
                                      "api/test-cases/route-binding-test/something/true/99/483752874564876/2232.12/123.45/" +
                                      "?Bool=false&String=everything",
                                      new()
                                      {
                                          Bool = false,
                                          DecimalNumber = 1,
                                          Double = 1,
                                          FromBody = "from body value",
                                          Int = 1,
                                          Long = 1,
                                          String = "nothing"
                                      });

        await Assert.That(rsp.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(res.String).IsEqualTo("everything");
        await Assert.That(res.Bool).IsEqualTo(false);
        await Assert.That(res.Int).IsEqualTo(99);
        await Assert.That(res.Long).IsEqualTo(483752874564876);
        await Assert.That(res.Double).IsEqualTo(2232.12);
        await Assert.That(res.FromBody).IsEqualTo("from body value");
        await Assert.That(res.Decimal).IsEqualTo(123.45m);
        await Assert.That(res.Blank).IsNull();
    }

    [Test]
    public async Task JsonArrayBindingToIEnumerableProps()
    {
        var (rsp, res) = await App.Client
                                  .GETAsync<TestCases.JsonArrayBindingForIEnumerableProps.Request,
                                      TestCases.JsonArrayBindingForIEnumerableProps.Response>(
                                      "/api/test-cases/json-array-binding-for-ienumerable-props?" +
                                      "doubles=[123.45,543.21]&" +
                                      "dates=[\"2022-01-01\",\"2022-02-02\"]&" +
                                      "guids=[\"b01ec302-0adc-4a2b-973d-bbfe639ed9a5\",\"e08664a4-efd8-4062-a1e1-6169c6eac2ab\"]&" +
                                      "ints=[1,2,3]&" +
                                      "steven={\"age\":12,\"name\":\"steven\"}&" +
                                      "dict={\"key1\":\"val1\",\"key2\":\"val2\"}",
                                      new());

        await Assert.That(rsp.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(res.Doubles.Length).IsEqualTo(2);
        await Assert.That(res.Doubles[0]).IsEqualTo(123.45);
        await Assert.That(res.Dates.Count).IsEqualTo(2);
        await Assert.That(res.Dates.First()).IsEqualTo(DateTime.Parse("2022-01-01"));
        await Assert.That(res.Guids.Count).IsEqualTo(2);
        await Assert.That(res.Guids[0]).IsEqualTo(Guid.Parse("b01ec302-0adc-4a2b-973d-bbfe639ed9a5"));
        await Assert.That(res.Ints.Count()).IsEqualTo(3);
        await Assert.That(res.Ints.First()).IsEqualTo(1);
        await Assert.That(res.Steven).IsEquivalentTo(new TestCases.JsonArrayBindingForIEnumerableProps.Request.Person
        {
            Age = 12,
            Name = "steven"
        });
        await Assert.That(res.Dict.Count).IsEqualTo(2);
        await Assert.That(res.Dict["key1"]).IsEqualTo("val1");
        await Assert.That(res.Dict["key2"]).IsEqualTo("val2");
    }

    [Test]
    public async Task JsonArrayBindingToListOfModels()
    {
        var (rsp, res) = await App.Client.POSTAsync<
                             Endpoint,
                             List<TestCases.JsonArrayBindingToListOfModels.Request>,
                             List<TestCases.JsonArrayBindingToListOfModels.Response>>(
                         [
                             new() { Name = "test1" },
                             new() { Name = "test2" }
                         ]);

        await Assert.That(rsp.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(res.Count).IsEqualTo(2);
        await Assert.That(res[0].Name).IsEqualTo("test1");
    }

    [Test]
    public async Task JsonArrayBindingToIEnumerableDto()
    {
        var req = new TestCases.JsonArrayBindingToIEnumerableDto.Request
        {
            new() { Id = 1, Name = "one" },
            new() { Id = 2, Name = "two" }
        };

        var (rsp, res) = await App.Client.POSTAsync<
                             TestCases.JsonArrayBindingToIEnumerableDto.Endpoint,
                             TestCases.JsonArrayBindingToIEnumerableDto.Request,
                             List<TestCases.JsonArrayBindingToIEnumerableDto.Response>>(req);

        await Assert.That(rsp.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(res.Count).IsEqualTo(2);
        await Assert.That(res).IsEquivalentTo(req.Select(x => new TestCases.JsonArrayBindingToIEnumerableDto.Response { Id = x.Id, Name = x.Name }).ToList());
    }

    [Test]
    public async Task DupeParamBindingToIEnumerableProps()
    {
        var (rsp, res) = await App.Client
                                  .GETAsync<TestCases.DupeParamBindingForIEnumerableProps.Request,
                                      TestCases.DupeParamBindingForIEnumerableProps.Response>(
                                      "/api/test-cases/dupe-param-binding-for-ienumerable-props?" +
                                      "doubles=123.45&" +
                                      "doubles=543.21&" +
                                      "dates=2022-01-01&" +
                                      "dates=2022-02-02&" +
                                      "guids=b01ec302-0adc-4a2b-973d-bbfe639ed9a5&" +
                                      "guids=e08664a4-efd8-4062-a1e1-6169c6eac2ab&" +
                                      "ints=1&" +
                                      "ints=2&" +
                                      "ints=3&" +
                                      "strings=[1,2]&" +
                                      "strings=three&" +
                                      "morestrings=[\"one\",\"two\"]&" +
                                      "morestrings=three&" +
                                      "persons={\"name\":\"john\",\"age\":45}&" +
                                      "persons={\"name\":\"doe\",\"age\":55}",
                                      new());

        await Assert.That(rsp.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(res.Doubles.Length).IsEqualTo(2);
        await Assert.That(res.Doubles[0]).IsEqualTo(123.45);
        await Assert.That(res.Dates.Count).IsEqualTo(2);
        await Assert.That(res.Dates.First()).IsEqualTo(DateTime.Parse("2022-01-01"));
        await Assert.That(res.Guids.Count).IsEqualTo(2);
        await Assert.That(res.Guids[0]).IsEqualTo(Guid.Parse("b01ec302-0adc-4a2b-973d-bbfe639ed9a5"));
        await Assert.That(res.Ints.Count()).IsEqualTo(3);
        await Assert.That(res.Ints.First()).IsEqualTo(1);
        await Assert.That(res.Strings.Length).IsEqualTo(2);
        await Assert.That(res.Strings[0]).IsEqualTo("[1,2]");
        await Assert.That(res.MoreStrings.Length).IsEqualTo(2);
        await Assert.That(res.MoreStrings[0]).IsEqualTo("[\"one\",\"two\"]");
        await Assert.That(res.Persons.Count()).IsEqualTo(2);
        await Assert.That(res.Persons.First().Name).IsEqualTo("john");
        await Assert.That(res.Persons.First().Age).IsEqualTo(45);
        await Assert.That(res.Persons.Last().Name).IsEqualTo("doe");
        await Assert.That(res.Persons.Last().Age).IsEqualTo(55);
    }

    [Test]
    public async Task BindingFromAttributeUse()
    {
        var (rsp, res) = await App.Client
                                  .POSTAsync<Request, TestCases.RouteBindingTest.Response>(
                                      "api/test-cases/route-binding-test/something/true/99/483752874564876/2232.12/123.45/" +
                                      "?Bool=false&String=everything&XBlank=256" +
                                      "&age=45&name=john&id=10c225a6-9195-4596-92f5-c1234cee4de7" +
                                      "&numbers=0&numbers=1&numbers=-222&numbers=1000&numbers=22" +
                                      "&child.id=8bedccb3-ff93-47a2-9fc4-b558cae41a06" +
                                      "&child.name=child name&child.age=-22" +
                                      "&child.strings[0]=string1&child.strings[1]=string2&child.strings[2]=&child.strings[3]=strangeString",
                                      new()
                                      {
                                          Bool = false,
                                          DecimalNumber = 1,
                                          Double = 1,
                                          FromBody = "from body value",
                                          Int = 1,
                                          Long = 1,
                                          String = "nothing",
                                          Blank = 1,
                                          Person = new()
                                          {
                                              Age = 50,
                                              Name = "wrong"
                                          }
                                      });

        await Assert.That(rsp.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(res.String).IsEqualTo("everything");
        await Assert.That(res.Bool).IsEqualTo(false);
        await Assert.That(res.Int).IsEqualTo(99);
        await Assert.That(res.Long).IsEqualTo(483752874564876);
        await Assert.That(res.Double).IsEqualTo(2232.12);
        await Assert.That(res.FromBody).IsEqualTo("from body value");
        await Assert.That(res.Decimal).IsEqualTo(123.45m);
        await Assert.That(res.Blank).IsEqualTo(256);
        await Assert.That(res.Person).IsNotNull()
            .And.IsEquivalentTo(new Person
            {
                Age = 45,
                Name = "john",
                Id = Guid.Parse("10c225a6-9195-4596-92f5-c1234cee4de7"),
                Child = new()
                {
                    Age = -22,
                    Name = "child name",
                    Id = Guid.Parse("8bedccb3-ff93-47a2-9fc4-b558cae41a06"),
                    Strings = ["string1", "string2", "", "strangeString"]
                },
                Numbers = [0, 1, -222, 1000, 22]
            });
    }

    [Test]
    public async Task BindingObjectFromQueryUse()
    {
        var (rsp, res) = await App.Client
                                  .GETAsync<TestCases.QueryObjectBindingTest.Request, TestCases.QueryObjectBindingTest.Response>(
                                      "api/test-cases/query-object-binding-test" +
                                      "?BoOl=TRUE&String=everything&iNt=99&long=483752874564876&DOUBLE=2232.12&Enum=3" +
                                      "&age=45&name=john&id=10c225a6-9195-4596-92f5-c1234cee4de7" +
                                      "&numbers=0&numbers=1&numbers=-222&numbers=1000&numbers=22" +
                                      "&favoriteDay=Friday&IsHidden=FALSE&ByteEnum=2" +
                                      "&child.id=8bedccb3-ff93-47a2-9fc4-b558cae41a06" +
                                      "&child.name=child name&child.age=-22" +
                                      "&CHILD.FavoriteDays=1&ChiLD.FavoriteDays=Saturday&CHILD.ISHiddeN=TruE" +
                                      "&child.strings=string1&child.strings=string2&child.strings=&child.strings=strangeString",
                                      new());
        
        await Assert.That(rsp.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(res).IsEquivalentTo(
            new TestCases.QueryObjectBindingTest.Response
            {
                Double = 2232.12,
                Bool = true,
                Enum = DayOfWeek.Wednesday,
                String = "everything",
                Int = 99,
                Long = 483752874564876,
                Person = new()
                {
                    Age = 45,
                    Name = "john",
                    Id = Guid.Parse("10c225a6-9195-4596-92f5-c1234cee4de7"),
                    FavoriteDay = DayOfWeek.Friday,
                    ByteEnum = ByteEnum.AnotherCheck,
                    IsHidden = false,
                    Child = new()
                    {
                        Age = -22,
                        Name = "child name",
                        Id = Guid.Parse("8bedccb3-ff93-47a2-9fc4-b558cae41a06"),
                        Strings = ["string1", "string2", "", "strangeString"],
                        FavoriteDays = [DayOfWeek.Monday, DayOfWeek.Saturday],
                        IsHidden = true
                    },
                    Numbers = [0, 1, -222, 1000, 22]
                }
            });
    }

    [Test]
    public async Task BindingArraysOfObjectsFromQueryUse()
    {
        var (rsp, res) = await App.Client
                                  .GETAsync<TestCases.QueryObjectWithObjectsArrayBindingTest.Request,
                                      TestCases.QueryObjectWithObjectsArrayBindingTest.Response>(
                                      "api/test-cases/query-arrays-of-objects-binding-test" +
                                      "?Child.Objects[0].String=test&Child.Objects[0].Bool=true&Child.Objects[0].Double=22.22&Child.Objects[0].Enum=4" +
                                      "&Child.Objects[0].Int=31&Child.Objects[0].Long=22" +
                                      "&Child.Objects[1].String=test2&Child.Objects[1].Enum=Wednesday" +
                                      "&Objects[0].String=test&Objects[0].Bool=true&Objects[0].Double=22.22&Objects[0].Enum=4" +
                                      "&Objects[0].Int=31&Objects[0].Long=22" +
                                      "&Objects[1].String=test2&Objects[1].Enum=Wednesday",
                                      new());

        await Assert.That(rsp.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(res).IsEquivalentTo(
            new TestCases.QueryObjectWithObjectsArrayBindingTest.Response
            {
                Person = new()
                {
                    Child = new()
                    {
                        Objects =
                        [
                            new()
                            {
                                String = "test",
                                Bool = true,
                                Double = 22.22,
                                Enum = DayOfWeek.Thursday,
                                Int = 31,
                                Long = 22
                            },

                            new()
                            {
                                String = "test2",
                                Enum = DayOfWeek.Wednesday
                            }
                        ]
                    },
                    Objects =
                    [
                        new()
                        {
                            String = "test",
                            Bool = true,
                            Double = 22.22,
                            Enum = DayOfWeek.Thursday,
                            Int = 31,
                            Long = 22
                        },

                        new()
                        {
                            String = "test2",
                            Enum = DayOfWeek.Wednesday
                        }
                    ]
                }
            });
    }

    [Test]
    public async Task RangeHandling()
    {
        var res = await App.RangeClient.GetStringAsync("api/test-cases/range", Cancellation);
        await Assert.That(res).IsEqualTo("fghij");
    }

    [Test]
    public async Task FileHandling()
    {
        using var imageContent = new ByteArrayContent(
            await new StreamContent(File.OpenRead("test.png"))
                .ReadAsByteArrayAsync(Cancellation));
        imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");

        using var form = new MultipartFormDataContent
        {
            { imageContent, "File", "test.png" },
            { new StringContent("500"), "Width" },
            { new StringContent("500"), "Height" }
        };

        var res = await App.AdminClient.PostAsync("api/uploads/image/save", form, Cancellation);

        using var md5Instance = MD5.Create();
        await using var stream = await res.Content.ReadAsStreamAsync(Cancellation);
        var resMd5 = BitConverter.ToString(md5Instance.ComputeHash(stream)).Replace("-", "");

        await Assert.That(resMd5).IsEqualTo("8A1F6A8E27D2E440280050DA549CBE3E");
    }

    [Test]
    public async Task FileHandlingFileBinding()
    {
        for (var i = 0; i < 3; i++) //repeat upload multiple times
        {
            await using var stream1 = File.OpenRead("test.png");
            await using var stream2 = File.OpenRead("test.png");
            await using var stream3 = File.OpenRead("test.png");

            var req = new Uploads.Image.SaveTyped.Request
            {
                File1 = new FormFile(stream1, 0, stream1.Length, "File1", "test.png")
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "image/png"
                },
                File2 = new FormFile(stream2, 0, stream2.Length, "File2", "test.png"),
                File3 = new FormFile(stream3, 0, stream2.Length, "File3", "test.png"),
                Width = 500,
                Height = 500,
                GuidId = Guid.NewGuid()
            };

            var res = await App.AdminClient.POSTAsync<
                          Uploads.Image.SaveTyped.Endpoint,
                          Uploads.Image.SaveTyped.Request>(req, sendAsFormData: true);

            using var md5Instance = MD5.Create();
            await using var stream = await res.Content.ReadAsStreamAsync(Cancellation);
            var resMd5 = BitConverter.ToString(md5Instance.ComputeHash(stream)).Replace("-", "");

            await Assert.That(resMd5).IsEqualTo("8A1F6A8E27D2E440280050DA549CBE3E");
        }
    }

    [Test]
    public async Task FormFileCollectionBinding()
    {
        await using var stream1 = File.OpenRead("test.png");
        await using var stream2 = File.OpenRead("test.png");
        await using var stream3 = File.OpenRead("test.png");
        await using var stream4 = File.OpenRead("test.png");
        await using var stream5 = File.OpenRead("test.png");
        await using var stream6 = File.OpenRead("test.png");

        var req = new TestCases.FormFileBindingTest.Request
        {
            File1 = new FormFile(stream1, 0, stream1.Length, "file1", "test1.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            },
            File2 = new FormFile(stream2, 0, stream2.Length, "file2", "test2.png"),

            Cars = new FormFileCollection
            {
                new FormFile(stream3, 0, stream3.Length, "car1", "car1.png"),
                new FormFile(stream4, 0, stream4.Length, "car2", "car2.png")
            },

            Jets = new FormFileCollection
            {
                new FormFile(stream5, 0, stream5.Length, "jet1", "jet1.png"),
                new FormFile(stream6, 0, stream6.Length, "jet2", "jet2.png")
            },

            Width = 500,
            Height = 500
        };

        var (rsp, res) = await App
                               .AdminClient
                               .POSTAsync<
                                   TestCases.FormFileBindingTest.Endpoint,
                                   TestCases.FormFileBindingTest.Request,
                                   TestCases.FormFileBindingTest.Response>(req, sendAsFormData: true);

        await Assert.That(rsp.IsSuccessStatusCode).IsTrue();
        await Assert.That(res.File1Name).IsEqualTo("test1.png");
        await Assert.That(res.File2Name).IsEqualTo("test2.png");
        await Assert.That(res.CarNames).IsEquivalentTo(["car1.png", "car2.png"]);
        await Assert.That(res.JetNames).IsEquivalentTo(["jet1.png", "jet2.png"]);
    }

    [Test]
    public async Task ComplexFormDataBindingViaSendAsFormData()
    {
        var book = new Book
        {
            BarCodes = new List<int>([1, 2, 3]),
            CoAuthors = [new Author { Name = "a1" }, new Author { Name = "a2" }],
            MainAuthor = new() { Name = "main" }
        };

        var (rsp, res) = await App.GuestClient.PUTAsync<ToFormEndpoint, Book, Book>(book, sendAsFormData: true);

        await Assert.That(rsp.IsSuccessStatusCode).IsTrue();
        await Assert.That(res).IsEquivalentTo(book);
    }

    [Test]
    public async Task ComplexFormDataBinding()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/test-cases/form-binding-complex-dtos");
        var content = new MultipartFormDataContent();

        content.Add(new StringContent("book title"), "Title");
        content.Add(new StreamContent(File.OpenRead("test.png")), "CoverImage", "test.png");
        content.Add(new StreamContent(File.OpenRead("test.png")), "SourceFiles[1]", "test.png");
        content.Add(new StreamContent(File.OpenRead("test.png")), "SourceFiles[0]", "test.png");
        content.Add(new StringContent("main author name"), "MainAuthor.Name");
        content.Add(new StreamContent(File.OpenRead("test.png")), "MainAuthor.ProfileImage", "test.png");
        content.Add(new StreamContent(File.OpenRead("test.png")), "MainAuthor.DocumentFiles", "test.png");
        content.Add(new StreamContent(File.OpenRead("test.png")), "MainAuthor.DocumentFiles", "test.png");
        content.Add(new StringContent("main author address street"), "MainAuthor.MainAddress.Street");
        content.Add(new StreamContent(File.OpenRead("test.png")), "MainAuthor.MainAddress.MainImage", "test.png");
        content.Add(new StreamContent(File.OpenRead("test.png")), "MainAuthor.MainAddress.AlternativeImages", "test.png");
        content.Add(new StreamContent(File.OpenRead("test.png")), "MainAuthor.MainAddress.AlternativeImages", "test.png");
        content.Add(new StringContent("main author other address 0 street"), "MainAuthor.OtherAddresses[0].Street");
        content.Add(new StreamContent(File.OpenRead("test.png")), "MainAuthor.OtherAddresses[0].MainImage", "test.png");
        content.Add(new StreamContent(File.OpenRead("test.png")), "MainAuthor.OtherAddresses[0].AlternativeImages", "test.png");
        content.Add(new StreamContent(File.OpenRead("test.png")), "MainAuthor.OtherAddresses[0].AlternativeImages", "test.png");
        content.Add(new StringContent("main author other address 1 street"), "MainAuthor.OtherAddresses[1].Street");
        content.Add(new StreamContent(File.OpenRead("test.png")), "MainAuthor.OtherAddresses[1].MainImage", "test.png");
        content.Add(new StreamContent(File.OpenRead("test.png")), "MainAuthor.OtherAddresses[1].AlternativeImages", "test.png");
        content.Add(new StreamContent(File.OpenRead("test.png")), "MainAuthor.OtherAddresses[1].AlternativeImages", "test.png");
        content.Add(new StringContent("co author 0 name"), "CoAuthors[0].Name");
        content.Add(new StreamContent(File.OpenRead("test.png")), "CoAuthors[0].ProfileImage", "test.png");
        content.Add(new StreamContent(File.OpenRead("test.png")), "CoAuthors[0].DocumentFiles", "test.png");
        content.Add(new StreamContent(File.OpenRead("test.png")), "CoAuthors[0].DocumentFiles", "test.png");
        content.Add(new StringContent("co author 0 address street"), "CoAuthors[0].MainAddress.Street");
        content.Add(new StreamContent(File.OpenRead("test.png")), "CoAuthors[0].MainAddress.MainImage", "test.png");
        content.Add(new StreamContent(File.OpenRead("test.png")), "CoAuthors[0].MainAddress.AlternativeImages", "test.png");
        content.Add(new StreamContent(File.OpenRead("test.png")), "CoAuthors[0].MainAddress.AlternativeImages", "test.png");
        content.Add(new StringContent("co author 0 other address 0 street"), "CoAuthors[0].OtherAddresses[0].Street");
        content.Add(new StreamContent(File.OpenRead("test.png")), "CoAuthors[0].OtherAddresses[0].MainImage", "test.png");
        content.Add(new StreamContent(File.OpenRead("test.png")), "CoAuthors[0].OtherAddresses[0].AlternativeImages[1]", "test.png");
        content.Add(new StreamContent(File.OpenRead("test.png")), "CoAuthors[0].OtherAddresses[0].AlternativeImages[0]", "test.png");
        content.Add(new StringContent("12345"), "BarCodes");
        content.Add(new StringContent("54321"), "BarCodes");
        request.Content = content;
        var response = await App.GuestClient.SendAsync(request, Cancellation);
        response.EnsureSuccessStatusCode();

        var x = TestCases.FormBindingComplexDtos.Endpoint.Result;

        await Assert.That(x).IsNotNull();

        await Assert.That(x!.Title).IsEqualTo("book title");
        await Assert.That(x.CoverImage.Name).IsEqualTo("CoverImage");
        await Assert.That(x.SourceFiles.Count).IsEqualTo(2);
        await Assert.That(x.SourceFiles[0].Name).IsEqualTo("SourceFiles[0]");
        await Assert.That(x.SourceFiles[1].Name).IsEqualTo("SourceFiles[1]");
        await Assert.That(x.MainAuthor).IsNotNull();
        await Assert.That(x.MainAuthor.Name).IsEqualTo("main author name");
        await Assert.That(x.MainAuthor.ProfileImage.Name).IsEqualTo("MainAuthor.ProfileImage");
        await Assert.That(x.MainAuthor.DocumentFiles.Count).IsEqualTo(2);
        await Assert.That(x.MainAuthor.DocumentFiles[0].Name).IsEqualTo("MainAuthor.DocumentFiles");
        await Assert.That(x.MainAuthor.DocumentFiles[1].Name).IsEqualTo("MainAuthor.DocumentFiles");
        await Assert.That(x.MainAuthor.MainAddress).IsNotNull();
        await Assert.That(x.MainAuthor.MainAddress.Street).IsEqualTo("main author address street");
        await Assert.That(x.MainAuthor.MainAddress.MainImage.Name).IsEqualTo("MainAuthor.MainAddress.MainImage");
        await Assert.That(x.MainAuthor.MainAddress.AlternativeImages.Count).IsEqualTo(2);
        await Assert.That(x.MainAuthor.MainAddress.AlternativeImages[0].Name).IsEqualTo("MainAuthor.MainAddress.AlternativeImages");
        await Assert.That(x.MainAuthor.MainAddress.AlternativeImages[1].Name).IsEqualTo("MainAuthor.MainAddress.AlternativeImages");
        await Assert.That(x.MainAuthor.OtherAddresses.Count).IsEqualTo(2);
        await Assert.That(x.MainAuthor.OtherAddresses[0].MainImage.Name).IsEqualTo("MainAuthor.OtherAddresses[0].MainImage");
        await Assert.That(x.MainAuthor.OtherAddresses[0].AlternativeImages.Count).IsEqualTo(2);
        await Assert.That(x.MainAuthor.OtherAddresses[0].AlternativeImages[0].Name).IsEqualTo("MainAuthor.OtherAddresses[0].AlternativeImages");
        await Assert.That(x.MainAuthor.OtherAddresses[0].AlternativeImages[1].Name).IsEqualTo("MainAuthor.OtherAddresses[0].AlternativeImages");
        await Assert.That(x.MainAuthor.OtherAddresses[1].MainImage.Name).IsEqualTo("MainAuthor.OtherAddresses[1].MainImage");
        await Assert.That(x.MainAuthor.OtherAddresses[1].AlternativeImages.Count).IsEqualTo(2);
        await Assert.That(x.MainAuthor.OtherAddresses[1].AlternativeImages[0].Name).IsEqualTo("MainAuthor.OtherAddresses[1].AlternativeImages");
        await Assert.That(x.MainAuthor.OtherAddresses[1].AlternativeImages[1].Name).IsEqualTo("MainAuthor.OtherAddresses[1].AlternativeImages");
        await Assert.That(x.CoAuthors.Count).IsEqualTo(1);
        await Assert.That(x.CoAuthors[0].Name).IsEqualTo("co author 0 name");
        await Assert.That(x.CoAuthors[0].ProfileImage.Name).IsEqualTo("CoAuthors[0].ProfileImage");
        await Assert.That(x.CoAuthors[0].DocumentFiles.Count).IsEqualTo(2);
        await Assert.That(x.CoAuthors[0].DocumentFiles[0].Name).IsEqualTo("CoAuthors[0].DocumentFiles");
        await Assert.That(x.CoAuthors[0].DocumentFiles[1].Name).IsEqualTo("CoAuthors[0].DocumentFiles");
        await Assert.That(x.CoAuthors[0].MainAddress.Street).IsEqualTo("co author 0 address street");
        await Assert.That(x.CoAuthors[0].MainAddress.MainImage.Name).IsEqualTo("CoAuthors[0].MainAddress.MainImage");
        await Assert.That(x.CoAuthors[0].MainAddress.AlternativeImages.Count).IsEqualTo(2);
        await Assert.That(x.CoAuthors[0].MainAddress.AlternativeImages[0].Name).IsEqualTo("CoAuthors[0].MainAddress.AlternativeImages");
        await Assert.That(x.CoAuthors[0].MainAddress.AlternativeImages[1].Name).IsEqualTo("CoAuthors[0].MainAddress.AlternativeImages");
        await Assert.That(x.CoAuthors[0].OtherAddresses.Count).IsEqualTo(1);
        await Assert.That(x.CoAuthors[0].OtherAddresses[0].Street).IsEqualTo("co author 0 other address 0 street");
        await Assert.That(x.CoAuthors[0].OtherAddresses[0].MainImage.Name).IsEqualTo("CoAuthors[0].OtherAddresses[0].MainImage");
        await Assert.That(x.CoAuthors[0].OtherAddresses[0].AlternativeImages.Count).IsEqualTo(2);
        await Assert.That(x.CoAuthors[0].OtherAddresses[0].AlternativeImages[0].Name).IsEqualTo("CoAuthors[0].OtherAddresses[0].AlternativeImages[0]");
        await Assert.That(x.CoAuthors[0].OtherAddresses[0].AlternativeImages[1].Name).IsEqualTo("CoAuthors[0].OtherAddresses[0].AlternativeImages[1]");
        await Assert.That(x.BarCodes.First()).IsEqualTo(12345);
        await Assert.That(x.BarCodes.Last()).IsEqualTo(54321);
    }

    [Test]
    public async Task PlainTextBodyModelBinding()
    {
        using var stringContent = new StringContent("this is the body content");
        stringContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");

        var rsp = await App.AdminClient.PostAsync("test-cases/plaintext/12345", stringContent, Cancellation);

        var res = await rsp.Content.ReadFromJsonAsync<TestCases.PlainTextRequestTest.Response>(Cancellation);

        await Assert.That(res).IsNotNull();
        await Assert.That(res!.BodyContent).IsEqualTo("this is the body content");
        await Assert.That(res.Id).IsEqualTo(12345);
    }

    [Test]
    public async Task GetRequestWithRouteParameterAndReqDto()
    {
        var (rsp, res) = await App.CustomerClient.GETAsync<EmptyRequest, ErrorResponse>(
                             "/api/sales/orders/retrieve/54321",
                             new());

        await Assert.That(rsp.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(res.Message).IsEqualTo("ok!");
    }

    [Test]
    public async Task QueryParamReadingInEndpointWithoutRequest()
    {
        var (rsp, res) = await App.GuestClient.GETAsync<
                             EmptyRequest,
                             TestCases.QueryParamBindingInEpWithoutReq.Response>(
                             "/api/test-cases/ep-witout-req-query-param-binding-test" +
                             "?customerId=09809" +
                             "&otherId=12" +
                             "&doubles=[123.45,543.21]" +
                             "&guids=[\"b01ec302-0adc-4a2b-973d-bbfe639ed9a5\",\"e08664a4-efd8-4062-a1e1-6169c6eac2ab\"]" +
                             "&ints=[1,2,3]" +
                             "&floaty=3.2",
                             new());

        await Assert.That(rsp.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(res.CustomerID).IsEqualTo(09809);
        await Assert.That(res.OtherID).IsEqualTo(12);
        await Assert.That(res.Doubles).IsEquivalentTo([123.45, 543.21]);
        await Assert.That(res.Guids).IsEquivalentTo([Guid.Parse("b01ec302-0adc-4a2b-973d-bbfe639ed9a5"), Guid.Parse("e08664a4-efd8-4062-a1e1-6169c6eac2ab")]);
        await Assert.That(res.Ints).IsEquivalentTo([1, 2, 3]);
        await Assert.That(res.Floaty).IsEqualTo(3.2f);
    }

    [Test]
    public async Task QueryParamReadingIsRequired()
    {
        var (rsp, res) = await App.GuestClient.GETAsync<
                             EmptyRequest,
                             ErrorResponse>("/api/test-cases/ep-witout-req-query-param-binding-test?customerId=09809&otherId=lkjhlkjh", new());

        await Assert.That(rsp.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(res.Errors).ContainsKey("otherId");
    }

    [Test]
    public async Task FromBodyJsonBinding()
    {
        var (rsp, res) = await App.CustomerClient.POSTAsync<
                             TestCases.FromBodyJsonBinding.Endpoint,
                             TestCases.FromBodyJsonBinding.Request,
                             TestCases.FromBodyJsonBinding.Response>(
                             new()
                             {
                                 Product = new()
                                 {
                                     Id = 202,
                                     Name = "test product",
                                     Price = 200.10m
                                 }
                             });

        await Assert.That(rsp.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(res.Product.Id).IsEqualTo(202);
        await Assert.That(res.Product.Name).IsEqualTo("test product");
        await Assert.That(res.Product.Price).IsEqualTo(200.10m);
        await Assert.That(res.CustomerID).IsEqualTo(123);
        await Assert.That(res.Id).IsEqualTo(0);
    }

    [Test]
    public async Task FromBodyJsonBindingValidationError()
    {
        var (rsp, res) = await App.CustomerClient.POSTAsync<
                             TestCases.FromBodyJsonBinding.Endpoint,
                             TestCases.FromBodyJsonBinding.Request,
                             ErrorResponse>(
                             new()
                             {
                                 Product = new()
                                 {
                                     Id = 202,
                                     Name = "test product",
                                     Price = 10.10m
                                 }
                             });

        await Assert.That(rsp.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(res.Errors).ContainsKey("Product.Price")
            .And.HasCount().EqualTo(1);
    }

    [Test]
    public async Task CustomRequestBinder()
    {
        var (rsp, res) = await App.CustomerClient.POSTAsync<
                             TestCases.CustomRequestBinder.Endpoint,
                             Product,
                             TestCases.CustomRequestBinder.Response>(
                             new()
                             {
                                 Id = 202,
                                 Name = "test product",
                                 Price = 10.10m
                             });

        await Assert.That(rsp.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(res.Product).IsNotNull();
        await Assert.That(res.Product!.Id).IsEqualTo(202);
        await Assert.That(res.Product.Name).IsEqualTo("test product");
        await Assert.That(res.Product.Price).IsEqualTo(10.10m);
        await Assert.That(res.CustomerID).IsEqualTo("123");
        await Assert.That(res.Id).IsNull();
    }

    [Test]
    public async Task TypedHeaderPropertyBinding()
    {
        using var stringContent = new StringContent("this is the body content");
        stringContent.Headers.ContentDisposition = ContentDispositionHeaderValue.Parse("attachment; filename=\"_filename_.jpg\"");

        var rsp = await App.GuestClient.PostAsync("api/test-cases/typed-header-binding-test", stringContent, Cancellation);
        await Assert.That(rsp.IsSuccessStatusCode).IsTrue();

        var res = await rsp.Content.ReadFromJsonAsync<string>(Cancellation);
        await Assert.That(res).IsEqualTo("_filename_.jpg");
    }

    [Test]
    public async Task DontBindAttribute()
    {
        var req = new TestCases.DontBindAttributeTest.Request
        {
            Id = 123,
            Name = "test"
        };

        var rsp = await App.GuestClient.PostAsJsonAsync("api/test-cases/dont-bind-attribute-test/IGNORE_ME", req, Cancellation);
        await Assert.That(rsp.IsSuccessStatusCode).IsTrue();

        var res = await rsp.Content.ReadAsStringAsync(Cancellation);
        await Assert.That(res).IsEqualTo("123 - test");
    }
}