using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MSX
{
    public class MsxClient
    {
        private const string URL_ROOT = "https://microsoftsales.crm.dynamics.com/api/data/v9.2/";

        private readonly string _cookie;

        public MsxClient(string cookie)
        {
            if (string.IsNullOrWhiteSpace(cookie))
                throw new Exception("Cookie cannot be blank!");
            _cookie = cookie;
        }

        private HttpRequestMessage PrepareRequest()
        {
            var req = new HttpRequestMessage();
            req.Headers.Add("cookie", _cookie);
            return req;
        }

        private async Task<HttpResponseMessage> GetAsync(string url)
        {
            var req = PrepareRequest();
            req.RequestUri = new Uri(url);
            var hc = new HttpClient();
            return await hc.SendAsync(req);
        }

        private async Task<HttpResponseMessage> PostAsync(string url, JObject body)
        {
            var req = PrepareRequest();
            req.Method = HttpMethod.Post;
            req.RequestUri = new Uri(url);
            req.Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json");
            var hc = new HttpClient();
            return await hc.SendAsync(req);
        }

        public async Task<JArray> RunQueryAsync(string odataQuery)
        {
            string url = URL_ROOT + odataQuery;
            var resp = await GetAsync(url);
            string content = await resp.Content.ReadAsStringAsync();
            if (resp.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception($"OData query '{odataQuery}' returned {resp.StatusCode}. Msg: {content}");

            JObject root = JObject.Parse(content);
            JProperty? prop = root.Property("value");
            if (prop != null)
                return (JArray)prop.Value;

            throw new Exception($"Unable to find 'value' property in OData response to query '{odataQuery}'");
        }

        public async Task<string> WhoAmIAsync()
        {
            string url = URL_ROOT + "WhoAmI()";
            var resp = await GetAsync(url);
            string content = await resp.Content.ReadAsStringAsync();
            if (resp.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception($"WhoAmI returned {resp.StatusCode}! Msg: {content}");

            JObject jo = JObject.Parse(content);
            JProperty? prop = jo.Property("UserId");
            if (prop != null)
                return prop.Value.ToString();

            throw new Exception("Unable to find UserId in WhoAmI response.");
        }

        public async Task<JArray> SearchUsersAsync(string fullname)
        {
            string query = $"systemusers?$select=msp_rolesummary,msp_solutionarea,title,msp_qualifier2,internalemailaddress,msp_salesdistrictname,fullname,msp_solutionareadetails&$filter=contains(fullname,'{fullname}')";
            return await RunQueryAsync(query);
        }

        public async Task<JArray> SearchAccountsAsync(string searchTerm)
        {
            string url = URL_ROOT + $"accounts?$filter=contains(name, '{searchTerm}')";
            var resp = await GetAsync(url);
            string content = await resp.Content.ReadAsStringAsync();
            if (resp.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception($"Searching accounts returned {resp.StatusCode}! Msg: {content}");

            JObject jo = JObject.Parse(content);
            JArray source = (JArray)jo["value"]!;
            JArray result = new JArray();
            foreach (JObject account in source)
            {
                result.Add(new JObject
                {
                    ["name"] = account["name"],
                    ["accountid"] = account["accountid"]
                });
            }
            return result;
        }

        public async Task<JArray> SearchOpportunitiesAsync(string accountId, string titleSearch)
        {
            string url = URL_ROOT + $"opportunities?$filter=_parentaccountid_value eq '{accountId}' and statecode eq 0 and contains(name, '{titleSearch}')";
            var resp = await GetAsync(url);
            string content = await resp.Content.ReadAsStringAsync();
            if (resp.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception($"Searching opportunities returned {resp.StatusCode}! Msg: {content}");

            JObject jo = JObject.Parse(content);
            JArray source = (JArray)jo["value"]!;
            JArray result = new JArray();
            foreach (JObject opp in source)
            {
                result.Add(new JObject
                {
                    ["opportunityid"] = opp["opportunityid"],
                    ["name"] = opp["name"],
                    ["description"] = opp["description"],
                    ["estimatedvalue"] = opp["estimatedvalue"]
                });
            }
            return result;
        }

        public async Task CreateTaskAsync(string title, string description, DateTime timestamp, TaskCategory? category = null, string? accountId = null, string? opportunityId = null)
        {
            JObject body = new JObject();
            body.Add("subject", title);
            body.Add("description", description);
            body.Add("scheduledstart", timestamp.ToString("yyyy-MM-ddTHH:mm:ssZ"));

            if (category != null)
                body.Add("msp_taskcategory", (int)category.Value);

            if (accountId != null)
                body.Add("regardingobjectid_account@odata.bind", $"/accounts({accountId})");
            else if (opportunityId != null)
                body.Add("regardingobjectid_opportunity@odata.bind", $"/opportunities({opportunityId})");

            var resp = await PostAsync(URL_ROOT + "tasks", body);
            if (resp.StatusCode != System.Net.HttpStatusCode.NoContent)
            {
                string content = await resp.Content.ReadAsStringAsync();
                throw new Exception($"Creation of task returned {resp.StatusCode}. Msg: {content}");
            }
        }

        public async Task<JArray> GetTasksForUserAsync(string userId, DateTime? from = null, DateTime? to = null)
        {
            DateTime effectiveFrom = from ?? DateTime.UtcNow.AddDays(-30);
            string filter = $"_ownerid_value eq '{userId}' and scheduledstart ge {effectiveFrom:yyyy-MM-dd}";
            if (to != null)
                filter += $" and scheduledstart le {to.Value:yyyy-MM-dd}";

            string url = URL_ROOT + $"tasks?$filter={filter}&$orderby=scheduledstart desc&$expand=regardingobjectid_account($select=name,accountid),regardingobjectid_opportunity($select=name,opportunityid,estimatedvalue)";

            var resp = await GetAsync(url);
            string content = await resp.Content.ReadAsStringAsync();
            if (resp.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception($"Getting recent tasks returned {resp.StatusCode}! Msg: {content}");

            JObject root = JObject.Parse(content);
            JArray tasks = (JArray)root["value"]!;
            JArray result = new JArray();

            foreach (JObject task in tasks)
            {
                JObject summary = new JObject
                {
                    ["subject"] = task["subject"],
                    ["description"] = task["description"],
                    ["scheduledstart"] = task["scheduledstart"]
                };

                JObject? account = task["regardingobjectid_account"] as JObject;
                JObject? opportunity = task["regardingobjectid_opportunity"] as JObject;

                if (account != null && account.HasValues)
                {
                    summary["regarding"] = new JObject
                    {
                        ["type"] = "account",
                        ["name"] = account["name"],
                        ["id"] = account["accountid"]
                    };
                }
                else if (opportunity != null && opportunity.HasValues)
                {
                    summary["regarding"] = new JObject
                    {
                        ["type"] = "opportunity",
                        ["name"] = opportunity["name"],
                        ["id"] = opportunity["opportunityid"],
                        ["value"] = opportunity["estimatedvalue"]?.Type == JTokenType.Null || opportunity["estimatedvalue"] == null
                            ? null
                            : (JToken)(int)Math.Round(opportunity["estimatedvalue"]!.Value<double>())
                    };
                }

                result.Add(summary);
            }

            return result;
        }

        public async Task<JArray> GetAssociatedOpportunitiesAsync(string systemUserId)
        {
            string fetchxml = @"<fetch version=""1.0"" output-format=""xml-platform"" mapping=""logical"" distinct=""true"" no-lock=""false""><entity name=""opportunity""><attribute name=""transactioncurrencyid""/><attribute name=""msp_parentopportunityid""/><attribute name=""name""/><attribute name=""description""/><attribute name=""estimatedvalue""/><attribute name=""estimatedclosedate""/><attribute name=""ownerid""/><attribute name=""createdon""/><attribute name=""parentcontactid""/><attribute name=""parentaccountid""/><attribute name=""msp_partneraccountid""/><attribute name=""msp_recommendationcode""/><attribute name=""msp_licensingprogram""/><attribute name=""msp_opportunitynumber""/><attribute name=""msp_activesalesstage""/><attribute name=""msp_solutionarea""/><attribute name=""msp_salesplay""/><attribute name=""statuscode""/><attribute name=""statecode""/><attribute name=""msp_opportunitytype""/><attribute name=""msp_estcompletiondate""/><attribute name=""msp_engagementstatus""/><attribute name=""msp_consumptionactivedevicesrecurring""/><attribute name=""msp_consumptionactivedevicesnonrecurring""/><attribute name=""msp_consumptionconsumedrecurring""/><attribute name=""msp_consumptionconsumednonrecurring""/><attribute name=""opportunityid""/><attribute name=""msp_riskscore""/><attribute name=""msp_forecastcommentsjsonfield""/><filter type=""and""><condition attribute=""statecode"" operator=""eq"" value=""0""/></filter><link-entity name=""team"" to=""opportunityid"" from=""regardingobjectid"" alias=""aa"" link-type=""inner""><filter type=""and""><condition attribute=""teamtype"" operator=""eq"" value=""1""/><condition attribute=""name"" operator=""like"" value=""%cc923a9d-7651-e311-9405-00155db3ba1e""/></filter><link-entity name=""teammembership"" intersect=""true"" visible=""false"" from=""teamid"" to=""teamid""><link-entity name=""systemuser"" alias=""ab"" from=""systemuserid"" to=""systemuserid""><filter type=""and""><condition attribute=""systemuserid"" operator=""eq"" value=""{PLACEHOLDER_USER_ID}""/></filter></link-entity></link-entity></link-entity><link-entity name=""account"" visible=""false"" to=""parentaccountid"" from=""accountid"" link-type=""outer"" alias=""a_76946cd0245c4349bbb98a1ed211155a""><attribute name=""territoryid""/><attribute name=""address1_country""/><attribute name=""name""/><attribute name=""accountid""/></link-entity><order attribute=""parentaccountid"" descending=""false""/></entity></fetch>";

            fetchxml = fetchxml.Replace("PLACEHOLDER_USER_ID", systemUserId);
            string query = "opportunities?fetchXml=" + Uri.EscapeDataString(fetchxml);

            JArray data = await RunQueryAsync(query);

            string alias = "a_76946cd0245c4349bbb98a1ed211155a";
            JArray result = new JArray();
            foreach (JObject opp in data)
            {
                JObject summary = new JObject
                {
                    ["opportunityid"] = opp["opportunityid"],
                    ["name"] = opp["name"],
                    ["description"] = opp["description"],
                    ["value"] = opp["estimatedvalue"],
                    ["closeDate"] = opp["estimatedclosedate"]
                };

                summary["account"] = new JObject
                {
                    ["id"] = opp[$"{alias}.accountid"],
                    ["name"] = opp[$"{alias}.name"]
                };

                string? forecastJson = opp["msp_forecastcommentsjsonfield"]?.ToString();
                if (!string.IsNullOrEmpty(forecastJson))
                {
                    try { summary["forecastComments"] = JArray.Parse(forecastJson); }
                    catch { summary["forecastComments"] = new JArray(); }
                }
                else
                {
                    summary["forecastComments"] = new JArray();
                }

                result.Add(summary);
            }

            return result;
        }
    }
}
