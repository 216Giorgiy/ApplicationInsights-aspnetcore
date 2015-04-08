Microsoft Application Insights for Asp.Net vNext applications
=============================================================

This repository has a code for [Application Insights monitoring](http://azure.microsoft.com/en-us/services/application-insights/) of [Asp.Net vNext](https://github.com/aspnet/home) applications. Read about contrubution policies on Application Insights Home [repository](https://github.com/microsoft/appInsights-home)


Getting Started
---------------

Add NuGet feed http://appinsights-aspnet.azurewebsites.net/nuget/. It has NuGet: Microsoft.ApplicationInsights.AspNet.

For standard Asp.Net template you need to modify four files (this will be the default template instrumentation in future).

***project.json*** 
Add new reference:
```
"Microsoft.ApplicationInsights.AspNet": "1.0.0.0-alpha"
```

***config.json*** 
Configure instrumentation key:
```
 "ApplicationInsights": {
 	"InstrumentationKey": "11111111-2222-3333-4444-555555555555"
 }
```

***Startup.cs***
Add service:
```
services.AddApplicationInsightsTelemetry(Configuration);
```

Add middleware and configure developer mode: 

```
// Add Application Insights monitoring to the request pipeline as a very first middleware.
app.UseApplicationInsightsRequestTelemetry();
...
// Add the following to the request pipeline only in development environment.
if (string.Equals(env.EnvironmentName, "Development", StringComparison.OrdinalIgnoreCase))
{
	app.SetApplicationInsightsTelemetryDeveloperMode();
}
...
// Add Application Insights exceptions handling to the request pipeline.
app.UseApplicationInsightsExceptionTelemetry();
```

***_Layout.cshtml***
Define using and injection:

```
@using Microsoft.ApplicationInsights.AspNet
@inject Microsoft.ApplicationInsights.DataContracts.RequestTelemetry RequestTelelemtry
```

And insert HtmlHelper to the end of ```<head>``` section:

```
	@Html.ApplicationInsightsJavaScriptSnippet(RequestTelelemtry.Context.InstrumentationKey);
</head>
```

Repository structure
--------------------

```
root\
    ApplicationInsights.AspNet.sln - Main Solution

    src\
        ApplicationInsights.AspNet - Application Insights package

    test\
        ApplicationInsights.AspNet.Tests - Unit tests
        FunctionalTestUtils - test utilities for functional tests
        SampleWebAppIntegration  - functional MVC test application
```

Developing
----------
1. Repository (private now): https://github.com/microsoft/AppInsights-aspnetv5
2. Asp.Net information: https://github.com/aspnet/home
3. VS 2015 installation: 
 - *(recommended by [Anastasia](https://github.com/abaranch))*: http://blogs.msdn.com/b/visualstudioalm/archive/2014/06/04/visual-studio-14-ctp-now-available-in-the-virtual-machine-azure-gallery.aspx
 - You can just install it on your machine: https://www.visualstudio.com/en-us/news/vs2015-vs.aspx


4. Make sure you have these (only these and in this order) feeds configured in Visual Studio. You may need to restart Visual Studio to allow packages to be restored correctly:
 - https://www.myget.org/F/aspnetvnext
 - http://appinsights-aspnet.azurewebsites.net/nuget/
 - https://www.nuget.org/api/v2/

Running and writing tests
-------------------------
There are two sets of tests unit tests and functional tests. Please use unit tests for all features testing. The purpose of functional tests is just end-to-end validation of functionality on sample applications.


*Functional tests*
Functional tests are regular web applications with unit tests integrated into them. Application can be compiled as a regular web application as well as set of tests. Typical functional test will do the following:

1. Host the current project in In-Proc server.
2. Initialize application insights telemetry channel.
3. Initiate request to self hosted web application using HttpClient.
4. Check data received in telemetry channel.

Those are modifications made for regular web application to make it work this way:

Add dependencies to project.json:


```
"FunctionalTestUtils": "1.0.0-*",
"xunit": "2.1.0.0-beta1-build2945",
"xunit.runner.aspnet": "2.1.0.0-beta1-build60",
"xunit.runner.visualstudio": "2.1.0.0-beta1-build1051"
```

and test command:

```
"test": "xunit.runner.aspnet"
```

Add this initialization logic to Startup.cs:

```
services.AddFunctionalTestTelemetryChannel();
```
