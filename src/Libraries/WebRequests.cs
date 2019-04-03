extern alias References;

using References::Newtonsoft.Json;
using References::Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
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
        public class WebRequest : IDisposable
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
            /// Gets or sets the web request synchronicity
            /// </summary>
            public bool Async { get; set; } = true;

            /// <summary>
            /// Gets or sets the request timeout
            /// </summary>
            public float Timeout { get; set; } = DefaultTimeout;

            /// <summary>
            /// Gets or sets the web request method
            /// </summary>
            public string Method { get; set; } = "GET";

            /// <summary>
            /// Gets the destination URL
            /// </summary>
            public string Url { get; }

            /// <summary>
            /// Gets or sets the request body
            /// </summary>
            public string Body { get; set; }

            /// <summary>
            /// Gets or sets the request body
            /// </summary>
            public Dictionary<string, string> Cookies { get; set; }

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

            private Process process;

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
            /// <param name="callback"></param>
            /// <param name="owner"></param>
            public WebRequest(string url, Action<WebResponse> callback, Plugin owner)
            {
                Url = url;
                CallbackV2 = callback;
                Owner = owner;
                removedFromManager = Owner?.OnRemovedFromManager.Add(owner_OnRemovedFromManager);
            }

            /// <summary>
            /// Gets timeout and ensures it is valid
            /// </summary>
            /// <returns></returns>
            private int GetTimeout()
            {
                return (int)Math.Round((Timeout.Equals(0f) ? DefaultTimeout : Timeout));
            }

            /// <summary>
            /// Creates web client process
            /// </summary>
            /// <returns></returns>
            protected Process CreateProcess()
            {
                string decompression = AllowDecompression ? "both" : "none";
                int timeout = GetTimeout();

                process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        WorkingDirectory = Interface.uMod.RootDirectory,
                        FileName = Path.Combine(Interface.uMod.RootDirectory, "WebClient.exe"),
                        Arguments = $"--method={Method} --url=\"{Url}\" --timeout={timeout} --decompression={decompression}",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true
                    }
                };

                if (RequestHeaders != null)
                {
                    foreach (KeyValuePair<string, string> kvp in RequestHeaders)
                    {
                        process.StartInfo.Arguments += $" --header=\"{kvp.Key}:{kvp.Value}\"";
                    }
                }
                if (Cookies != null && Cookies.Count > 0)
                {
                    foreach (KeyValuePair<string, string> kvp in Cookies)
                    {
                        process.StartInfo.Arguments += $" --cookie=\"{kvp.Key}:{kvp.Value}\"";
                    }
                }
                if (!string.IsNullOrEmpty(Body))
                {
                    process.StartInfo.Arguments += $" --body=\"{Body}\"";
                }
                if(Interface.uMod.Config.Options.WebRequests.BindIPEndpoint)
                {
                    IPAddress address = universal.Server.LocalAddress ?? universal.Server.Address;

                    if(!string.IsNullOrEmpty(Interface.uMod.Config.Options.WebRequests.PreferredEndpoint) && !IPAddress.TryParse(Interface.uMod.Config.Options.WebRequests.PreferredEndpoint, out address))
                    {
                        Interface.uMod.LogWarning($"IP Address invalid: Could not parse preferred endpoint ({Interface.uMod.Config.Options.WebRequests.PreferredEndpoint}) from umod.config.json");
                    }
                    
                    process.StartInfo.Arguments += $" --address=\"{address}\"";
                }

                return process;
            }

            /// <summary>
            /// Used by the worker thread to start the request
            /// </summary>
            public void Start()
            {
#if !DEBUG
                if (!WebClient.IsHashValid())
                {
                    Interface.uMod.LogError($"Secure web channel potentially compromised, cancelling web request {Url}");
                    Interface.uMod.NextTick(WebClient.CheckWebClientBinary);
                    return;
                }
#endif
                try
                {
                    using (process = CreateProcess())
                    {
                        StringBuilder responseBuilder = new StringBuilder();
                        StringBuilder errorTextBuilder = new StringBuilder();
                        
                        process.OutputDataReceived += new DataReceivedEventHandler(delegate (object sender, DataReceivedEventArgs e)
                        {
                            responseBuilder.Append(e.Data);
                        });

                        process.ErrorDataReceived += new DataReceivedEventHandler(delegate (object sender, DataReceivedEventArgs e)
                        {
                            errorTextBuilder.Append(e.Data);
                        });

                        process.Start();
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();
                        
                        if (!process.WaitForExit((GetTimeout() + 1) * 1000)) // process timeout must be slightly longer than request timeout
                        {
                            process.Kill();
                            OnTimeout();
                        }

                        process.CancelOutputRead();
                        process.CancelErrorRead();

                        string response = responseBuilder.ToString();
                        string errorText = errorTextBuilder.ToString();

                        if (process.ExitCode == 0) // Success
                        {
                            if (string.IsNullOrEmpty(response))
                            {
                                ResponseText = errorText;
                            }
                            else
                            {
                                JObject jsonObject = JObject.Parse(response);
                                Response = jsonObject.ToObject<WebResponse>();
                                ResponseText = Response.ReadAsString();
                                ResponseCode = Response.StatusCode;
                            }
                        }
                        else
                        {
                            if (process.ExitCode == (int)WebExceptionStatus.Timeout)
                            {
                                OnTimeout();
                            }
                            string exceptionName = Enum.GetName(typeof(WebExceptionStatus), process.ExitCode);
                            string message = $"Web request produced exception {exceptionName} (Url: {Url})";
                            if (Owner)
                            {
                                message += $" in '{Owner.Name} v{Owner.Version}' plugin";
                            }
                            message += Environment.NewLine + response;
                            Interface.uMod.LogError(message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    string message = $"Web client produced exception (Url: {Url})";
                    if (Owner)
                    {
                        message += $" in '{Owner.Name} v{Owner.Version}' plugin";
                    }
                    message += Environment.NewLine + ex.Message + Environment.NewLine + ex.StackTrace;
                    Interface.uMod.LogError(message);
                }
                finally
                {
                    Abort();
                    OnComplete();
                }
            }

            private void Abort()
            {
                if (process != null)
                {
                    try
                    {
                        if (process.Handle != IntPtr.Zero && !process.HasExited)
                        {
                            process.Kill();
                            process = null;
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        process?.Dispose();
                    }
                }
            }

            private void OnTimeout()
            {
                Abort();

                if (Owner != null)
                {
                    Event.Remove(ref removedFromManager);
                    Owner = null;
                }
            }

            private void OnComplete()
            {
                Event.Remove(ref removedFromManager);

                Action onComplete = delegate ()
                {
                    Owner?.TrackStart();
                    try
                    {
                        Callback?.Invoke(ResponseCode, ResponseText);

                        if (Response != null)
                        {
                            CallbackV2?.Invoke(Response);
                        }
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
                };

                if (Async)
                {
                    Interface.uMod.NextTick(onComplete);
                }
                else
                {
                    onComplete.Invoke();
                }
            }

            /// <summary>
            /// Called when the owner plugin was unloaded
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="manager"></param>
            private void owner_OnRemovedFromManager(Plugin sender, PluginManager manager)
            {
                Abort();
            }

            #region IDisposable Support
            private bool disposedValue = false; // To detect redundant calls

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        // Dispose managed state (managed objects) 
                        Cookies?.Clear();
                        RequestHeaders?.Clear();
                        Response?.Dispose();
                        Response = null;

                        process?.Dispose();
                        process = null;
                    }

                    // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                    // TODO: set large fields to null.

                    disposedValue = true;
                }
            }

            void IDisposable.Dispose()
            {
                // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
                Dispose(true);
            }
            #endregion
        }

        /// <summary>
        /// Represents a Response from a WebRequest
        /// </summary>
        public class WebResponse : IDisposable
        {
            /// <summary>
            /// The Headers from the response
            /// </summary>
            [JsonProperty("Headers")]
            public IDictionary<string, string> Headers { get; protected set; }

            /// <summary>
            /// The Content-Length returned from the response
            /// </summary>
            [JsonProperty("ContentLength")]
            public virtual long ContentLength { get; protected set; }

            /// <summary>
            /// Content-Type set by the Content-Type Header of the response
            /// </summary>
            [JsonProperty("ContentType")]
            public string ContentType { get; protected set; }

            /// <summary>
            /// Gets the Content-Encoding from the response
            /// </summary>
            [JsonProperty("ContentEncoding")]
            public string ContentEncoding { get; protected set; }

            /// <summary>
            /// Gets the status code returned from the responding server
            /// </summary>
            [JsonProperty("StatusCode")]
            public int StatusCode { get; protected set; }

            /// <summary>
            /// Gets information on the returned status code
            /// </summary>
            [JsonProperty("StatusDescription")]
            public string StatusDescription { get; protected set; }

            /// <summary>
            /// The original method used to get this response
            /// </summary>
            [JsonProperty("Method")]
            public RequestMethod Method { get; protected set; }

            /// <summary>
            /// Gets the Uri of the responding server
            /// </summary>
            [JsonProperty("ResponseUri")]
            public Uri ResponseUri { get; protected set; }

            /// <summary>
            /// Gets the HTTP protocol version
            /// </summary>
            [JsonProperty("ProtocolVersion")]
            public VersionNumber ProtocolVersion { get; protected set; }

            /// <summary>
            /// Gets the response body
            /// </summary>
            [JsonProperty("Body")]
            public byte[] Body { get; protected set; }

            /// <summary>
            /// Reads the Response in it's raw data
            /// </summary>
            /// <returns></returns>
            public byte[] ReadAsBytes() => ContentLength != 0 ? Body : new byte[0];

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
                Headers?.Clear();
                Headers = null;

                ContentType = null;
                ContentEncoding = null;
                ResponseUri = null;
                StatusDescription = null;
                Body = null;
            }
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
                    ThreadPool.GetAvailableThreads(out int workerThreads, out int completionPortThreads);
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
        public void Enqueue(string url, string body, Action<int, string> callback, Plugin owner, RequestMethod method = RequestMethod.GET, Dictionary<string, string> headers = null, float timeout = 30f)
        {
            WebRequest request = new WebRequest(url, callback, owner) { Method = method.ToString(), RequestHeaders = headers, Timeout = timeout, Body = body };
            EnqueueRequest(request);
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
        public void Enqueue(string url, string body, Action<WebResponse> callback, Plugin owner, RequestMethod method = RequestMethod.GET, Dictionary<string, string> headers = null, float timeout = 30f)
        {
            WebRequest request = new WebRequest(url, callback, owner) { Method = method.ToString(), RequestHeaders = headers, Timeout = timeout, Body = body };
            EnqueueRequest(request);
        }

        /// <summary>
        /// Enqueues a web request
        /// </summary>
        /// <param name="request"></param>
        public void EnqueueRequest(WebRequest request)
        {
            if (request.Async)
            {
                lock (syncRoot)
                {
                    queue.Enqueue(request);
                }
                workevent.Set();
            }
            else
            {
                request.Start();
            }
        }

        /// <summary>
        /// Returns the current queue length
        /// </summary>
        /// <returns></returns>
        [LibraryFunction("GetQueueLength")]
        public int GetQueueLength() => queue.Count;
    }
}
