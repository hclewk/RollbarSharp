using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RollbarSharp.Builders;
using RollbarSharp.Serialization;

namespace RollbarSharp
{
    /// <summary>
    /// The Rollbar client. This is where applications will interact
    /// with Rollbar. There shouldn't be any need for them to deal
    /// with any objects aside from this
    /// </summary>
    public static class Rollbar
    {
        public static Configuration Configuration { get; set; }

        /// <summary>
        /// Builds Rollbar requests from <see cref="Exception"/>s or text messages
        /// </summary>
        /// <remarks>This only builds the body of the request, not the whole notice payload</remarks>
        public static DataModelBuilder NoticeBuilder { get; set; }

        static Rollbar()
        {
            Configuration = Configuration.CreateFromAppConfig();
            NoticeBuilder = new DataModelBuilder(Configuration);
        }

        /// <summary>
        /// Sends an exception using the "critical" level
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="title"></param>
        /// <param name="modelAction"></param>
        /// <param name="userParam"></param>
        public static Task SendCriticalException(Exception ex, string title = null, Action<DataModel> modelAction = null, object userParam = null)
        {
            return SendException(ex, title, "critical", modelAction);
        }

        /// <summary>
        /// Sends an exception using the "error" level
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="title"></param>
        /// <param name="modelAction"></param>
        /// <param name="userParam"></param>
        public static Task SendErrorException(Exception ex, string title = null, Action<DataModel> modelAction = null, object userParam = null)
        {
            return SendException(ex, title, "error", modelAction);
        }

        /// <summary>
        /// Sents an exception using the "warning" level
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="title"></param>
        /// <param name="modelAction"></param>
        /// <param name="userParam"></param>
        public static Task SendWarningException(Exception ex, string title = null, Action<DataModel> modelAction = null, object userParam = null)
        {
           return SendException(ex, title, "warning", modelAction);
        }

        /// <summary>
        /// Sends the given <see cref="Exception"/> to Rollbar including
        /// the stack trace. 
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="title"></param>
        /// <param name="level">Default is "error". "critical" and "warning" may also make sense to use.</param>
        /// <param name="modelAction"></param>
        /// <param name="userParam"></param>
        public static Task SendException(Exception ex, string title = null, string level = "error", Action<DataModel> modelAction = null, object userParam = null)
        {
            var notice = NoticeBuilder.CreateExceptionNotice(ex, title, level);
            if (modelAction != null)
            {
                modelAction(notice);
            }
            return Send(notice, userParam);
        }

        /// <summary>
        /// Sends a text notice using the "critical" level
        /// </summary>
        /// <param name="message"></param>
        /// <param name="customData"></param>
        /// <param name="modelAction"></param>
        /// <param name="userParam"></param>
        public static Task SendCriticalMessage(string message, IDictionary<string, object> customData = null, Action<DataModel> modelAction = null, object userParam = null)
        {
            return SendMessage(message, "critical", customData, modelAction, userParam);
        }

        /// <summary>
        /// Sents a text notice using the "error" level
        /// </summary>
        /// <param name="message"></param>
        /// <param name="customData"></param>
        /// <param name="modelAction"></param>
        /// <param name="userParam"></param>
        public static Task SendErrorMessage(string message, IDictionary<string, object> customData = null, Action<DataModel> modelAction = null, object userParam = null)
        {
            return SendMessage(message, "error", customData, modelAction, userParam);
        }

        /// <summary>
        /// Sends a text notice using the "warning" level
        /// </summary>
        /// <param name="message"></param>
        /// <param name="customData"></param>
        /// <param name="modelAction"></param>
        /// <param name="userParam"></param>
        public static Task SendWarningMessage(string message, IDictionary<string, object> customData = null, Action<DataModel> modelAction = null, object userParam = null)
        {
            return SendMessage(message, "warning", customData, modelAction, userParam);
        }

        /// <summary>
        /// Sends a text notice using the "info" level
        /// </summary>
        /// <param name="message"></param>
        /// <param name="customData"></param>
        /// <param name="modelAction"></param>
        /// <param name="userParam"></param>
        public static Task SendInfoMessage(string message, IDictionary<string, object> customData = null, Action<DataModel> modelAction = null, object userParam = null)
        {
            return SendMessage(message, "info", customData, modelAction, userParam);
        }

        /// <summary>
        /// Sends a text notice using the "debug" level
        /// </summary>
        /// <param name="message"></param>
        /// <param name="customData"></param>
        /// <param name="modelAction"></param>
        /// <param name="userParam"></param>
        public static Task SendDebugMessage(string message, IDictionary<string, object> customData = null, Action<DataModel> modelAction = null, object userParam = null)
        {
            return SendMessage(message, "debug", customData, modelAction);
        }

        /// <summary>
        /// Sents a text notice using the given level of severity
        /// </summary>
        /// <param name="message"></param>
        /// <param name="level"></param>
        /// <param name="customData"></param>
        /// <param name="modelAction"></param>
        /// <param name="userParam"></param>
        public static Task SendMessage(string message, string level, IDictionary<string, object> customData = null, Action<DataModel> modelAction = null, object userParam = null)
        {
            var notice = NoticeBuilder.CreateMessageNotice(message, level, customData);
            if (modelAction != null)
            {
                modelAction(notice);
            }
            return Send(notice, userParam);
        }

        public static Task Send(DataModel data, object userParam)
        {
            var payload = new PayloadModel(Configuration.AccessToken, data);
            return HttpPost(payload, userParam);
        }

        /// <summary>
        /// Serialize the given object for transmission
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string Serialize(object data)
        {
            return JsonConvert.SerializeObject(data, Configuration.JsonSettings);
        }

        private static Task HttpPost(PayloadModel payload, object userParam)
        {
            var payloadString = Serialize(payload);
            return HttpPost(payloadString, userParam);
        }

        private static Task HttpPost(string payload, object userParam)
        {
            return Task.Factory.StartNew(() => HttpPostAsync(payload, userParam));
        }

        private static void HttpPostAsync(string payload, object userParam)
        {
            // convert the json payload to bytes for transmission
            var payloadBytes = Encoding.GetEncoding(Configuration.Encoding).GetBytes(payload);

            var request = (HttpWebRequest) WebRequest.Create(Configuration.Endpoint);
            request.ContentType = "application/json";
            request.Method = "POST";
            request.ContentLength = payloadBytes.Length;

            // we need to wrap GetRequestStream() in a try block
            // if the endpoint is unreachable, that exception gets thrown here
            try
            {
                using (var stream = request.GetRequestStream())
                {
                    stream.Write(payloadBytes, 0, payloadBytes.Length);
                    stream.Close();
                }
            }
            catch (Exception ex)
            {
                return;
            }

            // attempt to parse the response. wrap GetResponse() in a try block
            // since WebRequest throws exceptions for HTTP error status codes
            WebResponse response;

            try
            {
                response = request.GetResponse();
            }
            catch (WebException ex)
            {                
                return;
            }
            catch (Exception ex)
            {
                return;
            }
        }
    }
}
