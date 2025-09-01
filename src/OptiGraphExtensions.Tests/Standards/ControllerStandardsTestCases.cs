using System.Reflection;

using Microsoft.AspNetCore.Mvc;

using OptiGraphExtensions.Administration;

namespace OptiGraphExtensions.Tests.Standards;

public static class ControllerStandardsTestCases
{
    public static IEnumerable<TestCaseData> PageControllerActionTestCases
    {
        get
        {
            var assembly = Assembly.GetAssembly(typeof(AdministrationController));

            if (assembly == null)
            {
                yield break;
            }

            var controllers = assembly.GetTypes()
                                      .Where(t => t.BaseType?.Name.StartsWith("Controller") ?? false)
                                      .ToList();

            foreach (var controller in controllers)
            {
                var actions = controller.GetMethods()
                                        .Where(x => x.DeclaringType == controller && x.IsPublic)
                                        .ToList();

                foreach (var methodInfo in actions)
                {
                    if (methodInfo.ReturnType == typeof(IActionResult) || methodInfo.ReturnType == typeof(Task<IActionResult>))
                    {
                        yield return new TestCaseData(controller.Name, methodInfo.Name, methodInfo);
                    }
                }
            }
        }
    }
}
