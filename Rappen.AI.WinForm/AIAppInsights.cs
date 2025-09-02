using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using XrmToolBox.AppCode.AppInsights;
using XrmToolBox.Extensibility;

namespace Rappen.AI.WinForm
{
    //public static class Extensions
    //{
    //    public static string PaddedVersion(this Version version, int majorpad, int minorpad, int buildpad, int revisionpad)
    //    {
    //        return string.Format($"{{0:D{majorpad}}}.{{1:D{minorpad}}}.{{2:D{buildpad}}}.{{3:D{revisionpad}}}", version.Major, version.Minor, version.Build, version.Revision);
    //    }
    //}

    public class AIAiConfig
    {
        /// <summary>
        /// Initializes Application Insights configuration.
        /// When called from a tool, make sure to pass Assembly.GetExecutingAssembly() as loggingassembly parameter!!
        /// </summary>
        /// <param name="endpoint">AppInsights endpoint, usually https://dc.services.visualstudio.com/v2/track</param>
        /// <param name="ikey">Instrumentation Key for the AppInsights instance in the Azure portal</param>
        /// <param name="loggingassembly">Assembly info to include in logging, usually pass Assembly.GetExecutingAssembly()</param>
        /// <param name="toolname">Override name of the tool, defaults to last part of the logging assembly name</param>
        public AIAiConfig(string endpoint, Guid ikey, PluginControlBase tool, string provider, string model)
        {
            Tool = tool;
            var assembly = Assembly.GetExecutingAssembly();

            AiEndpoint = endpoint;
            InstrumentationKey = ikey;
            UserId = InstallationInfo.Instance.InstallationId;
            XTB = GetLastDotPart(Assembly.GetEntryAssembly().GetName().Name);
            Version = Assembly.GetEntryAssembly().GetName().Version.PaddedVersion(1, 4, 2, 2);
            PluginName = Tool.ToolName;
            PluginVersion = assembly.GetName().Version.PaddedVersion(1, 4, 2, 2);
            OperationName = provider;
            OperationId = model;

            // This will disable logging if the calling assembly is compiled with debug configuration
            LogEvents = !assembly.GetCustomAttributes<DebuggableAttribute>().Any(d => d.IsJITTrackingEnabled);
            var isc = new ItSecurityChecker();
            LogEvents = LogEvents && !isc.IsStatisticsCollectDisabled();
        }

        public PluginControlBase Tool;
        public string AiEndpoint { get; }
        public Guid UserId { get; }
        public Guid InstrumentationKey { get; }
        public bool LogEvents { get; set; } = true;
        public string OperationName { get; set; }
        public string OperationId { get; set; }
        public string PluginName { get; set; }
        public string PluginVersion { get; set; }
        public Guid SessionId { get; } = Guid.NewGuid();
        public string XTB { get; set; }
        public string Version { get; set; }

        private static string GetLastDotPart(string identifier)
        {
            return identifier == null ? null : !identifier.Contains(".") ? identifier : identifier.Substring(identifier.LastIndexOf('.') + 1);
        }
    }

    public class AIAppInsights
    {
        private readonly AIAiConfig _aiConfig;
        private int seq = 1;

        /// <summary>
        /// Initializes Application Insights instance.
        /// When called from a tool, make sure to pass Assembly.GetExecutingAssembly() as loggingassembly parameter!!
        /// </summary>
        /// <param name="endpoint">AppInsights endpoint, usually https://dc.services.visualstudio.com/v2/track</param>
        /// <param name="ikey">Instrumentation Key for the AppInsights instance in the Azure portal</param>
        /// <param name="loggingassembly">Assembly info to include in logging, usually pass Assembly.GetExecutingAssembly()</param>
        /// <param name="toolname">Override name of the tool, defaults to last part of the logging assembly name</param>
        public AIAppInsights(PluginControlBase tool, string endpoint, Guid ikey, string provider, string model)
        {
            _aiConfig = new AIAiConfig(endpoint, ikey, tool, provider, model);
        }

        public void WriteEvent(string eventName, double? count = null, double? duration = null, long? tokensout = null, long? tokensin = null, string message = null, Action<string> resultHandler = null)
        {
            _aiConfig.Tool.LogInfo($"{eventName}{(count != null ? $" Count: {count}" : "")}{(duration != null ? $" Duration: {duration}" : "")}");
            //if (!_aiConfig.LogEvents) return;
            var logRequest = GetLogRequest("Event");
            logRequest.Data.BaseData.Name = eventName;
            logRequest.Data.BaseData.Measurements = new AIAiMeasurements
            {
                Count = count,
                Duration = duration,
                TokensOut = tokensout,
                TokensIn = tokensin
            };
            logRequest.Data.BaseData.Properties = new AIAiProperties
            {
                Message = message
            };
            var json = SerializeRequest<AIAiLogRequest>(logRequest);
            SendToAi(json);
        }

        private static string SerializeRequest<T>(object o)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
                serializer.WriteObject(stream, o);
                stream.Position = 0;
                StreamReader reader = new StreamReader(stream);
                string json = reader.ReadToEnd();

                return json;
            }
        }

        private AIAiLogRequest GetLogRequest(string action)
        {
            return new AIAiLogRequest(seq++, _aiConfig, action);
        }

        private async void SendToAi(string json, Action<string> handleresult = null)
        {
            var result = string.Empty;
#if DEBUGx
#else
            try
            {
                using (HttpClient client = HttpHelper.GetHttpClient())
                {
                    var content = new StringContent(json, Encoding.UTF8, "application/x-json-stream");
                    var response = await client.PostAsync(_aiConfig.AiEndpoint, content);

                    if (!response.IsSuccessStatusCode)
                    {
                        result = response.ToString();
                    }
                }
            }
            catch (Exception e)
            {
                result = e.ToString();
            }
#endif
            handleresult?.Invoke(result);
        }
    }

    public class HttpHelper
    {
        public static HttpClient GetHttpClient()
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Connection", "close");

            return client;
        }
    }

    #region DataContracts

    [DataContract]
    public class AIAiBaseData
    {
        [DataMember(Name = "measurements")]
        public AIAiMeasurements Measurements { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "properties")]
        public AIAiProperties Properties { get; set; }
    }

    [DataContract]
    public class AIAiData
    {
        [DataMember(Name = "baseData")]
        public AIAiBaseData BaseData { get; set; }

        [DataMember(Name = "baseType")]
        public string BaseType { get; set; }
    }

    [DataContract]
    public class AIAiLogRequest
    {
        public AIAiLogRequest(int sequence, AIAiConfig aiConfig, string action)
        {
            Name = $"Microsoft.ApplicationInsights.{aiConfig.InstrumentationKey}.{action}";
            Time = $"{DateTime.UtcNow:O}";
            Sequence = sequence.ToString("0000000000");
            InstrumentationKey = aiConfig.InstrumentationKey.ToString();
            Tags = new AIAiTags
            {
                OSVersion = aiConfig.XTB,
                ApplicationVersion = aiConfig.Version,
                ClientType = aiConfig.PluginName,
                ClientModel = aiConfig.PluginVersion,
                UserId = aiConfig.UserId.ToString(),
                SessionId = aiConfig.SessionId.ToString(),
                OperationName = aiConfig.OperationName,
                OperationId = aiConfig.OperationId,
            };
            Data = new AIAiData
            {
                BaseType = $"{action}Data",
                BaseData = new AIAiBaseData()
            };
        }

        [DataMember(Name = "data")]
        public AIAiData Data { get; set; }

        [DataMember(Name = "iKey")]
        public string InstrumentationKey { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "seq")]
        public string Sequence { get; set; }

        [DataMember(Name = "tags")]
        public AIAiTags Tags { get; set; }

        [DataMember(Name = "time")]
        public string Time { get; set; }
    }

    [DataContract]
    public class AIAiMeasurements
    {
        [DataMember(Name = "count")]
        public double? Count { get; set; }

        [DataMember(Name = "duration")]
        public double? Duration { get; set; }

        [DataMember(Name = "tokensout")]
        public long? TokensOut { get; set; }

        [DataMember(Name = "tokensin")]
        public long? TokensIn { get; set; }
    }

    [DataContract]
    public class AIAiProperties
    {
        [DataMember(Name = "pluginName")]
        public string PluginName { get; set; }

        [DataMember(Name = "pluginVersion")]
        public string PluginVersion { get; set; }

        [DataMember(Name = "message")]
        public string Message { get; set; }
    }

    [DataContract]
    public class AIAiTags
    {
        [DataMember(Name = "ai.device.osVersion")]
        public string OSVersion { get; set; }       // XTB

        [DataMember(Name = "ai.application.ver")]
        public string ApplicationVersion { get; set; }  // XTB version

        [DataMember(Name = "ai.device.type")]
        public string ClientType { get; set; }      // Tool

        [DataMember(Name = "ai.device.model")]
        public string ClientModel { get; set; }     // Tool version

        [DataMember(Name = "ai.operation.name")]
        public string OperationName { get; set; }   // Provider

        [DataMember(Name = "ai.operation.id")]
        public string OperationId { get; set; }     // Model

        [DataMember(Name = "ai.session.id")]
        public string SessionId { get; set; } = Guid.NewGuid().ToString();

        [DataMember(Name = "ai.user.id")]
        public string UserId { get; set; }
    }

    #endregion DataContracts
}