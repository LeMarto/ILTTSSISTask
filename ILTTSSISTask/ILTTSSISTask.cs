using System;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.SqlServer.Dts.Runtime;

namespace ILTTSSISTask
{
    [DtsTask(
    DisplayName = "Informatica Linear Taskflow Trigger",
    TaskType = "ILTTSSISTask",
    TaskContact = "",
    IconResource = "ILTTSSISTask.ILTTSSISTask.ico",
    RequiredProductLevel = DTSProductLevel.None)]
    public class ILTTSSISTask : Microsoft.SqlServer.Dts.Runtime.Task
    {
        #region Linear Taskflow Name Property
        private string _linearTaskflowName;
        [CategoryAttribute("Linear Taskflow Name")]
        [Description("Name of the Linear Taskflow to be triggered")]
        public string LinearTaskflowName
        {
            get
            {
                return _linearTaskflowName;
            }
            set
            {
                _linearTaskflowName = value;
            }
        }
        #endregion
        #region Username Property
        private string _username;
        [CategoryAttribute("Username")]
        [Description("Username to be used in the login to the informatica login endpoint url")]
        public string Username
        {
            get
            {
                return _username;
            }
            set
            {
                _username = value;
            }
        }
        #endregion
        #region Password Property
        private string _password;
        [CategoryAttribute("Password")]
        [Description("Password to be used in the login to the informatica login endpoint url")]
        public string Password
        {
            get
            {
                return _password;
            }
            set
            {
                _password = value;
            }
        }
        #endregion
        #region Login Endpoint Url
        private string _loginEndpointUrl;
        [CategoryAttribute("Login Endpoint Url")]
        [Description("Url for the Informatica login endpoint")]
        public string LoginEndpoint
        {
            get
            {
                return _loginEndpointUrl;
            }
            set
            {
                _loginEndpointUrl = value;
            }
        }
        #endregion
        private string _jobEndpointUrl;
        private string _sessionId;
        private string _runId;
        private string _taskId;
        private static string FetchValue(string json, string name)
        {
            int start = json.IndexOf(name);
            string sub = json.Substring(start - 1, json.Length - start);
            int doubleQuoteCount = 0;
            StringBuilder value = new StringBuilder();
            foreach (char c in sub)
            {
                if (c == '\"')
                    doubleQuoteCount++;
                else if (doubleQuoteCount > 2 && doubleQuoteCount < 4)
                    value.Append(c);
            }
            return value.ToString();
        }
        public override void InitializeTask(Connections connections, VariableDispenser variableDispenser, IDTSInfoEvents events, IDTSLogging log, EventInfos eventInfos, LogEntryInfos logEntryInfos, ObjectReferenceTracker refTracker)
        {
            base.InitializeTask(connections, variableDispenser, events, log, eventInfos, logEntryInfos, refTracker);
            SetSecurityProtocol();
        }
        public override DTSExecResult Validate(Connections connections, VariableDispenser variableDispenser, IDTSComponentEvents componentEvents, IDTSLogging log)
        {
            if (String.IsNullOrEmpty(_username))
                throw new ApplicationException("Username is mandatory!");

            if (String.IsNullOrEmpty(_password))
                throw new ApplicationException("Password is mandatory!");

            if (String.IsNullOrEmpty(_linearTaskflowName))
                throw new ApplicationException("Linear Taskflow name is mandatory!");

            if (String.IsNullOrEmpty(_loginEndpointUrl))
                throw new ApplicationException("Login Endpoint Url is mandatory!");

            return DTSExecResult.Success;
        }

        private void SetSecurityProtocol()
        {
            ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Tls
                | SecurityProtocolType.Tls11
                | SecurityProtocolType.Tls12
                | SecurityProtocolType.Ssl3;
        }
        private bool LoginToInformatica(IDTSComponentEvents componentEvents)
        {
            StringBuilder requestBodyStringBuilder = new StringBuilder();
            requestBodyStringBuilder.Append("{");
            requestBodyStringBuilder.AppendFormat(" \"@type\":\"login\", \"username\":\"{0}\", \"password\":\"{1}\"", _username, _password);
            requestBodyStringBuilder.Append("}");

            StringContent requestBody = new StringContent(requestBodyStringBuilder.ToString());
            requestBody.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    HttpResponseMessage response = httpClient.PostAsync(_loginEndpointUrl, requestBody).Result;
                    string responseBody = response.Content.ReadAsStringAsync().Result;

                    if (response.IsSuccessStatusCode)
                    {
                        _jobEndpointUrl = FetchValue(responseBody, "serverUrl");
                        _sessionId = FetchValue(responseBody, "icSessionId");

                        bool continueFiring = true;
                        componentEvents.FireInformation(0, "Login Endpoint", String.Format("JobEndpointUrl:\"{0}\", SessionId:\"{1}\"", _jobEndpointUrl, _sessionId), String.Empty, 0, ref continueFiring);
                        return true;
                    }
                    string errorCode = FetchValue(responseBody, "code");
                    string errorDescription = FetchValue(responseBody, "description");

                    componentEvents.FireError(0, "Login Endpoint Error", String.Format("HTTP Status Code:\"{0}\", Error Code:\"{1}\", Error Description:\"{2}\"", response.StatusCode, errorCode, errorDescription), String.Empty, 0);
                }
            }
            catch (Exception e)
            {
                componentEvents.FireError(0, "Exception", String.Format("Message:\"{0}\", Stack Trace:\"{1}\"", e.Message, e.StackTrace), String.Empty, 0);
                return false;
            }
            return false;
        }
        private bool TriggerLinearTaskflow(IDTSComponentEvents componentEvents)
        {
            string jobEndpointUrl = String.Format("{0}/api/v2/job", _jobEndpointUrl);
            StringBuilder requestBodyStringBuilder = new StringBuilder();
            bool continueFiring = true;

            requestBodyStringBuilder.Append("{");
            requestBodyStringBuilder.AppendFormat(" \"@type\":\"job\", \"taskName\":\"{0}\", \"taskType\":\"{1}\"", _linearTaskflowName, "WORKFLOW");
            requestBodyStringBuilder.Append("}");

            StringContent requestBody = new StringContent(requestBodyStringBuilder.ToString());
            requestBody.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            requestBody.Headers.Add("icsessionid", _sessionId);

            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    HttpResponseMessage response = httpClient.PostAsync(jobEndpointUrl, requestBody).Result;
                    string responseBody = response.Content.ReadAsStringAsync().Result;

                    if (response.IsSuccessStatusCode)
                    {
                        _runId = FetchValue(responseBody, "runId");
                        _taskId = FetchValue(responseBody, "taskId");

                        componentEvents.FireInformation(0, "Job Endpoint", String.Format("Run Id:\"{0}\", Task Id:\"{1}\"", _runId, _taskId), String.Empty, 0, ref continueFiring);
                        return true;
                    }
                    
                    string errorCode = FetchValue(responseBody, "code");
                    string errorDescription = FetchValue(responseBody, "description");

                    componentEvents.FireError(0, "Job Endpoint Error", String.Format("HTTP Status Code:\"{0}\", Error Code:\"{1}\", Error Description:\"{2}\"", response.StatusCode, errorCode, errorDescription), String.Empty, 0);
                }
                return false;
            }
            catch (Exception e)
            {
                componentEvents.FireError(0, "Exception", String.Format("Message:\"{0}\", Stack Trace:\"{1}\"", e.Message, e.StackTrace), String.Empty, 0);
                return false;
            }
        }
        public override DTSExecResult Execute(Connections connections, VariableDispenser variableDispenser, IDTSComponentEvents componentEvents, IDTSLogging log, object transaction)
        {
            // Do the base Validation, if result is OK, then we continue
            if (base.Execute(connections, variableDispenser, componentEvents, log, transaction) == DTSExecResult.Failure)
                return DTSExecResult.Failure;

            if (!LoginToInformatica(componentEvents))
                return DTSExecResult.Failure;

            if (!TriggerLinearTaskflow(componentEvents))
                return DTSExecResult.Failure;

            return DTSExecResult.Success;
        }
    }
}
