using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestProject;

class Program
{
    public static PlanningApi _planningApi = new PlanningApi();

    public static void Main()
    {
        _ = MainAsync();
        Console.ReadKey();

    }

    public static async Task MainAsync()
    {
        var list = await _planningApi.planning.GetAvailableOrgUnitGuids(2025, 5);

        foreach (var unit in list.Take(12)) 
            Console.WriteLine(unit);

        Console.ReadKey();
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
}