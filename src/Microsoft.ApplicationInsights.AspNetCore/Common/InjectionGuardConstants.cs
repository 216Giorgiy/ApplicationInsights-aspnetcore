﻿namespace Microsoft.ApplicationInsights.AspNetCore.Common
{
    /// <summary>
    /// These values are listed to guard against malicious injections by limiting the max size allowed in an HTTP Response.
    /// These max limits are intentionally exaggerated to allow for unexpected responses, while still guarding against unreasonably large responses.
    /// Example: While a 32 character response may be expected, 50 characters may be permitted while a 10,000 character response would be unreasonable and malicious.
    /// </summary>
    public static class InjectionGuardConstants
    {
        /// <summary>
        /// Max length of AppId allowed in response from Breeze.
        /// </summary>
        public const int AppIdMaxLength = 50;

        /// <summary>
        /// Max length of incoming Request Header value allowed.
        /// </summary>
        public const int RequestHeaderMaxLength = 1024;

        /// <summary>
        /// Max length of context header key.
        /// </summary>
        public const int ContextHeaderKeyMaxLength = 50;

        /// <summary>
        /// Max length of context header value.
        /// </summary>
        public const int ContextHeaderValueMaxLength = 1024;

        /// <summary>
        /// Max length of traceparent header value.
        /// </summary>
        public const int TraceParentHeaderMaxLength = 55;

        /// <summary>
        /// Max length of tracestate header value.
        /// </summary>
        public const int TraceStateHeaderMaxLength = 512;
    }
}