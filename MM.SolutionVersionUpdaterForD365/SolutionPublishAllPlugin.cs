using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace MM.SolutionVersionUpdaterForD365;

public class SolutionPublishAllPlugin(string unsecureString) : IPlugin
{
    public void Execute(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null || string.IsNullOrWhiteSpace(unsecureString))
            return;

        var factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
        var systemUserService = factory.CreateOrganizationService(null);

        var solutions = RetrieveSolutions(systemUserService);

        if (solutions == default || !solutions.Any())
            return;
        
        foreach (var solution in solutions)
        {
            var solutionVersion = solution.GetAttributeValue<string>("version");

            var majorVersion = solutionVersion.Split('.')[0];
            var minorVersion = solutionVersion.Split('.')[1];
            var maintenanceVersion = solutionVersion.Split('.')[2];
            var buildVersion = solutionVersion.Split('.')[3];

            Update(systemUserService, new Entity(solution.LogicalName, solution.Id)
            {
                ["version"] =
                    $"{majorVersion}.{minorVersion}.{maintenanceVersion}.{IncrementVersion(buildVersion)}",
            });
        }
    }

    private static string IncrementVersion(string version) =>
        int.TryParse(version, out var r) ? (r + 1).ToString() : version;

    private List<Entity> RetrieveSolutions(IOrganizationService service)
    {
        var query = new QueryExpression("solution");
        query.ColumnSet.AddColumns("version");
        query.Criteria.AddCondition("uniquename", ConditionOperator.In, unsecureString.Split(','));

        return service.RetrieveMultiple(query).Entities.ToList();
    }

    private static void Update(IOrganizationService service, Entity entity) => service.Update(entity);
}