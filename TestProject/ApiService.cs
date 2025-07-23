using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI.WebControls;

namespace TestProject
{
    public abstract class ApiServiceBaseUrl
    {
        public abstract string Url { get; }
    }

    public abstract class ApiService<T> where T : ApiServiceBaseUrl, new()
    {
        private static class Literals
        {
            private static string FormatMessage(string msg) =>
                (string.IsNullOrEmpty(msg) ? "" : "\n") + msg;
            public static string ServerInteractionError(Exception ex) =>
                "Ошибка взаимодействия с сервером:\n" + GetInnerExceptionErrors(ex);
            public static string GetInnerExceptionErrors(Exception ex)
            {
                var result = FormatMessage(ex.Message);

                if (ex.InnerException != null)
                {
                    result += FormatMessage(GetInnerExceptionErrors(ex.InnerException));
                }

                return result;
            }

            public static string ServerInteractionError(string msg, HttpStatusCode status) =>
                $"Ошибка взаимодействия с сервером ({status.ToString()})" + FormatMessage(msg);
            public static string ServerOperationFaled(string msg, HttpStatusCode status) =>
                $"Не удалось выполнить запрос ({status.ToString()})" + FormatMessage(msg);
            public static string StringIsNull() =>
                $"Не удалось выполнить запрос: входная строка была пустой";

            public static string Error(Exception ex) =>
                "Ошибка" + FormatMessage(ex.Message) + FormatMessage(ex?.InnerException?.Message);
        }

        private IFormatProvider _formatProvider = new CultureInfo("us-US");
        private ApiServiceBaseUrl _baseUrlContainer = new T();
        private static string[] _requestsWithoutActionName = { "GetRequest", "PostRequest", "DeleteRequest", "PutRequest", "PatchRequest" };

        private static string GetCallerTypeAndName()
        {
            StackTrace stackTrace = new StackTrace();
            StackFrame stackFrame = stackTrace.GetFrame(7);
            MethodBase methodBase = stackFrame.GetMethod();

            if (_requestsWithoutActionName.Contains(methodBase.Name))
                return methodBase.DeclaringType.Name;
            else
                return methodBase.DeclaringType.Name + "/" + methodBase.Name;
        }

        private static string[] GetCallerParameterNames()
        {
            StackTrace stackTrace = new StackTrace();
            StackFrame stackFrame = stackTrace.GetFrame(7);
            MethodBase methodBase = stackFrame.GetMethod();

            return methodBase.GetParameters().Select(x => x.Name).ToArray();
        }

        private string GetUrl() =>
            GetBaseUrl() + GetCallerTypeAndName();

        private string GetSlashParams(IComparable[] parameters)
        {
            string stringParams = string.Empty;

            foreach (var param in parameters)
            {
                string stringParam = null;

                if (param is DateTime)
                    stringParam = DateTime.Parse(param.ToString()).ToString(_formatProvider);
                else if (param is string && string.IsNullOrEmpty(param.ToString()))
                    throw new Exception(Literals.StringIsNull());
                else
                    stringParam = param.ToString();

                stringParams += "/" + stringParam;
            }

            return stringParams;
        }

        private string GetQueryParams(Dictionary<string, IComparable> parameters, string url)
        {
            var builder = new UriBuilder(url);
            var query = HttpUtility.ParseQueryString(builder.Query);

            foreach (var param in parameters)
            {
                string stringParam = null;

                if (param.Value is DateTime)
                    stringParam = DateTime.Parse(param.Value.ToString()).ToString(_formatProvider);
                else
                    stringParam = param.Value.ToString();

                query[param.Key] = stringParam;
            }

            builder.Query = query.ToString();
            string resultUrl = builder.ToString();

            return resultUrl;
        }

        private string GetQueryParams(string url, (string name, IComparable value)[] parameters)
        {
            var builder = new UriBuilder(url);
            var query = HttpUtility.ParseQueryString(builder.Query);

            foreach (var param in parameters)
            {
                string stringParam = null;

                if (param.value is DateTime)
                    stringParam = DateTime.Parse(param.value.ToString()).ToString(_formatProvider);
                else
                    stringParam = param.value.ToString();

                query[param.name] = stringParam;
            }

            builder.Query = query.ToString();
            string resultUrl = builder.ToString();

            return resultUrl;
        }

        private string GetQueryParams(string url, IComparable[] parameters)
        {
            var builder = new UriBuilder(url);
            var query = HttpUtility.ParseQueryString(builder.Query);
            var parameterNames = GetCallerParameterNames();

            for (int i = 0; i < parameters.Length; i++)
            {
                var name = parameterNames[i];
                var value = parameters[i];

                string stringParam = null;

                if (value is DateTime)
                    stringParam = DateTime.Parse(value.ToString()).ToString(_formatProvider);
                else
                    stringParam = value?.ToString() ?? default;

                query[name] = stringParam;
            }

            builder.Query = query.ToString();
            string resultUrl = builder.ToString();

            return resultUrl;
        }


        private async Task ShowFaledReason(HttpResponseMessage response)
        {
            var answer = await response.Content.ReadAsStringAsync();

            if ((int)response.StatusCode >= 500)
                MessageBoxService.OutError(Literals.ServerInteractionError(answer, response.StatusCode));
            else
                MessageBoxService.OutInformation(Literals.ServerOperationFaled(answer, response.StatusCode));
        }

        private string GetBaseUrl() => _baseUrlContainer.Url;
        private static string _token;

        public TimeSpan TimeOut { get; set; } = new TimeSpan(0, 20, 0);

        private async Task<HttpClient> GetClient(string Uri)
        {
            if (_token is null)
                _token = await AuthorityObject.GetWebApiToken();

            var client = new HttpClient()
            {
                Timeout = TimeOut,
                BaseAddress = new Uri(Uri)
            };


            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_token}");
            return client;
        }

        protected virtual async Task<T> ResponseAnalysis<T>(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();

            // Проверяем, не является ли ответ HTML
            if (body.TrimStart().StartsWith("<html", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception($"Сервер вернул HTML-страницу с ошибкой: {response.StatusCode} Полный ответ: {body} Повторите попытку операции");
            }

            if (response.IsSuccessStatusCode)
            {
                return !string.IsNullOrWhiteSpace(body)
                    ? JsonConvert.DeserializeObject<T>(body)
                    : default(T);
            }

            var message = "В результате выполнения запроса возникла ошибка: ";
            message += $"{(int)response.StatusCode} {response.ReasonPhrase}";

            if (!string.IsNullOrWhiteSpace(body))
            {
                message += $"\n{body}";

                var error = JsonConvert.DeserializeObject<Error>(body);
                if (error != null)
                    message = error.Message;
            }

            throw new Exception(message);
        }

        protected virtual async Task ResponseAnalysis(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
                return;

            var message = "В результате выполнения запроса возникла ошибка: ";
            message += $"{(int)response.StatusCode} {response.ReasonPhrase}";

            if (!string.IsNullOrWhiteSpace(body))
            {
                message += $"\n{body}";

                var error = JsonConvert.DeserializeObject<Error>(body);
                if (error != null)
                    message = error.Message;
            }

            throw new Exception(message);
        }

        #region Get
        protected async Task<HttpResponseMessage> GetAsync()
        {
            string url = GetUrl();
            HttpResponseMessage response;

            try
            {
                using (var client = await GetClient(url))
                {
                    response = await client.GetAsync(client.BaseAddress);
                }

                if (!response.IsSuccessStatusCode)
                {
                    await ShowFaledReason(response);
                    return default;
                }

                await ResponseAnalysis(response);

                return response;
            }
            catch (HttpRequestException ex)
            {
                MessageBoxService.OutError(
                    Literals.ServerInteractionError(ex));

                return default;
            }
            catch (Exception ex)
            {
                MessageBoxService.OutError(
                    Literals.Error(ex));

                return default;
            }
        }

        protected async Task<T> GetAsync<T>()
        {
            string url = GetUrl();
            HttpResponseMessage response;

            try
            {
                using (var client = await GetClient(url))
                {
                    response = await client.GetAsync(client.BaseAddress);
                }

                if (!response.IsSuccessStatusCode)
                {
                    await ShowFaledReason(response);
                    return default;
                }

                return await ResponseAnalysis<T>(response);

            }
            catch (HttpRequestException ex)
            {
                MessageBoxService.OutError(
                    Literals.ServerInteractionError(ex));

                return default;
            }
            catch (Exception ex)
            {
                MessageBoxService.OutError(
                    Literals.Error(ex));

                return default;
            }
        }

        protected async Task<T> GetAsync<T>(params IComparable[] parameters)

        {
            string url = GetUrl();
            HttpResponseMessage response;

            try
            {
                string stringParams = GetSlashParams(parameters);

                using (var client = await GetClient(url + stringParams))
                {
                    response = await client.GetAsync(client.BaseAddress);
                }

                if (!response.IsSuccessStatusCode)
                {
                    await ShowFaledReason(response);
                    return default;
                }

                return await ResponseAnalysis<T>(response);

            }
            catch (HttpRequestException ex)
            {
                MessageBoxService.OutError(
                    Literals.ServerInteractionError(ex));

                return default;
            }
            catch (Exception ex)
            {
                MessageBoxService.OutError(
                    Literals.Error(ex));

                return default;
            }
        }

        protected async Task<HttpResponseMessage> GetAsync(params IComparable[] parameters)
        {
            string url = GetUrl();
            HttpResponseMessage response;

            try
            {
                string stringParams = GetSlashParams(parameters);

                using (var client = await GetClient(url + stringParams))
                {
                    response = await client.GetAsync(client.BaseAddress);
                }

                if (!response.IsSuccessStatusCode)
                {
                    await ShowFaledReason(response);
                    return default;
                }

                await ResponseAnalysis(response);

                return response;
            }
            catch (HttpRequestException ex)
            {
                MessageBoxService.OutError(
                    Literals.ServerInteractionError(ex));

                return default;
            }
            catch (Exception ex)
            {
                MessageBoxService.OutError(
                    Literals.Error(ex));

                return default;
            }
        }

        protected async Task<HttpResponseMessage> GetQueryAsync(params IComparable[] parameters)
        {
            string url = GetUrl();
            HttpResponseMessage response;

            try
            {
                string fullUrl = GetQueryParams(url, parameters);

                using (var client = await GetClient(fullUrl))
                {
                    response = await client.GetAsync(client.BaseAddress);
                }

                if (!response.IsSuccessStatusCode)
                {
                    await ShowFaledReason(response);
                    return default;
                }

                await ResponseAnalysis(response);

                return response;
            }
            catch (HttpRequestException ex)
            {
                MessageBoxService.OutError(
                    Literals.ServerInteractionError(ex));

                return default;
            }
            catch (Exception ex)
            {
                MessageBoxService.OutError(
                    Literals.Error(ex));

                return default;
            }
        }

        protected async Task<HttpResponseMessage> GetQueryAsync(Dictionary<string, IComparable> parameters)
        {
            string url = GetUrl();
            HttpResponseMessage response;

            try
            {
                string fullUrl = GetQueryParams(parameters, url);

                using (var client = await GetClient(fullUrl))
                {
                    response = await client.GetAsync(client.BaseAddress);
                }

                if (!response.IsSuccessStatusCode)
                {
                    await ShowFaledReason(response);
                    return default;
                }

                await ResponseAnalysis(response);

                return response;
            }
            catch (HttpRequestException ex)
            {
                MessageBoxService.OutError(
                    Literals.ServerInteractionError(ex));

                return default;
            }
            catch (Exception ex)
            {
                MessageBoxService.OutError(
                    Literals.Error(ex));

                return default;
            }
        }

        protected async Task<T> GetAsync<T>(Dictionary<string, IComparable> parameters)
             
        {
            string url = GetUrl();
            HttpResponseMessage response;

            try
            {
                string fullUrl = GetQueryParams(parameters, url);

                using (var client = await GetClient(fullUrl))
                {
                    response = await client.GetAsync(client.BaseAddress);
                }

                if (!response.IsSuccessStatusCode)
                {
                    await ShowFaledReason(response);
                    return default;
                }

                return await ResponseAnalysis<T>(response);

            }
            catch (HttpRequestException ex)
            {
                MessageBoxService.OutError(
                    Literals.ServerInteractionError(ex));

                return default;
            }
            catch (Exception ex)
            {
                MessageBoxService.OutError(
                    Literals.Error(ex));

                return default;
            }
        }

        protected async Task<T> GetQueryAsync<T>(params IComparable[] parameters)
        {
            string url = GetUrl();
            HttpResponseMessage response;

            try
            {
                string fullUrl = GetQueryParams(url, parameters);

                using (var client = await GetClient(fullUrl))
                {
                    response = await client.GetAsync(client.BaseAddress);
                }

                if (!response.IsSuccessStatusCode)
                {
                    await ShowFaledReason(response);
                    return default;
                }

                return await ResponseAnalysis<T>(response);
            }
            catch (HttpRequestException ex)
            {
                MessageBoxService.OutError(
                    Literals.ServerInteractionError(ex));

                return default;
            }
            catch (Exception ex)
            {
                MessageBoxService.OutError(
                    Literals.Error(ex));

                return default;
            }
        }

        protected async Task<T> GetQueryAsync<T>(params (string name, IComparable value)[] parameters)
             
        {
            string url = GetUrl();
            HttpResponseMessage response;

            try
            {
                string fullUrl = GetQueryParams(url, parameters);

                using (var client = await GetClient(fullUrl))
                {
                    response = await client.GetAsync(client.BaseAddress);
                }

                if (!response.IsSuccessStatusCode)
                {
                    await ShowFaledReason(response);
                    return default;
                }

                return await ResponseAnalysis<T>(response);

            }
            catch (HttpRequestException ex)
            {
                MessageBoxService.OutError(
                    Literals.ServerInteractionError(ex));

                return default;
            }
            catch (Exception ex)
            {
                MessageBoxService.OutError(
                    Literals.Error(ex));

                return default;
            }
        }

        #endregion

        #region Post
        protected async Task<HttpResponseMessage> PostAsync(object data = null)
        {
            string url = GetUrl();
            HttpResponseMessage response = null;

            try
            {

                StringContent content = null;

                if (data != null)
                {
                    var json = JsonConvert.SerializeObject(data);
                    content = new StringContent(json, Encoding.UTF8, "application/json");
                }

                using (var client = await GetClient(url))
                {
                    response = await client.PostAsync(client.BaseAddress, content);
                }

                if (!response.IsSuccessStatusCode)
                {
                    await ShowFaledReason(response);
                    return default;
                }

                await ResponseAnalysis(response);

                return response;

            }
            catch (HttpRequestException ex)
            {
                MessageBoxService.OutError(
                    Literals.ServerInteractionError(ex));

                return default;
            }
            catch (Exception ex)
            {
                MessageBoxService.OutError(
                    Literals.Error(ex));

                return default;
            }
        }

        protected async Task<T> PostAsync<T>(object data = null)  
        {
            string url = GetUrl();
            HttpResponseMessage response = null;
            StringContent requestContent = null;

            try
            {

                if (data != null)
                {
                    var json = JsonConvert.SerializeObject(data);
                    requestContent = new StringContent(json, Encoding.UTF8, "application/json");
                }

                using (var client = await GetClient(url))
                {
                    response = await client.PostAsync(client.BaseAddress, requestContent);
                }

                if (!response.IsSuccessStatusCode)
                {
                    await ShowFaledReason(response);
                    return default;
                }

                return await ResponseAnalysis<T>(response);

            }
            catch (HttpRequestException ex)
            {
                MessageBoxService.OutError(
                    Literals.ServerInteractionError(ex));

                return default;
            }
            catch (Exception ex)
            {
                MessageBoxService.OutError(
                    Literals.Error(ex));

                return default;
            }
        }

        protected async Task<T> PostAsync<T>(params IComparable[] parameters)  
        {
            string url = GetUrl();
            HttpResponseMessage response = null;
            StringContent requestContent = null;

            try
            {
                var paramsStr = GetSlashParams(parameters);

                using (var client = await GetClient(url + paramsStr))
                {
                    response = await client.PostAsync(client.BaseAddress, requestContent);
                }

                if (!response.IsSuccessStatusCode)
                {
                    await ShowFaledReason(response);
                    return default;
                }

                return await ResponseAnalysis<T>(response);

            }
            catch (HttpRequestException ex)
            {
                MessageBoxService.OutError(
                    Literals.ServerInteractionError(ex));

                return default;
            }
            catch (Exception ex)
            {
                MessageBoxService.OutError(
                    Literals.Error(ex));

                return default;
            }
        }
        protected async Task<T> PostQueryAsync<T>(params IComparable[] parameters)
        {
            string url = GetUrl();
            HttpResponseMessage response = null;
            StringContent requestContent = null;

            try
            {
                string fullUrl = GetQueryParams(url, parameters);

                using (var client = await GetClient(fullUrl))
                {
                    response = await client.PostAsync(client.BaseAddress, requestContent);
                }

                if (!response.IsSuccessStatusCode)
                {
                    await ShowFaledReason(response);
                    return default;
                }

                return await ResponseAnalysis<T>(response);

            }
            catch (HttpRequestException ex)
            {
                MessageBoxService.OutError(
                    Literals.ServerInteractionError(ex));

                return default;
            }
            catch (Exception ex)
            {
                MessageBoxService.OutError(
                    Literals.Error(ex));

                return default;
            }
        }

        protected async Task<HttpResponseMessage> PostAsync(params IComparable[] parameters)
        {
            string url = GetUrl();
            HttpResponseMessage response = null;

            try
            {
                var paramsStr = GetSlashParams(parameters);
                StringContent content = null;

                using (var client = await GetClient(url + paramsStr))
                {
                    response = await client.PostAsync(client.BaseAddress, content);
                }

                if (!response.IsSuccessStatusCode)
                {
                    await ShowFaledReason(response);
                    return default;
                }

                await ResponseAnalysis(response);

                return response;

            }
            catch (HttpRequestException ex)
            {
                MessageBoxService.OutError(
                    Literals.ServerInteractionError(ex));

                return default;
            }
            catch (Exception ex)
            {
                MessageBoxService.OutError(
                    Literals.Error(ex));

                return default;
            }
        }

        protected async Task<HttpResponseMessage> PostQueryAsync(params IComparable[] parameters)
        {
            string url = GetUrl();
            HttpResponseMessage response = null;

            try
            {
                string fullUrl = GetQueryParams(url, parameters);

                StringContent content = null;

                using (var client = await GetClient(fullUrl))
                {
                    response = await client.PostAsync(client.BaseAddress, content);
                }

                if (!response.IsSuccessStatusCode)
                {
                    await ShowFaledReason(response);
                    return default;
                }

                await ResponseAnalysis(response);

                return response;

            }
            catch (HttpRequestException ex)
            {
                MessageBoxService.OutError(
                    Literals.ServerInteractionError(ex));

                return default;
            }
            catch (Exception ex)
            {
                MessageBoxService.OutError(
                    Literals.Error(ex));

                return default;
            }
        }

        #endregion

        #region Delete

        protected async Task<T> DeleteAsync<T>()
             
        {
            string url = GetUrl();
            HttpResponseMessage response;

            try
            {
                using (var client = await GetClient(url))
                {
                    response = await client.DeleteAsync(client.BaseAddress);
                }

                if (!response.IsSuccessStatusCode)
                {
                    await ShowFaledReason(response);
                    return default;
                }

                return await ResponseAnalysis<T>(response);

            }
            catch (HttpRequestException ex)
            {
                MessageBoxService.OutError(
                    Literals.ServerInteractionError(ex));

                return default;
            }
            catch (Exception ex)
            {
                MessageBoxService.OutError(
                    Literals.Error(ex));

                return default;
            }
        }

        protected async Task<T> DeleteAsync<T>(params IComparable[] parameters)
             
        {
            string url = GetUrl();
            HttpResponseMessage response;

            try
            {
                string stringParams = GetSlashParams(parameters);

                using (var client = await GetClient(url + stringParams))
                {
                    response = await client.DeleteAsync(client.BaseAddress);
                }

                if (!response.IsSuccessStatusCode)
                {
                    await ShowFaledReason(response);
                    return default;
                }

                return await ResponseAnalysis<T>(response);

            }
            catch (HttpRequestException ex)
            {
                MessageBoxService.OutError(
                    Literals.ServerInteractionError(ex));

                return default;
            }
            catch (Exception ex)
            {
                MessageBoxService.OutError(
                    Literals.Error(ex));

                return default;
            }
        }

        protected async Task<T> DeleteQueryAsync<T>(params IComparable[] parameters)

        {
            string url = GetUrl();
            HttpResponseMessage response;

            try
            {
                string fullUrl = GetQueryParams(url, parameters);

                using (var client = await GetClient(fullUrl))
                {
                    response = await client.DeleteAsync(client.BaseAddress);
                }

                if (!response.IsSuccessStatusCode)
                {
                    await ShowFaledReason(response);
                    return default;
                }

                return await ResponseAnalysis<T>(response);

            }
            catch (HttpRequestException ex)
            {
                MessageBoxService.OutError(
                    Literals.ServerInteractionError(ex));

                return default;
            }
            catch (Exception ex)
            {
                MessageBoxService.OutError(
                    Literals.Error(ex));

                return default;
            }
        }

        protected async Task<HttpResponseMessage> DeleteAsync(params IComparable[] parameters)
        {
            string url = GetUrl();
            HttpResponseMessage response;

            try
            {
                string stringParams = GetSlashParams(parameters);

                using (var client = await GetClient(url + stringParams))
                {
                    response = await client.DeleteAsync(client.BaseAddress);
                }

                if (!response.IsSuccessStatusCode)
                {
                    await ShowFaledReason(response);
                    return default;
                }

                await ResponseAnalysis(response);

                return response;
            }
            catch (HttpRequestException ex)
            {
                MessageBoxService.OutError(
                    Literals.ServerInteractionError(ex));

                return default;
            }
            catch (Exception ex)
            {
                MessageBoxService.OutError(
                    Literals.Error(ex));

                return default;
            }
        }

        protected async Task<HttpResponseMessage> DeleteQueryAsync(params IComparable[] parameters)
        {
            string url = GetUrl();
            HttpResponseMessage response;

            try
            {
                string fullUrl = GetQueryParams(url, parameters);

                using (var client = await GetClient(fullUrl))
                {
                    response = await client.DeleteAsync(client.BaseAddress);
                }

                if (!response.IsSuccessStatusCode)
                {
                    await ShowFaledReason(response);
                    return default;
                }

                await ResponseAnalysis(response);

                return response;
            }
            catch (HttpRequestException ex)
            {
                MessageBoxService.OutError(
                    Literals.ServerInteractionError(ex));

                return default;
            }
            catch (Exception ex)
            {
                MessageBoxService.OutError(
                    Literals.Error(ex));

                return default;
            }
        }

        protected async Task<HttpResponseMessage> DeleteAsync()
        {
            string url = GetUrl();
            HttpResponseMessage response;

            try
            {
                using (var client = await GetClient(url))
                {
                    response = await client.DeleteAsync(client.BaseAddress);
                }

                if (!response.IsSuccessStatusCode)
                {
                    await ShowFaledReason(response);
                    return default;
                }

                await ResponseAnalysis(response);

                return response;
            }
            catch (HttpRequestException ex)
            {
                MessageBoxService.OutError(
                    Literals.ServerInteractionError(ex));

                return default;
            }
            catch (Exception ex)
            {
                MessageBoxService.OutError(
                    Literals.Error(ex));

                return default;
            }
        }

        protected async Task<T> DeleteQueryAsync<T>(Dictionary<string, IComparable> parameters)
             
        {
            string url = GetUrl();
            HttpResponseMessage response;

            try
            {
                string fullUrl = GetQueryParams(parameters, url);

                using (var client = await GetClient(fullUrl))
                {
                    response = await client.DeleteAsync(client.BaseAddress);
                }

                if (!response.IsSuccessStatusCode)
                {
                    await ShowFaledReason(response);
                    return default;
                }

                return await ResponseAnalysis<T>(response);

            }
            catch (HttpRequestException ex)
            {
                MessageBoxService.OutError(
                    Literals.ServerInteractionError(ex));

                return default;
            }
            catch (Exception ex)
            {
                MessageBoxService.OutError(
                    Literals.Error(ex));

                return default;
            }
        }

        protected async Task<HttpResponseMessage> DeleteQueryAsync(Dictionary<string, IComparable> parameters)
        {
            string url = GetUrl();
            HttpResponseMessage response;

            try
            {
                string fullUrl = GetQueryParams(parameters, url);

                using (var client = await GetClient(fullUrl))
                {
                    response = await client.DeleteAsync(client.BaseAddress);
                }

                if (!response.IsSuccessStatusCode)
                {
                    await ShowFaledReason(response);
                    return default;
                }

                await ResponseAnalysis(response);

                return response;

            }
            catch (HttpRequestException ex)
            {
                MessageBoxService.OutError(
                    Literals.ServerInteractionError(ex));

                return default;
            }
            catch (Exception ex)
            {
                MessageBoxService.OutError(
                    Literals.Error(ex));

                return default;
            }
        }

        #endregion

        #region Put

        protected async Task<HttpResponseMessage> PutAsync(object data = null)
        {
            string url = GetUrl();
            HttpResponseMessage response = null;

            try
            {

                StringContent content = null;

                if (data != null)
                {
                    var json = JsonConvert.SerializeObject(data);
                    content = new StringContent(json, Encoding.UTF8, "application/json");
                }

                using (var client = await GetClient(url))
                {
                    response = await client.PutAsync(client.BaseAddress, content);
                }

                if (!response.IsSuccessStatusCode)
                {
                    await ShowFaledReason(response);
                    return default;
                }

                await ResponseAnalysis(response);

                return response;

            }
            catch (HttpRequestException ex)
            {
                MessageBoxService.OutError(
                    Literals.ServerInteractionError(ex));

                return default;
            }
            catch (Exception ex)
            {
                MessageBoxService.OutError(
                    Literals.Error(ex));

                return default;
            }
        }

        protected async Task<T> PutAsync<T>(object data = null)  
        {
            string url = GetUrl();
            HttpResponseMessage response = null;
            StringContent requestContent = null;

            try
            {

                if (data != null)
                {
                    var json = JsonConvert.SerializeObject(data);
                    requestContent = new StringContent(json, Encoding.UTF8, "application/json");
                }

                using (var client = await GetClient(url))
                {
                    response = await client.PutAsync(client.BaseAddress, requestContent);
                }

                if (!response.IsSuccessStatusCode)
                {
                    await ShowFaledReason(response);
                    return default;
                }

                return await ResponseAnalysis<T>(response);

            }
            catch (HttpRequestException ex)
            {
                MessageBoxService.OutError(
                    Literals.ServerInteractionError(ex));

                return default;
            }
            catch (Exception ex)
            {
                MessageBoxService.OutError(
                    Literals.Error(ex));

                return default;
            }
        }

        protected async Task<T> PutAsync<T>(params IComparable[] parameters)  
        {
            string url = GetUrl();
            HttpResponseMessage response = null;
            StringContent requestContent = null;

            try
            {
                var paramsStr = GetSlashParams(parameters);

                using (var client = await GetClient(url + paramsStr))
                {
                    response = await client.PutAsync(client.BaseAddress, requestContent);
                }

                if (!response.IsSuccessStatusCode)
                {
                    await ShowFaledReason(response);
                    return default;
                }

                return await ResponseAnalysis<T>(response);

            }
            catch (HttpRequestException ex)
            {
                MessageBoxService.OutError(
                    Literals.ServerInteractionError(ex));

                return default;
            }
            catch (Exception ex)
            {
                MessageBoxService.OutError(
                    Literals.Error(ex));

                return default;
            }
        }

        protected async Task<T> PutQueryAsync<T>(params IComparable[] parameters)
        {
            string url = GetUrl();
            HttpResponseMessage response = null;
            StringContent requestContent = null;

            try
            {
                var fullUrl = GetQueryParams(url, parameters);

                using (var client = await GetClient(fullUrl))
                {
                    response = await client.PutAsync(client.BaseAddress, requestContent);
                }

                if (!response.IsSuccessStatusCode)
                {
                    await ShowFaledReason(response);
                    return default;
                }

                return await ResponseAnalysis<T>(response);

            }
            catch (HttpRequestException ex)
            {
                MessageBoxService.OutError(
                    Literals.ServerInteractionError(ex));

                return default;
            }
            catch (Exception ex)
            {
                MessageBoxService.OutError(
                    Literals.Error(ex));

                return default;
            }
        }

        protected async Task<HttpResponseMessage> PutAsync(params IComparable[] parameters)
        {
            string url = GetUrl();
            HttpResponseMessage response = null;

            try
            {
                var paramsStr = GetSlashParams(parameters);
                StringContent content = null;

                using (var client = await GetClient(url + paramsStr))
                {
                    response = await client.PutAsync(client.BaseAddress, content);
                }

                if (!response.IsSuccessStatusCode)
                {
                    await ShowFaledReason(response);
                    return default;
                }

                await ResponseAnalysis(response);

                return response;

            }
            catch (HttpRequestException ex)
            {
                MessageBoxService.OutError(
                    Literals.ServerInteractionError(ex));

                return default;
            }
            catch (Exception ex)
            {
                MessageBoxService.OutError(
                    Literals.Error(ex));

                return default;
            }
        }

        protected async Task<HttpResponseMessage> PutQueryAsync(params IComparable[] parameters)
        {
            string url = GetUrl();
            HttpResponseMessage response = null;

            try
            {
                var fullUrl = GetQueryParams(url, parameters);
                StringContent content = null;

                using (var client = await GetClient(fullUrl))
                {
                    response = await client.PutAsync(client.BaseAddress, content);
                }

                if (!response.IsSuccessStatusCode)
                {
                    await ShowFaledReason(response);
                    return default;
                }

                await ResponseAnalysis(response);

                return response;

            }
            catch (HttpRequestException ex)
            {
                MessageBoxService.OutError(
                    Literals.ServerInteractionError(ex));

                return default;
            }
            catch (Exception ex)
            {
                MessageBoxService.OutError(
                    Literals.Error(ex));

                return default;
            }
        }

        #endregion

    }
}
