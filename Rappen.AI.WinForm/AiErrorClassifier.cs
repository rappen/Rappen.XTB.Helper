using System;
using System.Linq;
using System.Net;
using Rappen.XRM.Helpers.Extensions;

namespace Rappen.AI.WinForm
{
    internal enum AiErrorKind
    {
        Unknown,
        RateLimited,
        TransientUnavailable,
        Authentication,
        Configuration
    }

    internal static class AiErrorClassifier
    {
        internal static AiErrorKind Classify(Exception ex)
        {
            var statusCode = GetStatusCode(ex);
            if (statusCode.HasValue)
            {
                if (statusCode.Value == (int)HttpStatusCode.Unauthorized || statusCode.Value == (int)HttpStatusCode.Forbidden)
                {
                    return AiErrorKind.Authentication;
                }
                if (statusCode.Value == (int)HttpStatusCode.TooManyRequests)
                {
                    return AiErrorKind.RateLimited;
                }
                if (statusCode.Value == 529 || statusCode.Value == (int)HttpStatusCode.RequestTimeout || statusCode.Value == (int)HttpStatusCode.BadGateway ||
                    statusCode.Value == (int)HttpStatusCode.ServiceUnavailable || statusCode.Value == (int)HttpStatusCode.GatewayTimeout)
                {
                    return AiErrorKind.TransientUnavailable;
                }
                if (statusCode.Value == (int)HttpStatusCode.BadRequest || statusCode.Value == (int)HttpStatusCode.NotFound || statusCode.Value == 422)
                {
                    return AiErrorKind.Configuration;
                }
            }

            var text = GetErrorText(ex);
            if (ContainsAny(text, "429", "too many requests", "rate limit", "quota exceeded", "throttle"))
            {
                return AiErrorKind.RateLimited;
            }
            if (ContainsAny(text, "unauthorized", "forbidden", "authentication", "api key", "invalid key", "access denied"))
            {
                return AiErrorKind.Authentication;
            }
            if (ContainsAny(text, "timeout", "timed out", "temporarily unavailable", "service unavailable", "overloaded", "try again later", "connection reset", "network error"))
            {
                return AiErrorKind.TransientUnavailable;
            }
            if (ContainsAny(text, "bad request", "invalid request", "model not found", "deployment not found", "invalid endpoint", "configuration"))
            {
                return AiErrorKind.Configuration;
            }

            return AiErrorKind.Unknown;
        }

        internal static string UserMessage(AiErrorKind errorKind)
        {
            switch (errorKind)
            {
                case AiErrorKind.RateLimited:
                    return "The AI service is receiving too many requests right now. Please wait a moment and try again.";
                case AiErrorKind.TransientUnavailable:
                    return "The AI service is temporarily unavailable. Please try again in a moment.";
                case AiErrorKind.Authentication:
                    return "The AI provider rejected the request. Please verify API key and provider settings.";
                case AiErrorKind.Configuration:
                    return "The AI provider settings appear invalid or incomplete. Please review model, endpoint, and related settings.";
                default:
                    return "The AI request failed. Please try again.";
            }
        }

        private static int? GetStatusCode(Exception ex)
        {
            var allExceptions = new[] { ex }.Concat(ex?.GetAllInnerExceptions() ?? Enumerable.Empty<Exception>());
            foreach (var exception in allExceptions.Where(e => e != null))
            {
                var type = exception.GetType();

                var statusCodeProperty = type.GetProperty("StatusCode");
                if (statusCodeProperty != null)
                {
                    var statusCodeValue = statusCodeProperty.GetValue(exception);
                    if (TryToStatusCode(statusCodeValue, out var statusCode))
                    {
                        return statusCode;
                    }
                }

                var statusProperty = type.GetProperty("Status");
                if (statusProperty != null)
                {
                    var statusValue = statusProperty.GetValue(exception);
                    if (TryToStatusCode(statusValue, out var statusCode))
                    {
                        return statusCode;
                    }
                }
            }

            return null;
        }

        private static bool TryToStatusCode(object value, out int statusCode)
        {
            if (value is HttpStatusCode httpStatusCode)
            {
                statusCode = (int)httpStatusCode;
                return true;
            }
            if (value is int i)
            {
                statusCode = i;
                return true;
            }
            if (value != null && int.TryParse(value.ToString(), out var parsed))
            {
                statusCode = parsed;
                return true;
            }

            statusCode = 0;
            return false;
        }

        private static string GetErrorText(Exception ex)
        {
            var allExceptions = new[] { ex }.Concat(ex?.GetAllInnerExceptions() ?? Enumerable.Empty<Exception>());
            return string.Join(Environment.NewLine, allExceptions
                .Where(e => e != null)
                .Select(e => e.Message));
        }

        private static bool ContainsAny(string text, params string[] matches) =>
            matches.Any(match => text.IndexOf(match, StringComparison.OrdinalIgnoreCase) >= 0);
    }
}
