using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestProject;
using TestProject.Model;

class Program
{
    public static PlanningApi _planningApi = new PlanningApi();
    public static DiadockApi _diadockApi = new DiadockApi();
    private static RetailsApi _retailsApi = new RetailsApi();

    public static void Main()
    {
        _ = MainAsync();
        Console.ReadKey();

    }

    public static async Task MainAsync()
    {
        await TestDiadocGetDocument();
        await TestRetails();
        await TestPlanningGetAvailableOrgUnitGuids();

        Console.ReadKey();
    }

    private static async Task TestDiadocGetDocument()
    {
        var data = await _diadockApi.diadoc.Documents(Guid.Parse("24a69df2-106f-489c-a612-3f238d48a3ba"), null, new DateTime(2025, 07, 17), new DateTime(2025, 07, 23, 23, 59, 59), 0, 0, false);
    }

    private static async Task TestPlanningGetAvailableOrgUnitGuids()
    {
        var data = await _planningApi.planning.GetAvailableOrgUnitGuids(2025, 5);
    }

    private static async Task TestRetails()
    {
        Guid[] retailIds = { Guid.Parse("07cfb899-a450-493c-acc5-120f3819e8f5") };

        var test1 = await _retailsApi.retails.GetRequest(new GetRetails
        {
            RetailGuids = retailIds
        });

        var test2 = await _retailsApi.retails.Short(new GetRetails
        {
            RetailGuids = retailIds
        });
    }

    public class PlanningApi
    {
        public class BaseUrl : ApiServiceBaseUrl
        {
            public override string Url =>
                //"https://localhost:7233/api/";
                "https://api.garzdrav.ru:7090/v1/planning/api/";
        }

        public readonly Planning planning = new Planning();


        public class Planning : ApiService<BaseUrl>
        {
            public async Task<List<Guid>> GetAvailableOrgUnitGuids(int year, int month) =>
                await GetAsync<List<Guid>>(year, month);


        }
    }

    public class DiadockApi
    {
        public class BaseUrl : ApiServiceBaseUrl
        {
            public override string Url => "https://api.garzdrav.ru:7090/v1/diadocservice/api/";
        }

        public readonly Diadoc diadoc = new Diadoc();

        public class Diadoc : ApiService<BaseUrl>
        {
            public async Task<IEnumerable<Document>> Documents(Guid organizationGuid, string diadocDepartmentId, DateTime dateFrom, DateTime dateTo, int category, int dateFilter, bool excludeSubdepartments) =>
                await GetQueryAsync<IEnumerable<Document>>(organizationGuid, diadocDepartmentId, dateFrom, dateTo, category, dateFilter, excludeSubdepartments);
        }
    }


    public class RetailsApi
    {
        public class BaseUrl : ApiServiceBaseUrl
        {
            public override string Url => "https://api.garzdrav.ru:7090/v1/retails/api/";
        }

        public readonly Retails retails = new Retails();


        public class Retails : ApiService<BaseUrl>
        {
            public async Task<IEnumerable<RetailModel>> GetRequest(GetRetails request) =>
                await PostAsync<IEnumerable<RetailModel>>(request);

            public async Task<IEnumerable<ShortRetailModel>> Short(GetRetails request) =>
                await PostAsync<IEnumerable<ShortRetailModel>>(request);

        }
    }
}