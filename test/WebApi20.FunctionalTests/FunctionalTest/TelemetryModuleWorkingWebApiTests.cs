namespace WebApi20.FunctionalTests.FunctionalTest
{
    using FunctionalTestUtils;
    using System;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.W3C;
    using Xunit;
    using Xunit.Abstractions;

    public class TelemetryModuleWorkingWebApiTests : TelemetryTestsBase, IDisposable
    {
        private const string assemblyName = "WebApi20.FunctionalTests20";
        public TelemetryModuleWorkingWebApiTests(ITestOutputHelper output) : base (output)
        {
        }

        // The NET451 conditional check is wrapped inside the test to make the tests visible in the test explorer. We can move them to the class level once if the issue is resolved.

        [Fact]
        public void TestBasicDependencyPropertiesAfterRequestingBasicPage()
        {
            const string RequestPath = "/api/values";

            using (var server = new InProcessServer(assemblyName, this.output))
            {
                DependencyTelemetry expected = new DependencyTelemetry();
                expected.ResultCode = "200";
                expected.Success = true;
                expected.Name = "GET " + RequestPath;
                expected.Data = server.BaseHost + RequestPath;

                this.ValidateBasicDependency(server, RequestPath, expected);
            }
        }

        [Fact]
        public void TestDependencyAndRequestWithW3CStandard()
        {
            Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_ENABLE_W3C_TRACING", bool.TrueString);
            const string RequestPath = "/api/values";

            using (var server = new InProcessServer(assemblyName, this.output))
            {
                DependencyTelemetry expected = new DependencyTelemetry
                {
                    ResultCode = "200",
                    Success = true,
                    Name = "GET " + RequestPath,
                    Data = server.BaseHost + RequestPath
                };

                var activity = new Activity("dummy")
                    .Start()
                    .GenerateW3CContext();

                string expectedTraceId = activity.GetTraceId();
                string expectedParentSpanId = activity.GetSpanId();

                var (request, dependency) = this.ValidateBasicDependency(server, RequestPath, expected);

                Assert.Equal(expectedTraceId, request.tags["ai.operation.id"]);
                Assert.Equal(expectedTraceId, dependency.tags["ai.operation.id"]);
                Assert.Equal(expectedParentSpanId, dependency.tags["ai.operation.parentId"]);
            }
        }

        [Fact]
        public void TestIfPerformanceCountersAreCollected()
        {
#if NET451 || NET461
            this.output.WriteLine("Validating perfcounters");
            ValidatePerformanceCountersAreCollected(assemblyName);
#endif
        }

        public void Dispose()
        {
            while (Activity.Current != null)
            {
                Activity.Current.Stop();
            }
            Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_ENABLE_W3C_TRACING", null);
        }
    }
}
