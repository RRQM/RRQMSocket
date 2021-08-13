//------------------------------------------------------------------------------
//  此代码版权（除特别声明或在RRQMCore.XREF命名空间的代码）归作者本人若汝棋茗所有
//  源代码使用协议遵循本仓库的开源协议及附加协议，若本仓库没有设置，则按MIT开源协议授权
//  CSDN博客：https://blog.csdn.net/qq_40374647
//  哔哩哔哩视频：https://space.bilibili.com/94253567
//  Gitee源代码仓库：https://gitee.com/RRQM_Home
//  Github源代码仓库：https://github.com/RRQM
//  交流QQ群：234762506
//  感谢您的下载和使用
//------------------------------------------------------------------------------
//------------------------------------------------------------------------------
using RRQMCore.ByteManager;
using RRQMCore.Log;
using RRQMCore.XREF.Newtonsoft.Json;
using RRQMCore.XREF.Newtonsoft.Json.Linq;
using RRQMSocket.Http;
using System;
using System.Text;

namespace RRQMSocket.RPC.JsonRpc
{
    /// <summary>
    /// JsonRpcParser解析器
    /// </summary>
    public class JsonRpcParser : TcpService<JsonRpcSocketClient>, IRPCParser
    {
        private ActionMap actionMap;

        private MethodMap methodMap;

        private JsonRpcProtocolType protocolType;

        /// <summary>
        /// 构造函数
        /// </summary>
        public JsonRpcParser()
        {
            this.actionMap = new ActionMap();
        }

        /// <summary>
        /// 函数键映射图
        /// </summary>
        public ActionMap ActionMap { get { return this.actionMap; } }

        private int maxPackageSize;

        /// <summary>
        /// 最大数据包长度
        /// </summary>
        public int MaxPackageSize
        {
            get { return maxPackageSize; }
        }

        /// <summary>
        /// 函数映射
        /// </summary>
        public MethodMap MethodMap
        {
            get { return methodMap; }
        }

        /// <summary>
        /// 协议类型
        /// </summary>
        public JsonRpcProtocolType ProtocolType
        {
            get { return protocolType; }
        }

        /// <summary>
        /// 所属服务器
        /// </summary>
        public RPCService RPCService { get; private set; }

        /// <summary>
        /// 执行函数
        /// </summary>
        public Action<IRPCParser, MethodInvoker, MethodInstance> RRQMExecuteMethod { get; private set; }

        /// <summary>
        /// 结束调用
        /// </summary>
        /// <param name="methodInvoker"></param>
        /// <param name="methodInstance"></param>
        public void OnEndInvoke(MethodInvoker methodInvoker, MethodInstance methodInstance)
        {
            ISocketClient socketClient = (ISocketClient)methodInvoker.Caller;
            error error = new error();

            switch (methodInvoker.Status)
            {
                case InvokeStatus.Success:
                    {
                        error = null;
                        break;
                    }
                case InvokeStatus.UnFound:
                    {
                        error.code = -32601;
                        error.message = "函数未找到";
                        break;
                    }
                case InvokeStatus.UnEnable:
                    {
                        error.code = -32601;
                        error.message = "函数已被禁用";
                        break;
                    }
                case InvokeStatus.Abort:
                    {
                        error.code = -32601;
                        error.message = "函数已被中断执行";
                        break;
                    }
                case InvokeStatus.InvocationException:
                    {
                        error.code = -32603;
                        error.message = "函数内部异常";
                        break;
                    }
                case InvokeStatus.Exception:
                    {
                        error.code = -32602;
                        error.message = methodInvoker.StatusMessage;
                        break;
                    }
            }
            JsonRequestContext jsonRequestContext = (JsonRequestContext)methodInvoker.Flag;
            if (jsonRequestContext.needResponse)
            {
                ByteBlock byteBlock = this.BytePool.GetByteBlock(this.BufferLength);
                this.BuildResponseByteBlock(byteBlock, methodInvoker, jsonRequestContext.id, methodInvoker.ReturnParameter, error);
                if (socketClient.Online)
                {
                    try
                    {
                        string s = Encoding.UTF8.GetString(byteBlock.ToArray());
                        socketClient.Send(byteBlock);
                    }
                    catch (Exception ex)
                    {
                        this.Logger.Debug(LogType.Error, this, ex.Message);
                    }
                    finally
                    {
                        byteBlock.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="methodInstances"></param>
        public void OnRegisterServer(IServerProvider provider, MethodInstance[] methodInstances)
        {
            foreach (var methodInstance in methodInstances)
            {
                foreach (var att in methodInstance.RPCAttributes)
                {
                    if (att is JsonRpcAttribute attribute)
                    {
                        if (methodInstance.IsByRef)
                        {
                            throw new RRQMRPCException($"JsonRpc服务中不允许有out及ref关键字，服务：{methodInstance.Method.Name}");
                        }
                        string actionKey = string.IsNullOrEmpty(attribute.MethodKey) ? methodInstance.Method.Name : attribute.MethodKey;

                        try
                        {
                            this.actionMap.Add(actionKey, methodInstance);
                        }
                        catch
                        {
                            throw new RRQMRPCException($"函数键为{actionKey}的方法已注册。");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 取消注册服务
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="methodInstances"></param>
        public void OnUnregisterServer(IServerProvider provider, MethodInstance[] methodInstances)
        {
        }

        /// <summary>
        /// 设置执行委托
        /// </summary>
        /// <param name="executeMethod"></param>
        public void SetExecuteMethod(Action<IRPCParser, MethodInvoker, MethodInstance> executeMethod)
        {
            this.RRQMExecuteMethod = executeMethod;
        }

        /// <summary>
        /// 设置地图映射
        /// </summary>
        /// <param name="methodMap"></param>
        public void SetMethodMap(MethodMap methodMap)
        {
            this.methodMap = methodMap;
        }

        /// <summary>
        /// 设置服务
        /// </summary>
        /// <param name="service"></param>
        public void SetRPCService(RPCService service)
        {
            this.RPCService = service;
        }

        /// <summary>
        /// 构建请求内容
        /// </summary>
        /// <param name="jsonString">数据</param>
        /// <param name="methodInstance">调用服务实例</param>
        /// <param name="context"></param>
        /// <returns></returns>
        protected virtual void BuildRequestContext(string jsonString, out MethodInstance methodInstance, out JsonRequestContext context)
        {
            try
            {
                context = JsonConvert.DeserializeObject<JsonRequestContext>(jsonString);
                if (context.id != null)
                {
                    context.needResponse = true;
                }
            }
            catch (Exception ex)
            {
                context = new JsonRequestContext();
                context.needResponse = true;
                throw ex;
            }

            if (this.actionMap.TryGet(context.method, out methodInstance))
            {
                if (context.@params == null)
                {
                    if (methodInstance.ParameterNames.Length != 0)
                    {
                        throw new RRQMRPCException("调用参数计数不匹配");
                    }
                    return;
                }
                if (context.@params.GetType() != typeof(JArray))
                {
                    JObject obj = (JObject)context.@params;
                    context.parameters = new object[methodInstance.ParameterNames.Length];
                    //内联
                    for (int i = 0; i < methodInstance.ParameterNames.Length; i++)
                    {
                        if (obj.TryGetValue(methodInstance.ParameterNames[i], out JToken jToken))
                        {
                            Type type = methodInstance.ParameterTypes[i];
                            context.parameters[i] = jToken.ToObject(type);
                        }
                        else if (methodInstance.Parameters[i].HasDefaultValue)
                        {
                            context.parameters[i] = methodInstance.Parameters[i].DefaultValue;
                        }
                        else
                        {
                            throw new RRQMRPCException("调用参数计数不匹配");
                        }
                    }
                }
                else
                {
                    JArray array = (JArray)context.@params;
                    if (array.Count != methodInstance.ParameterNames.Length)
                    {
                        throw new RRQMRPCException("调用参数计数不匹配");
                    }
                    context.parameters = new object[methodInstance.ParameterNames.Length];

                    for (int i = 0; i < array.Count; i++)
                    {
                        context.parameters[i] = context.@params[i].ToObject(methodInstance.ParameterTypes[i]);
                    }
                }
            }
            else
            {
                methodInstance = null;
            }
        }

        /// <summary>
        /// 构建响应数据
        /// </summary>
        /// <param name="responseByteBlock"></param>
        /// <param name="methodInvoker"></param>
        /// <param name="id"></param>
        /// <param name="result"></param>
        /// <param name="error"></param>
        protected virtual void BuildResponseByteBlock(ByteBlock responseByteBlock, MethodInvoker methodInvoker, string id, object result, error error)
        {
            JObject jobject = new JObject();
            if (error == null)
            {
                //成功
                jobject.Add("jsonrpc", JToken.FromObject("2.0"));
                jobject.Add("result", result == null ? null : JToken.FromObject(result));
                jobject.Add("id", id == null ? null : JToken.FromObject(id));
            }
            else
            {
                jobject.Add("jsonrpc", JToken.FromObject("2.0"));
                jobject.Add("error", JToken.FromObject(error));
                jobject.Add("id", id == null ? null : JToken.FromObject(id));
            }
            switch (this.protocolType)
            {
                case JsonRpcProtocolType.Tcp:
                    {
                        responseByteBlock.Write(Encoding.UTF8.GetBytes(jobject.ToString(Formatting.None)));
                        break;
                    }
                case JsonRpcProtocolType.Http:
                    {
                        HttpResponse httpResponse = new HttpResponse();
                        httpResponse.FromJson(jobject.ToString(Formatting.None));
                        httpResponse.Build(responseByteBlock);
                        break;
                    }
            }
        }

        /// <summary>
        /// 载入配置
        /// </summary>
        /// <param name="serviceConfig"></param>
        protected override void LoadConfig(ServiceConfig serviceConfig)
        {
            base.LoadConfig(serviceConfig);
            this.protocolType = (JsonRpcProtocolType)serviceConfig.GetValue(JsonRpcParserConfig.ProtocolTypeProperty);
            this.maxPackageSize = (int)serviceConfig.GetValue(JsonRpcParserConfig.MaxPackageSizeProperty);
        }

        /// <summary>
        /// 创建SocketCliect
        /// </summary>
        /// <param name="socketClient"></param>
        /// <param name="createOption"></param>
        protected override void OnCreateSocketCliect(JsonRpcSocketClient socketClient, CreateOption createOption)
        {
            if (createOption.NewCreate)
            {
                socketClient.OnReceived = this.OnReceived;
            }
            switch (this.protocolType)
            {
                case JsonRpcProtocolType.Tcp:
                    socketClient.SetAdapter(new TerminatorDataHandlingAdapter(this.maxPackageSize, "\r\n"));
                    break;

                case JsonRpcProtocolType.Http:
                    socketClient.SetAdapter(new HttpDataHandlingAdapter(this.maxPackageSize, HttpType.Server));
                    break;
            }
        }

        private void OnReceived(SimpleSocketClient socketClient, ByteBlock byteBlock, object obj)
        {
            MethodInvoker methodInvoker = new MethodInvoker();
            methodInvoker.Caller = socketClient;
            MethodInstance methodInstance = null;
            JsonRequestContext context = null;
            try
            {
                string jsonString = null;
                switch (this.protocolType)
                {
                    case JsonRpcProtocolType.Tcp:
                        {
                            jsonString = Encoding.UTF8.GetString(byteBlock.Buffer, 0, byteBlock.Len);
                            break;
                        }
                    case JsonRpcProtocolType.Http:
                        {
                            HttpRequest httpRequest = (HttpRequest)obj;
                            jsonString = httpRequest.Body;
                            methodInvoker.Flag = httpRequest;
                            break;
                        }
                }
                this.BuildRequestContext(jsonString, out methodInstance, out context);

                if (methodInstance == null)
                {
                    methodInvoker.Status = InvokeStatus.UnFound;
                }
                else if (methodInstance.IsEnable)
                {
                    methodInvoker.Parameters = context.parameters;
                }
                else
                {
                    methodInvoker.Status = InvokeStatus.UnEnable;
                }
            }
            catch (Exception ex)
            {
                methodInvoker.Status = InvokeStatus.Exception;
                methodInvoker.StatusMessage = ex.Message;
            }

            methodInvoker.Flag = context;

            this.RRQMExecuteMethod.Invoke(this, methodInvoker, methodInstance);
        }
    }
}