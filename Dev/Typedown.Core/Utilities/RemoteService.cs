using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace Typedown.Core.Utilities
{
    /// <summary>反馈表单提交的内容。</summary>
    public sealed record FeedbackRequest(int Rating, string? Feedback, string? Contact);

    /// <summary>运行时异常上报。</summary>
    public sealed record ErrorReport(string Version, string System, string Type, string Content);

    /// <summary>反馈 / 上报接口的应答：<see cref="Code"/> 为 0 表示成功，否则 <see cref="Msg"/> 是原因。</summary>
    public sealed record RemoteServiceReply(int? Code, string? Msg);

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true)]
    [JsonSerializable(typeof(FeedbackRequest))]
    [JsonSerializable(typeof(ErrorReport))]
    [JsonSerializable(typeof(RemoteServiceReply))]
    internal sealed partial class RemoteServiceJsonContext : JsonSerializerContext
    {
    }

    /// <summary>Typedown 服务端的反馈与异常上报接口。</summary>
    public static class RemoteService
    {
        private const string FeedbackUrl = "https://typedown.ownbox.cn/feedback";

        private const string ReportUrl = "https://typedown.ownbox.cn/report";

        public static Task<RemoteServiceReply> SubmitFeedbackAsync(FeedbackRequest request) =>
            PostAsync(FeedbackUrl, request, RemoteServiceJsonContext.Default.FeedbackRequest);

        public static Task<RemoteServiceReply> ReportAsync(ErrorReport report) =>
            PostAsync(ReportUrl, report, RemoteServiceJsonContext.Default.ErrorReport);

        private static async Task<RemoteServiceReply> PostAsync<T>(string url, T payload, JsonTypeInfo<T> typeInfo)
        {
            using var client = new HttpClient();
            using var result = await client.PostAsync(url, JsonContent.Create(payload, typeInfo));
            if (result.StatusCode != HttpStatusCode.OK)
                throw new Exception(result.ReasonPhrase);
            return await result.Content.ReadFromJsonAsync(RemoteServiceJsonContext.Default.RemoteServiceReply)
                ?? new RemoteServiceReply(null, null);
        }
    }
}
