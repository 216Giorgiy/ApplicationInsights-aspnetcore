﻿namespace Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http.Headers;
    using System.Reflection;
    using Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.AspNetCore.Common;
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.W3C;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DiagnosticAdapter;
    using Microsoft.Extensions.Primitives;

#pragma warning disable 612, 618
    /// <summary>
    /// <see cref="IApplicationInsightDiagnosticListener"/> implementation that listens for events specific to AspNetCore hosting layer.
    /// </summary>
    internal class HostingDiagnosticListener : IApplicationInsightDiagnosticListener
    {
        /// <summary>
        /// Determine whether the running AspNetCore Hosting version is 2.0 or higher. This will affect what DiagnosticSource events we receive.
        /// To support AspNetCore 1.0 and 2.0, we listen to both old and new events.
        /// If the running AspNetCore version is 2.0, both old and new events will be sent. In this case, we will ignore the old events.
        /// </summary>
        public static bool IsAspNetCore20 = typeof(WebHostBuilder).GetTypeInfo().Assembly.GetName().Version.Major >= 2;

        private readonly TelemetryClient client;
        private readonly IApplicationIdProvider applicationIdProvider;
        private readonly string sdkVersion = SdkVersionUtils.GetVersion();
        private readonly bool injectResponseHeaders;
        private readonly bool trackExceptions;
        private readonly bool enableW3CHeaders;
        private const string ActivityCreatedByHostingDiagnosticListener = "ActivityCreatedByHostingDiagnosticListener";

        /// <summary>
        /// Initializes a new instance of the <see cref="T:HostingDiagnosticListener"/> class.
        /// </summary>
        /// <param name="client"><see cref="TelemetryClient"/> to post traces to.</param>
        /// <param name="applicationIdProvider">Provider for resolving application Id to be used in multiple instruemntation keys scenarios.</param>
        /// <param name="injectResponseHeaders">Flag that indicates that response headers should be injected.</param>
        /// <param name="trackExceptions">Flag that indicates that exceptions should be tracked.</param>
        /// <param name="enableW3CHeaders">Flag that indicates that W3C header parsing should be enabled.</param>
        public HostingDiagnosticListener(TelemetryClient client, IApplicationIdProvider applicationIdProvider, bool injectResponseHeaders, bool trackExceptions, bool enableW3CHeaders)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            this.applicationIdProvider = applicationIdProvider;
            this.injectResponseHeaders = injectResponseHeaders;
            this.trackExceptions = trackExceptions;
            this.enableW3CHeaders = enableW3CHeaders;
        }

        /// <inheritdoc/>
        public string ListenerName { get; } = "Microsoft.AspNetCore";

        /// <summary>
        /// Diagnostic event handler method for 'Microsoft.AspNetCore.Hosting.HttpRequestIn' event.
        /// </summary>
        [DiagnosticName("Microsoft.AspNetCore.Hosting.HttpRequestIn")]
        public void OnHttpRequestIn()
        {
            // do nothing, just enable the diagnotic source
        }

        /// <summary>
        /// Diagnostic event handler method for 'Microsoft.AspNetCore.Hosting.HttpRequestIn.Start' event.
        /// </summary>
        [DiagnosticName("Microsoft.AspNetCore.Hosting.HttpRequestIn.Start")]
        public void OnHttpRequestInStart(HttpContext httpContext)
        {
            if (this.client.IsEnabled())
            {
                if (Activity.Current == null)
                {
                    AspNetCoreEventSource.Instance.LogHostingDiagnosticListenerOnHttpRequestInStartActivityNull();
                    return;
                }

                var currentActivity = Activity.Current;
                var isActivityCreatedFromRequestIdHeader = true;
                string sourceAppId = null;

<<<<<<< HEAD
                if (currentActivity.ParentId != null)
=======
                if (this.enableW3CHeaders)
>>>>>>> a758c05... Experimental support for W3C headers
                {
                    isActivityCreatedFromRequestIdHeader = false;
                    var activity = new Activity(ActivityCreatedByHostingDiagnosticListener);

                    SetW3CContext(httpContext.Request.Headers, activity, out sourceAppId);
                    activity.SetParentId(activity.GetTraceId()).Start();
                }
<<<<<<< HEAD
                else if (httpContext.Request.Headers.TryGetValue(RequestResponseHeaders.StandardRootIdHeader, out var xmsRequestRootId))
=======
                else if (currentActivity.ParentId == null &&
                    httpContext.Request.Headers.TryGetValue(RequestResponseHeaders.StandardRootIdHeader, out StringValues alternativeRootIdValues) &&
                    alternativeRootIdValues != StringValues.Empty)
>>>>>>> a758c05... Experimental support for W3C headers
                {
                    isActivityCreatedFromRequestIdHeader = false;
                    var activity = new Activity(ActivityCreatedByHostingDiagnosticListener)
                        .SetParentId(StringUtilities.EnforceMaxLength(alternativeRootIdValues.First(),
                        InjectionGuardConstants.RequestHeaderMaxLength));
                    activity.Start();
<<<<<<< HEAD
                    httpContext.Features.Set(activity);

                    currentActivity = activity;
                }
                else
                {
                    // As a first step in supporting W3C protocol in ApplicationInsights,
                    // we want to generate Activity Ids in the W3C compatible format.
                    // While .NET changes to Activity are pending, we want to ensure trace starts with W3C compatible Id
                    // as early as possible, so that everyone has a chance to upgrade and have compatibility with W3C systems once they arrive.
                    // So if there is no current Activity (i.e. there were no Request-Id header in the incoming request), we'll override ParentId on 
                    // the current Activity by the properly formatted one. This workaround should go away
                    // with W3C support on .NET https://github.com/dotnet/corefx/issues/30331

                    var activity = new Activity(ActivityCreatedByHostingDiagnosticListener);

                    activity.SetParentId(StringUtilities.GenerateTraceId());
                    activity.Start();
                    httpContext.Features.Set(activity);
                    currentActivity = activity;

                    // end of workaround
=======
>>>>>>> a758c05... Experimental support for W3C headers
                }

                var requestTelemetry = InitializeRequestTelemetry(httpContext, currentActivity, isActivityCreatedFromRequestIdHeader, Stopwatch.GetTimestamp());
                if (sourceAppId != null)
                {
                    requestTelemetry.Source = sourceAppId;
                }

                SetAppIdInResponseHeader(httpContext, requestTelemetry);
            }
        }

        /// <summary>
        /// Diagnostic event handler method for 'Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop' event.
        /// </summary>
        [DiagnosticName("Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop")]
        public void OnHttpRequestInStop(HttpContext httpContext)
        {
            EndRequest(httpContext, Stopwatch.GetTimestamp());
        }

        /// <summary>
        /// Diagnostic event handler method for 'Microsoft.AspNetCore.Hosting.BeginRequest' event.
        /// </summary>
        [DiagnosticName("Microsoft.AspNetCore.Hosting.BeginRequest")]
        public void OnBeginRequest(HttpContext httpContext, long timestamp)
        {
            if (this.client.IsEnabled() && !IsAspNetCore20)
            {
                var activity = new Activity(ActivityCreatedByHostingDiagnosticListener);
                var isActivityCreatedFromRequestIdHeader = false;

                string sourceAppId = null;
                IHeaderDictionary requestHeaders = httpContext.Request.Headers;

                if (this.enableW3CHeaders)
                {
                    SetW3CContext(httpContext.Request.Headers, activity, out sourceAppId);
                    // length enforced in TrySetW3CContext
                    activity.SetParentId(activity.GetTraceId());
                }
                else if (requestHeaders.TryGetValue(RequestResponseHeaders.RequestIdHeader, out StringValues requestIdValues) &&
                    requestIdValues != StringValues.Empty)
                {
                    var requestId = StringUtilities.EnforceMaxLength(requestIdValues.First(), InjectionGuardConstants.RequestHeaderMaxLength);
                    isActivityCreatedFromRequestIdHeader = true;
                    activity.SetParentId(requestId);

                    string[] baggage = requestHeaders.GetCommaSeparatedValues(RequestResponseHeaders.CorrelationContextHeader);
                    if (baggage != StringValues.Empty)
                    {
                        foreach (var item in baggage)
                        {
                            NameValueHeaderValue baggageItem;
                            if (NameValueHeaderValue.TryParse(item, out baggageItem))
                            {
                                var itemName = StringUtilities.EnforceMaxLength(baggageItem.Name, InjectionGuardConstants.ContextHeaderKeyMaxLength);
                                var itemValue = StringUtilities.EnforceMaxLength(baggageItem.Value, InjectionGuardConstants.ContextHeaderValueMaxLength);
                                activity.AddBaggage(itemName, itemValue);
                            }
                        }
                    }
                }
                else if (requestHeaders.TryGetValue(RequestResponseHeaders.StandardRootIdHeader, out StringValues alternativeRootIdValues) &&
                         alternativeRootIdValues != StringValues.Empty)
                {
                    string alternativeRootId = StringUtilities.EnforceMaxLength(alternativeRootIdValues.First(), InjectionGuardConstants.RequestHeaderMaxLength);
                    activity.SetParentId(alternativeRootId);
                }
                else
                {
                    // As a first step in supporting W3C protocol in ApplicationInsights,
                    // we want to generate Activity Ids in the W3C compatible format.
                    // While .NET changes to Activity are pending, we want to ensure trace starts with W3C compatible Id
                    // as early as possible, so that everyone has a chance to upgrade and have compatibility with W3C systems once they arrive.
                    // So if there is no current Activity (i.e. there were no Request-Id header in the incoming request), we'll override ParentId on 
                    // the current Activity by the properly formatted one. This workaround should go away
                    // with W3C support on .NET https://github.com/dotnet/corefx/issues/30331

                    activity.SetParentId(StringUtilities.GenerateTraceId());

                    // end of workaround
                }

                activity.Start();
                httpContext.Features.Set(activity);

                var requestTelemetry = InitializeRequestTelemetry(httpContext, activity, isActivityCreatedFromRequestIdHeader, timestamp);
                if (sourceAppId != null)
                {
                    requestTelemetry.Source = sourceAppId;
                }

                SetAppIdInResponseHeader(httpContext, requestTelemetry);
            }
        }

        /// <summary>
        /// Diagnostic event handler method for 'Microsoft.AspNetCore.Hosting.EndRequest' event.
        /// </summary>
        [DiagnosticName("Microsoft.AspNetCore.Hosting.EndRequest")]
        public void OnEndRequest(HttpContext httpContext, long timestamp)
        {
            if (!IsAspNetCore20)
            {
                EndRequest(httpContext, timestamp);
            }
        }

        /// <summary>
        /// Diagnostic event handler method for 'Microsoft.AspNetCore.Hosting.UnhandledException' event.
        /// </summary>
        [DiagnosticName("Microsoft.AspNetCore.Hosting.UnhandledException")]
        public void OnHostingException(HttpContext httpContext, Exception exception)
        {
            this.OnException(httpContext, exception);

            // In AspNetCore 1.0, when an exception is unhandled it will only send the UnhandledException event, but not the EndRequest event, so we need to call EndRequest here.
            // In AspNetCore 2.0, after sending UnhandledException, it will stop the created activity, which will send HttpRequestIn.Stop event, so we will just end the request there.
            if (!IsAspNetCore20)
            {
                this.EndRequest(httpContext, Stopwatch.GetTimestamp());
            }
        }

        /// <summary>
        /// Diagnostic event handler method for 'Microsoft.AspNetCore.Hosting.HandledException' event.
        /// </summary>
        [DiagnosticName("Microsoft.AspNetCore.Diagnostics.HandledException")]
        public void OnDiagnosticsHandledException(HttpContext httpContext, Exception exception)
        {
            this.OnException(httpContext, exception);
        }

        /// <summary>
        /// Diagnostic event handler method for 'Microsoft.AspNetCore.Diagnostics.UnhandledException' event.
        /// </summary>
        [DiagnosticName("Microsoft.AspNetCore.Diagnostics.UnhandledException")]
        public void OnDiagnosticsUnhandledException(HttpContext httpContext, Exception exception)
        {
            this.OnException(httpContext, exception);
        }

        private RequestTelemetry InitializeRequestTelemetry(HttpContext httpContext, Activity activity, bool isActivityCreatedFromRequestIdHeader, long timestamp)
        {
            var requestTelemetry = new RequestTelemetry();

            if (isActivityCreatedFromRequestIdHeader)
            {
                requestTelemetry.Context.Operation.ParentId = activity.ParentId;
            }
            else if (httpContext.Request.Headers.TryGetValue(RequestResponseHeaders.StandardParentIdHeader, out var standardParentId))
            {
                standardParentId = StringUtilities.EnforceMaxLength(standardParentId, InjectionGuardConstants.RequestHeaderMaxLength);
                requestTelemetry.Context.Operation.ParentId = standardParentId;
            }

            requestTelemetry.Context.Operation.Id = activity.RootId;
            requestTelemetry.Id = this.enableW3CHeaders ? activity.GetSpanId() : activity.Id;

            foreach (var prop in activity.Baggage)
            {
                if (!requestTelemetry.Context.Properties.ContainsKey(prop.Key))
                {
                    requestTelemetry.Context.Properties[prop.Key] = prop.Value;
                }
            }

            this.client.Initialize(requestTelemetry);

            // set Source
            string headerCorrelationId = HttpHeadersUtilities.GetRequestContextKeyValue(httpContext.Request.Headers, RequestResponseHeaders.RequestContextSourceKey);

            string applicationId = null;
            // If the source header is present on the incoming request, and it is an external component (not the same ikey as the one used by the current component), populate the source field.
            if (!string.IsNullOrEmpty(headerCorrelationId))
            {
                headerCorrelationId = StringUtilities.EnforceMaxLength(headerCorrelationId, InjectionGuardConstants.AppIdMaxLength);
                if (string.IsNullOrEmpty(requestTelemetry.Context.InstrumentationKey))
                {
                    requestTelemetry.Source = headerCorrelationId;
                }

                else if ((this.applicationIdProvider?.TryGetApplicationId(requestTelemetry.Context.InstrumentationKey, out applicationId) ?? false)
                    && applicationId != headerCorrelationId)
                {
                    requestTelemetry.Source = headerCorrelationId;
                }
            }

            requestTelemetry.Start(timestamp);
            httpContext.Features.Set(requestTelemetry);

            return requestTelemetry;
        }

        private void SetAppIdInResponseHeader(HttpContext httpContext, RequestTelemetry requestTelemetry)
        {
            if (this.injectResponseHeaders)
            {
                IHeaderDictionary responseHeaders = httpContext.Response?.Headers;
                if (responseHeaders != null &&
                    !string.IsNullOrEmpty(requestTelemetry.Context.InstrumentationKey) &&
                    (!responseHeaders.ContainsKey(RequestResponseHeaders.RequestContextHeader) ||
                     HttpHeadersUtilities.ContainsRequestContextKeyValue(responseHeaders,
                         RequestResponseHeaders.RequestContextTargetKey)))
                {
                    string applicationId = null;
                    if (this.applicationIdProvider?.TryGetApplicationId(requestTelemetry.Context.InstrumentationKey,
                            out applicationId) ?? false)
                    {
                        HttpHeadersUtilities.SetRequestContextKeyValue(responseHeaders,
                            RequestResponseHeaders.RequestContextTargetKey, applicationId);
                    }
                }
            }
        }

        private void EndRequest(HttpContext httpContext, long timestamp)
        {
            if (this.client.IsEnabled())
            {
                var telemetry = httpContext?.Features.Get<RequestTelemetry>();

                if (telemetry == null)
                {
                    return;
                }

                telemetry.Stop(timestamp);
                telemetry.ResponseCode = httpContext.Response.StatusCode.ToString(CultureInfo.InvariantCulture);

                var successExitCode = httpContext.Response.StatusCode < 400;
                if (telemetry.Success == null)
                {
                    telemetry.Success = successExitCode;
                }
                else
                {
                    telemetry.Success &= successExitCode;
                }

                if (string.IsNullOrEmpty(telemetry.Name))
                {
                    telemetry.Name = httpContext.Request.Method + " " + httpContext.Request.Path.Value;
                }
                
                telemetry.Url = httpContext.Request.GetUri();
                telemetry.Context.GetInternalContext().SdkVersion = this.sdkVersion;
                this.client.TrackRequest(telemetry);

                var activity = httpContext?.Features.Get<Activity>();
                if (activity != null && activity.OperationName == ActivityCreatedByHostingDiagnosticListener)
                {
                    activity.Stop();
                }
            }
        }

        private void OnException(HttpContext httpContext, Exception exception)
        {
            if (this.trackExceptions && this.client.IsEnabled())
            {
                var telemetry = httpContext?.Features.Get<RequestTelemetry>();
                if (telemetry != null)
                {
                    telemetry.Success = false;
                }

                var exceptionTelemetry = new ExceptionTelemetry(exception);
                exceptionTelemetry.HandledAt = ExceptionHandledAt.Platform;
                exceptionTelemetry.Context.GetInternalContext().SdkVersion = this.sdkVersion;
                this.client.Track(exceptionTelemetry);
            }
        }

        private void SetW3CContext(IHeaderDictionary requestHeaders, Activity activity, out string sourceAppId)
        {
            sourceAppId = null;
            if (requestHeaders.TryGetValue(W3CConstants.TraceParentHeader, out StringValues traceParentValues))
            {
                activity.SetTraceParent(StringUtilities.EnforceMaxLength(traceParentValues.First(), InjectionGuardConstants.TraceParentHeaderMaxLength));
            }
            else
            {
                activity.GenerateW3CContext();
            }

            string[] traceStateValues =
                requestHeaders.GetCommaSeparatedValues(W3CConstants.TraceStateHeader);

            if (traceStateValues != null && traceStateValues.Any())
            {
                var sourceAppIdStr = traceStateValues.FirstOrDefault(s => s.StartsWith("msappid"));
                if (sourceAppIdStr != null)
                {
                    var pair = sourceAppIdStr.Split('=');
                    if (pair.Length == 2)
                    {
                        sourceAppId = pair[1];
                    }
                }

                string traceStateExceptAppId = string.Join(",", traceStateValues.Where(s => !s.StartsWith("msappid")));
                activity.SetTraceState(StringUtilities.EnforceMaxLength(traceStateExceptAppId, InjectionGuardConstants.TraceStateHeaderMaxLength));
            }

            if (!activity.Baggage.Any())
            {
                string[] baggage =
                    requestHeaders.GetCommaSeparatedValues(RequestResponseHeaders.CorrelationContextHeader);
                if (baggage != StringValues.Empty)
                {
                    foreach (var item in baggage)
                    {
                        if (NameValueHeaderValue.TryParse(item, out var baggageItem))
                        {
                            var itemName = StringUtilities.EnforceMaxLength(baggageItem.Name, InjectionGuardConstants.ContextHeaderKeyMaxLength);
                            var itemValue = StringUtilities.EnforceMaxLength(baggageItem.Value, InjectionGuardConstants.ContextHeaderValueMaxLength);
                            activity.AddBaggage(itemName, itemValue);
                        }
                    }
                }
            }
        }
    }
#pragma warning restore 612, 618
}