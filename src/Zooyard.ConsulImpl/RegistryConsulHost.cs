using Consul;
using SemVer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Zooyard.Core;

namespace Zooyard.ConsulImpl
{
    public class RegistryConsulHost : IRegistryHost
    {
        public const string NOTES_KEY = "notes";
        public const string INTERVAL_KEY = "interval";
        public const string SERVICEID_KEY = "serviceid";

        private const string VERSION_PREFIX = "version=";

        private readonly URL _configuration;
        private readonly ConsulClient _consul;

        public RegistryConsulHost(string configurationUrl = null)
        {
            _configuration = string.IsNullOrWhiteSpace(configurationUrl) ? URL.valueOf("http://localhost:8500"):URL.valueOf(configurationUrl);

            _consul = new ConsulClient(config =>
            {
                config.Address = new Uri($"http://{_configuration.Host}:{_configuration.Port}");
            });
        }

        private string GetVersionFromStrings(IEnumerable<string> strings)
        {
            return strings?.FirstOrDefault(x => x.StartsWith(VERSION_PREFIX, StringComparison.Ordinal)).TrimStart(VERSION_PREFIX);
        }

        public IList<URL> Find()
        {
            return Find(nameTagsPredicate: x => true, registryInformationPredicate: x => true);
        }

        public IList<URL> Find(string name)
        {
            var queryResult = _consul.Health.Service(name, tag: "", passingOnly: true).GetAwaiter().GetResult();
            var instances = queryResult.Response.Select(serviceEntry => {
                var url = $"{serviceEntry.Service.Address}:{serviceEntry.Service.Port}?id={serviceEntry.Service.ID}&interface={serviceEntry.Service.Service}&{string.Join("&", serviceEntry.Service.Tags)}";
                var result = URL.valueOf(url);
                return result;
            });
            return instances.ToList();
        }

        public IList<URL> FindWithVersion(string name, string version)
        {
            var instances = Find(name);
            var range = new Range(version);

            return instances.Where(x => range.IsSatisfied(x.GetParameter(URL.VERSION_KEY))).ToArray();
        }

        private IDictionary<string, string[]> GetServicesCatalog()
        {
            var queryResult = _consul.Catalog.Services().GetAwaiter().GetResult(); // local agent datacenter is implied
            var services = queryResult.Response;

            return services;
        }

        public IList<URL> Find(Predicate<KeyValuePair<string, string[]>> nameTagsPredicate, Predicate<URL> registryInformationPredicate)
        {
            return (GetServicesCatalog())
                .Where(kvp => nameTagsPredicate(kvp))
                .Select(kvp => kvp.Key)
                .Select(Find)
                .SelectMany(task => task)
                .Where(x => registryInformationPredicate(x))
                .ToList();
        }

        public IList<URL> Find(Predicate<KeyValuePair<string, string[]>> predicate)
        {
            return Find(nameTagsPredicate: predicate, registryInformationPredicate: x => true);
        }

        public IList<URL> Find(Predicate<URL> predicate)
        {
            return Find(nameTagsPredicate: x => true, registryInformationPredicate: predicate);
        }

        public IList<URL> FindAll()
        {
            var queryResult = _consul.Agent.Services().GetAwaiter().GetResult();
            var instances = queryResult.Response.Select(serviceEntry =>
            {

                var url = $"{serviceEntry.Value.Address}:{serviceEntry.Value.Port}?id={serviceEntry.Value.ID}&interface={serviceEntry.Value.Service}&{string.Join("&", serviceEntry.Value.Tags)}";
                var result = URL.valueOf(url);
                return result;
            });

            return instances.ToList();
        }

        //private string GetServiceId(string serviceName, Uri uri)
        //{
        //    var ipAddress = DnsHelper.GetIpAddressAsync().GetAwaiter().GetResult();
        //    return $"{serviceName}_{ipAddress.Replace(".", "_")}_{uri.Port}";
        //}

        public URL RegisterService(URL url)
        {
            var tagList =from item in url.Parameters select $"{item.Key}={item.Value}";

            var registration = new AgentServiceRegistration
            {
                ID = url.ServiceKey, 
                Name = url.ServiceInterface,
                Tags = tagList.ToArray(),
                Address = $"{url.Protocol}://{url.Host}" ,
                Port = url.Port,
                Check = GetAgentServiceCheck(url)
            };

            _consul.Agent.ServiceRegister(registration).GetAwaiter().GetResult();

            var resultUrl = $"{registration.Address}:{registration.Port}?{string.Join("&", tagList)}";
            var result = URL.valueOf(resultUrl);
            return result;
        }
        private AgentServiceCheck GetAgentServiceCheck(URL url)
        {
            var check = $"{url.Protocol}://{url.Host}:{url.Port}/{url.Path}".TrimEnd('/') + "/status";
            return new AgentServiceCheck { HTTP = check, Interval = TimeSpan.FromSeconds(2) };
        }

        public bool DeregisterService(URL url)
        {
            var serviceId = url.ServiceKey;
            var writeResult = _consul.Agent.ServiceDeregister(serviceId).GetAwaiter().GetResult();
            var isSuccess = writeResult.StatusCode == HttpStatusCode.OK;
            //string success = isSuccess ? "succeeded" : "failed";

            return isSuccess;
        }

        private string GetCheckId(string serviceId, URL url)
        {
            return $"{serviceId}_{url.ServiceKey}";
        }

        public string RegisterHealthCheck(URL checkUrl)
        {
            if (checkUrl == null)
            {
                throw new ArgumentNullException(nameof(checkUrl));
            }
            var serviceId = checkUrl.GetParameter(SERVICEID_KEY);
            var checkId = GetCheckId(serviceId, checkUrl);
            var notes = checkUrl.GetParameter(NOTES_KEY);
            TimeSpan? interval = null;
            var intervalPara = checkUrl.GetParameter<double>(INTERVAL_KEY, -1);
            if (intervalPara>0)
            {
                interval = TimeSpan.FromSeconds(intervalPara);
            }
            var checkRegistration = new AgentCheckRegistration
            {
                ID = checkId,
                Name = checkUrl.ServiceInterface,
                Notes = notes,
                ServiceID = serviceId,
                HTTP = checkUrl.ToString(), 
                Interval = interval
            };
            var writeResult = _consul.Agent.CheckRegister(checkRegistration).GetAwaiter().GetResult();
            var isSuccess = writeResult.StatusCode == HttpStatusCode.OK;
            //string success = isSuccess ? "succeeded" : "failed";

            return checkId;
        }

        public bool DeregisterHealthCheck(URL checkUrl)
        {
            var serviceId = checkUrl.GetParameter(SERVICEID_KEY);
            var checkId = GetCheckId(serviceId, checkUrl);
            var writeResult = _consul.Agent.CheckDeregister(checkId).GetAwaiter().GetResult();
            var isSuccess = writeResult.StatusCode == HttpStatusCode.OK;
            //string success = isSuccess ? "succeeded" : "failed";

            return isSuccess;
        }

        public void KeyValuePut(string key, string value)
        {
            var keyValuePair = new KVPair(key) { Value = Encoding.UTF8.GetBytes(value) };
            _consul.KV.Put(keyValuePair).GetAwaiter().GetResult();
        }

        public string KeyValueGet(string key)
        {
            var queryResult = _consul.KV.Get(key).GetAwaiter().GetResult();
            if (queryResult.Response == null)
            {
                return null;
            }

            return Encoding.UTF8.GetString(queryResult.Response.Value);
        }

        public void KeyValueDelete(string key)
        {
            _consul.KV.Delete(key).GetAwaiter().GetResult();
        }

        public void KeyValueDeleteTree(string prefix)
        {
            _consul.KV.DeleteTree(prefix).GetAwaiter().GetResult();
        }

        public string[] KeyValuesGetKeys(string prefix)
        {
            var queryResult = _consul.KV.Keys(prefix ?? "").GetAwaiter().GetResult();
            return queryResult.Response;
        }

    }
}