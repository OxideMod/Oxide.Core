using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using uMod.Plugins;

namespace uMod.Libraries
{
    /// <summary>
    /// Request methods for web requests
    /// </summary>
    public enum RequestMethod
    {
        /// <summary>
        /// Requsts deletion of the specified resource
        /// </summary>
        DELETE,

        /// <summary>
        /// Requests data from the specified resource
        /// </summary>
        GET,

        /// <summary>
        /// Applies partial modifications to the specified resource
        /// </summary>
        PATCH,

        /// <summary>
        /// Submits data to be processed to the specified resource
        /// </summary>
        POST,

        /// <summary>
        /// Uploads a representation of the specified URI
        /// </summary>
        PUT,

        /// <summary>
        /// Grabs only HTTP headers and no document body
        /// </summary>
        HEAD
    };

    /// <summary>
    /// The WebRequests library
    /// </summary>
    public class WebRequests : Library
    {
        private static readonly Universal.Universal universal = Interface.uMod.GetLibrary<Universal.Universal>();

        private readonly AutoResetEvent workevent = new AutoResetEvent(false);
        private readonly Queue<WebRequest> queue = new Queue<WebRequest>();
        private readonly Thread workerthread;
        private readonly int maxCompletionPortThreads;
        private readonly int maxWorkerThreads;
        private readonly object syncRoot = new object();

        private bool shutdown;

        /// <summary>
        /// Specifies the default HTTP request timeout in seconds
        /// </summary>
        public static float DefaultTimeout = 30f;

        /// <summary>
        /// Specifies the HTTP request decompression support
        /// </summary>
        public static bool AllowDecompression = false;

        /// <summary>
        /// Represents a single WebRequest instance
        /// </summary>
        public class WebRequest
        {
            /// <summary>
            /// Gets the callback delegate
            /// </summary>
            public Action<int, string> Callback { get; }

            /// <summary>
            /// Gets the v2 callback delegate
            /// </summary>
            public Action<WebResponse> CallbackV2 { get; }

            /// <summary>
            /// Gets or sets the request timeout
            /// </summary>
            public float Timeout { get; set; }

            /// <summary>
            /// Gets or sets the web request method
            /// </summary>
            public string Method { get; set; }

            /// <summary>
            /// Gets the destination URL
            /// </summary>
            public string Url { get; }

            /// <summary>
            /// Gets or sets the request body
            /// </summary>
            public string Body { get; set; }

            /// <summary>
            /// Gets the HTTP response code
            /// </summary>
            public int ResponseCode { get; protected set; }

            /// <summary>
            /// Gets the HTTP response text
            /// </summary>
            public string ResponseText { get; protected set; }

            /// <summary>
            /// Gets the HTTP response object
            /// </summary>
            public WebResponse Response { get; protected set; }

            /// <summary>
            /// Gets the plugin to which this web request belongs, if any
            /// </summary>
            public Plugin Owner { get; protected set; }

            /// <summary>
            /// Gets or sets the web request headers
            /// </summary>
            public Dictionary<string, string> RequestHeaders { get; set; }

            private HttpWebRequest request;
            private WaitHandle waitHandle;
            private RegisteredWaitHandle registeredWaitHandle;
            private Event.Callback<Plugin, PluginManager> removedFromManager;

            /// <summary>
            /// Initializes a new instance of the WebRequest class
            /// </summary>
            /// <param name="url"></param>
            /// <param name="callback"></param>
            /// <param name="owner"></param>
            public WebRequest(string url, Action<int, string> callback, Plugin owner)
            {
                Url = url;
                Callback = callback;
                Owner = owner;
                removedFromManager = Owner?.OnRemovedFromManager.Add(owner_OnRemovedFromManager);
            }

            /// <summary>
            /// Initializes a new instance of the WebRequest class
            /// </summary>
            /// <param name="url"></param>
            /// <param name="owner"></param>
            /// <param name="callback"></param>
            public WebRequest(string url, Plugin owner, Action<WebResponse> callback) : this(url, null, owner)
            {
                CallbackV2 = callback;
            }

            /// <summary>
            /// Used by the worker thread to start the request
            /// </summary>
            public void Start()
            {
                try
                {
                    // Create the web request
                    request = (HttpWebRequest)System.Net.WebRequest.Create(Url);
                    request.Method = Method;
                    request.Credentials = CredentialCache.DefaultCredentials;
                    request.Proxy = null; // Make sure no proxy is set
                    request.KeepAlive = false;
                    request.Timeout = (int)Math.Round((Timeout.Equals(0f) ? DefaultTimeout : Timeout) * 1000f);
                    request.ServicePoint.MaxIdleTime = request.Timeout;
                    request.ServicePoint.Expect100Continue = ServicePointManager.Expect100Continue;
                    request.ServicePoint.ConnectionLimit = ServicePointManager.DefaultConnectionLimit;
#if !NET35 && !NET40
                    request.ServerCertificateValidationCallback = delegate { return true; };
#else
                    ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
#endif
                    request.AutomaticDecompression = AllowDecompression ? DecompressionMethods.GZip | DecompressionMethods.Deflate : DecompressionMethods.None;

                    // Exclude loopback requests and Linux from IP binding for now
                    if (!request.RequestUri.IsLoopback && Environment.OSVersion.Platform != PlatformID.Unix)
                    {
                        request.ServicePoint.BindIPEndPointDelegate = (servicePoint, remoteEndPoint, retryCount) =>
                        {
                            // Try to assign server's assigned IP address, not primary network adapter address
                            return new IPEndPoint(universal.Server.LocalAddress ?? universal.Server.Address, 0); // TODO: Figure out why this doesn't work on Linux
                        };
                    }

                    // Optional request body for POST requests
                    byte[] data = new byte[0];
                    if (Body != null && !request.Method.Equals("HEAD"))
                    {
                        data = Encoding.UTF8.GetBytes(Body);
                        request.ContentLength = data.Length;
                        request.ContentType = "application/x-www-form-urlencoded";
                    }

                    if (RequestHeaders != null)
                    {
                        request.SetRawHeaders(RequestHeaders);
                    }

                    // Perform DNS lookup and connect (blocking)
                    if (data.Length > 0)
                    {
                        request.BeginGetRequestStream(result =>
                        {
                            if (request != null)
                            {
                                try
                                {
                                    // Write request body
                                    using (Stream stream = request.EndGetRequestStream(result))
                                    {
                                        stream.Write(data, 0, data.Length);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    ResponseText = FormatWebException(ex, ResponseText ?? string.Empty);
                                    if (Response == null)
                                    {
                                        Response = new WebResponse(ResponseText,
                                            Uri.TryCreate(Url, UriKind.Absolute, out Uri uri) ? uri : null,
                                            (RequestMethod)Enum.Parse(typeof(RequestMethod), request.Method));
                                    }

                                    request?.Abort();
                                    OnComplete();
                                    return;
                                }

                                WaitForResponse();
                            }
                        }, null);
                    }
                    else
                    {
                        WaitForResponse();
                    }
                }
                catch (Exception ex)
                {
                    ResponseText = FormatWebException(ex, ResponseText ?? string.Empty);
                    string message = $"Web request produced exception (Url: {Url})";
                    if (Owner)
                    {
                        message += $" in '{Owner.Name} v{Owner.Version}' plugin";
                    }

                    Interface.uMod.LogException(message, ex);

                    if (Response == null)
                    {
                        Response = new WebResponse(ResponseText,
                            Uri.TryCreate(Url, UriKind.Absolute, out Uri uri) ? uri : null,
                            (RequestMethod)Enum.Parse(typeof(RequestMethod), request.Method));
                    }

                    request?.Abort();
                    OnComplete();
                }
            }

            private void WaitForResponse()
            {
                IAsyncResult result = request.BeginGetResponse(res =>
                {
                    try
                    {
                        using (HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(res))
                        {
                            Response = new WebResponse(response, Owner);
                            ResponseText = Response.ReadAsString();
                            ResponseCode = Response.StatusCode;
                        }
                    }
                    catch (WebException ex)
                    {
                        ResponseText = FormatWebException(ex, ResponseText ?? string.Empty);
                        HttpWebResponse response = ex.Response as HttpWebResponse;
                        if (response != null)
                        {
                            try
                            {
                                Response = new WebResponse(response, Owner);
                                ResponseCode = Response.StatusCode;
                                ResponseText = Response.ReadAsString();
                            }
                            catch (Exception)
                            {
                                if (Response == null)
                                {
                                    Response = new WebResponse(ResponseText,
                                        Uri.TryCreate(Url, UriKind.Absolute, out Uri uri) ? uri : null,
                                        (RequestMethod)Enum.Parse(typeof(RequestMethod), Method));
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ResponseText = FormatWebException(ex, ResponseText ?? string.Empty);
                        string message = $"Web request produced exception (Url: {Url})";
                        if (Owner)
                        {
                            message += $" in '{Owner.Name} v{Owner.Version}' plugin";
                        }

                        Interface.uMod.LogException(message, ex);
                        if (Response == null)
                        {
                            Response = new WebResponse(ResponseText, Uri.TryCreate(Url,
                                UriKind.Absolute, out Uri uri) ? uri : null,
                                (RequestMethod)Enum.Parse(typeof(RequestMethod), Method));
                        }
                    }

                    if (request != null)
                    {
                        request.Abort();
                        OnComplete();
                    }
                }, null);

                waitHandle = result.AsyncWaitHandle;
                registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(waitHandle, OnTimeout, null, request.Timeout, true);
            }

            private void OnTimeout(object state, bool timedOut)
            {
                if (timedOut)
                {
                    request?.Abort();
                }

                if (Owner != null)
                {
                    Event.Remove(ref removedFromManager);
                    Owner = null;
                }
            }

            private void OnComplete()
            {
                Event.Remove(ref removedFromManager);
                registeredWaitHandle?.Unregister(waitHandle);
                Interface.uMod.NextTick(() =>
                {
                    if (request != null)
                    {
                        request = null;
                        Owner?.TrackStart();
                        try
                        {
                            Callback?.Invoke(ResponseCode, ResponseText);
                            CallbackV2?.Invoke(Response);
                        }
                        catch (Exception ex)
                        {
                            string message = "Web request callback raised an exception";
                            if (Owner && Owner != null)
                            {
                                message += $" in '{Owner.Name} v{Owner.Version}' plugin";
                            }

                            Interface.uMod.LogException(message, ex);
                        }

                        Owner?.TrackEnd();
                        Owner = null;
                    }
                });
            }

            /// <summary>
            /// Called when the owner plugin was unloaded
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="manager"></param>
            private void owner_OnRemovedFromManager(Plugin sender, PluginManager manager)
            {
                if (request != null)
                {
                    HttpWebRequest outstandingRequest = request;
                    request = null;
                    outstandingRequest.Abort();
                }
            }
        }

        /// <summary>
        /// Represents a Response from a WebRequest
        /// </summary>
        public class WebResponse : IDisposable
        {
            // Holds the response data if any
            private MemoryStream responseStream;

            /// <summary>
            /// The Headers from the response
            /// </summary>
            public IDictionary<string, string> Headers { get; protected set; }

            /// <summary>
            /// The Content-Length returned from the response
            /// </summary>
            public virtual long ContentLength => responseStream?.Length ?? 0;

            /// <summary>
            /// Content-Type set by the Content-Type Header of the response
            /// </summary>
            public string ContentType { get; protected set; }

            /// <summary>
            /// Gets the Content-Encoding from the response
            /// </summary>
            public string ContentEncoding { get; protected set; }

            /// <summary>
            /// Gets the status code returned from the responding server
            /// </summary>
            public int StatusCode { get; protected set; }

            /// <summary>
            /// Gets information on the returned status code
            /// </summary>
            public string StatusDescription { get; protected set; }

            /// <summary>
            /// The original method used to get this response
            /// </summary>
            public RequestMethod Method { get; protected set; }

            /// <summary>
            /// Gets the Uri of the responding server
            /// </summary>
            public Uri ResponseUri { get; protected set; }

            /// <summary>
            /// Gets the HTTP protocol version
            /// </summary>
            public VersionNumber ProtocolVersion { get; protected set; }

            protected WebResponse(HttpWebResponse response, Plugin Owner = null)
            {
                // Make sure we aren't creating an empty response
                if (response == null)
                {
                    throw new ArgumentNullException(nameof(response), "A WebResponse cannot be created from an null HttpResponse");
                }

                // Verify the original Request Method
                switch (response.Method.ToUpper())
                {
                    case "DELETE":
                        Method = RequestMethod.DELETE;
                        break;

                    case "GET":
                        Method = RequestMethod.GET;
                        break;

                    case "HEAD":
                        Method = RequestMethod.HEAD;
                        break;

                    case "PATCH":
                        Method = RequestMethod.PATCH;
                        break;

                    case "POST":
                        Method = RequestMethod.POST;
                        break;

                    case "PUT":
                        Method = RequestMethod.PUT;
                        break;

                    default:
                        throw new InvalidOperationException($"Unknown request method was defined '{response.Method}'");
                }

                ContentType = response.ContentType;
                ResponseUri = response.ResponseUri;
                StatusCode = (int)response.StatusCode;
                StatusDescription = response.StatusDescription;
                ProtocolVersion = new VersionNumber(response.ProtocolVersion?.Major ?? 1, response.ProtocolVersion?.Minor ?? 1, response.ProtocolVersion?.Revision ?? 0);
                ContentEncoding = response.ContentEncoding;

                if (response.Headers != null)
                {
                    Headers = new Dictionary<string, string>();

                    for (int h = 0; h < response.Headers.Count; h++)
                    {
                        string key = response.Headers.GetKey(h);
                        string[] values = response.Headers.GetValues(h);

                        if (values != null && values.Length > 0 && !Headers.ContainsKey(key))
                        {
                            Headers.Add(key, string.Join(";", values));
                        }
                    }
                }

                if (Method != RequestMethod.HEAD)
                {
                    try
                    {
                        using (Stream stream = response.GetResponseStream())
                        {
                            responseStream = new MemoryStream();

                            byte[] responseCache = new byte[256];
                            int currentBytes;

                            while (stream != null && (currentBytes = stream.Read(responseCache, 0, 256)) != 0)
                            {
                                responseStream.Write(responseCache, 0, currentBytes);
                                responseCache = new byte[256];
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        string message = $"Web request produced exception (Url: {ResponseUri?.AbsoluteUri})";
                        if (Owner)
                        {
                            message += $" in '{Owner.Name} v{Owner.Version}' plugin";
                        }

                        Interface.uMod.LogException(message, ex);
                    }
                }
            }

            internal WebResponse(System.Net.WebResponse response, Plugin Owner = null) : this((HttpWebResponse)response, Owner)
            {
            }

            internal WebResponse(string errorText, Uri originalUri, RequestMethod method)
            {
                StatusDescription = errorText;
            }

            /// <summary>
            /// Reads the Response in it's raw data
            /// </summary>
            /// <returns></returns>
            public byte[] ReadAsBytes() => ContentLength != 0 ? responseStream.ToArray() : new byte[0];

            /// <summary>
            /// Reads the response as a string
            /// </summary>
            /// <param name="encoding"></param>
            /// <returns></returns>
            public string ReadAsString(Encoding encoding = null) => ContentLength != 0 ? encoding?.GetString(ReadAsBytes()) ?? Encoding.UTF8.GetString(ReadAsBytes()) : null;

            /// <summary>
            /// Converts the response string from Json to a .NET Object
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="encoding"></param>
            /// <returns></returns>
            public T ConvertFromJson<T>(Encoding encoding = null) => ContentLength != 0 ? Utility.ConvertFromJson<T>(ReadAsString(encoding)) : default(T);

            /// <summary>
            /// Safely disposes this object
            /// </summary>
            public virtual void Dispose()
            {
                responseStream?.Dispose();
                responseStream = null;
                Headers?.Clear();
                Headers = null;

                ContentType = null;
                ContentEncoding = null;
                ResponseUri = null;
                StatusDescription = null;
            }
        }

        /// <summary>
        /// Formats given WebException to string
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        public static string FormatWebException(Exception exception, string response)
        {
            if (!string.IsNullOrEmpty(response))
            {
                response += Environment.NewLine;
            }

            response += exception.Message; // TODO: Fix duplicate messages

            if (exception.InnerException != null && !response.Equals(exception.InnerException.Message))
            {
                response = FormatWebException(exception.InnerException, response);
            }

            return response;
        }

        /// <summary>
        /// Initializes a new instance of the WebRequests library
        /// </summary>
        public WebRequests()
        {
            ThreadPool.GetMaxThreads(out maxWorkerThreads, out maxCompletionPortThreads);
            maxCompletionPortThreads = (int)(maxCompletionPortThreads * 0.6);
            maxWorkerThreads = (int)(maxWorkerThreads * 0.75);

            // Start worker thread
            workerthread = new Thread(Worker);
            workerthread.Start();
        }

        /// <summary>
        /// Shuts down the worker thread
        /// </summary>
        public override void Shutdown()
        {
            if (!shutdown)
            {
                shutdown = true;
                workevent.Set();
                Thread.Sleep(250);
                workerthread.Abort();
            }
        }

        /// <summary>
        /// The worker thread method
        /// </summary>
        private void Worker()
        {
            try
            {
                while (!shutdown)
                {
                    int workerThreads, completionPortThreads;
                    ThreadPool.GetAvailableThreads(out workerThreads, out completionPortThreads);
                    if (workerThreads <= maxWorkerThreads || completionPortThreads <= maxCompletionPortThreads)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    WebRequest request = null;
                    lock (syncRoot)
                    {
                        if (queue.Count > 0)
                        {
                            request = queue.Dequeue();
                        }
                    }

                    if (request != null)
                    {
                        request.Start();
                    }
                    else
                    {
                        workevent.WaitOne();
                    }
                }
            }
            catch (Exception ex)
            {
                Interface.uMod.LogException("WebRequests worker: ", ex);
            }
        }

        /// <summary>
        /// Enqueues a DELETE, GET, PATCH, POST, HEAD, or PUT web request
        /// </summary>
        /// <param name="url"></param>
        /// <param name="body"></param>
        /// <param name="callback"></param>
        /// <param name="owner"></param>
        /// <param name="method"></param>
        /// <param name="headers"></param>
        /// <param name="timeout"></param>
        [LibraryFunction("Enqueue")]
        public void Enqueue(string url, string body, Action<int, string> callback, Plugin owner, RequestMethod method = RequestMethod.GET, Dictionary<string, string> headers = null, float timeout = 0f)
        {
            WebRequest request = new WebRequest(url, callback, owner) { Method = method.ToString(), RequestHeaders = headers, Timeout = timeout, Body = body };
            lock (syncRoot)
            {
                queue.Enqueue(request);
            }
            workevent.Set();
        }

        /// <summary>
        /// Enqueues a DELETE, GET, PATCH, POST, HEAD, or PUT web request
        /// </summary>
        /// <param name="url"></param>
        /// <param name="body"></param>
        /// <param name="callback"></param>
        /// <param name="owner"></param>
        /// <param name="method"></param>
        /// <param name="headers"></param>
        /// <param name="timeout"></param>
        [LibraryFunction("EnqueueV2")]
        public void Enqueue(string url, string body, Action<WebResponse> callback, Plugin owner, RequestMethod method = RequestMethod.GET, Dictionary<string, string> headers = null, float timeout = 0f)
        {
            WebRequest request = new WebRequest(url, owner, callback) { Method = method.ToString(), RequestHeaders = headers, Timeout = timeout, Body = body };
            lock (syncRoot)
            {
                queue.Enqueue(request);
            }
            workevent.Set();
        }

        /// <summary>
        /// Returns the current queue length
        /// </summary>
        /// <returns></returns>
        [LibraryFunction("GetQueueLength")]
        public int GetQueueLength() => queue.Count;
    }

    // HttpWebRequest extensions to add raw header support
    public static class HttpWebRequestExtensions
    {
        /// <summary>
        /// Headers that require modification via a property
        /// </summary>
        private static readonly string[] RestrictedHeaders = {
            "Accept",
            "Connection",
            "Content-Length",
            "Content-Type",
            "Date",
            "Expect",
            "Host",
            "If-Modified-Since",
            "Keep-Alive",
            "Proxy-Connection",
            "Range",
            "Referer",
            "Transfer-Encoding",
            "User-Agent"
        };

        /// <summary>
        /// Dictionary of all of the header properties
        /// </summary>
        private static readonly Dictionary<string, PropertyInfo> HeaderProperties = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initialize the HeaderProperties dictionary
        /// </summary>
        static HttpWebRequestExtensions()
        {
            Type type = typeof(HttpWebRequest);
            foreach (string header in RestrictedHeaders)
            {
                HeaderProperties[header] = type.GetProperty(header.Replace("-", ""));
            }
        }

        /// <summary>
        /// Sets raw HTTP request headers
        /// </summary>
        /// <param name="request">Request object</param>
        /// <param name="headers">Dictionary of headers to set</param>
        public static void SetRawHeaders(this WebRequest request, Dictionary<string, string> headers)
        {
            foreach (KeyValuePair<string, string> keyValPair in headers)
            {
                request.SetRawHeader(keyValPair.Key, keyValPair.Value);
            }
        }

        /// <summary>
        /// Sets a raw HTTP request header
        /// </summary>
        /// <param name="request">Request object</param>
        /// <param name="name">Name of the header</param>
        /// <param name="value">Value of the header</param>
        public static void SetRawHeader(this WebRequest request, string name, string value)
        {
            if (HeaderProperties.ContainsKey(name))
            {
                PropertyInfo property = HeaderProperties[name];
                if (property.PropertyType == typeof(DateTime))
                {
                    property.SetValue(request, DateTime.Parse(value), null);
                }
                else if (property.PropertyType == typeof(bool))
                {
                    property.SetValue(request, bool.Parse(value), null);
                }
                else if (property.PropertyType == typeof(long))
                {
                    property.SetValue(request, long.Parse(value), null);
                }
                else
                {
                    property.SetValue(request, value, null);
                }
            }
            else
            {
                request.Headers[name] = value;
            }
        }
    }
}
