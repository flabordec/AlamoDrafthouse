using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace PushbulletDotNet
{
    public interface IDevice
    {
        bool Active { get; }
        string Id { get; }
        string Nickname { get; }
    }

    public class Device : IDevice
    {
        public string Id { get; }
        public string Nickname { get; }
        public bool Active { get; }

        public Device(string id, string nickname, bool active)
        {
            Id = id;
            Nickname = nickname;
            Active = active;
        }

        public static Device FromJToken(JToken device)
        {
            return new Device(
                (string)device["iden"],
                (string)device["nickname"],
                (bool)device["active"]);
        }
    }

    public interface IDevicesList
    {
        IDevice GetDeviceById(string id);
        IDevice GetDeviceByNickname(string nickname);
    }

    public class DevicesList : IDevicesList
    {
        private IReadOnlyDictionary<string, IDevice> _devicesById;
        private IReadOnlyDictionary<string, IDevice> _devicesByNickname;

        public DevicesList(IEnumerable<IDevice> devices)
        {
            _devicesById = devices.ToDictionary(d => d.Id);
            _devicesByNickname = devices.ToDictionary(d => d.Nickname);
        }

        public IDevice GetDeviceById(string id) => _devicesById[id];

        public IDevice GetDeviceByNickname(string nickname) => _devicesByNickname[nickname];
    }

    public class Pushbullet
    {
        private readonly object _memberLock = new object();

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly string _authenticationToken;

        private IDevicesList _devices;


        public Pushbullet(string authenticationToken)
        {
            _authenticationToken = authenticationToken;
        }

        private async ValueTask InitializeDevicesAsync()
        {
            if (_devices == null)
            {
                var devices = new List<IDevice>();
                await foreach (var device in GetDevicesAsync())
                {
                    devices.Add(device);
                }
                lock (_memberLock)
                {
                    if (_devices == null)
                    {
                        _devices = new DevicesList(devices);
                    }
                }
            }
        }

        public async ValueTask<IDevice> GetDeviceById(string id)
        {
            await InitializeDevicesAsync();
            return _devices.GetDeviceById(id);
        }

        public async ValueTask<IDevice> GetDeviceByNickname(string nickname)
        {
            await InitializeDevicesAsync();
            return _devices.GetDeviceByNickname(nickname);
        }

        public async IAsyncEnumerable<IDevice> GetDevicesAsync()
        {
            JObject devicesJson = await PushbulletGetAsync("v2/devices");
            foreach (var deviceJson in devicesJson["devices"].Children())
            {
                var device = Device.FromJToken(deviceJson);
                if (device.Active)
                    yield return device;
            }
            yield break;
        }

        public async Task PushNoteAsync(string title, string noteBody, IEnumerable<IDevice> devices)
        {
            var tasks = 
                from device in devices
                select PushNoteAsync(title, noteBody, device);
            await Task.WhenAll(tasks);
        }

        public async Task PushNoteAsync(string title, string noteBody, IDevice device)
        {
            await PushbulletPushAsync(
                "v2/pushes",
                new Dictionary<string, string>()
                {
                    ["type"] = "note",
                    ["title"] = title,
                    ["device_iden"] = device.Id,
                    ["body"] = noteBody,
                });
        }

        public async Task<JObject> PushbulletGetAsync(string method)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authenticationToken);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            Uri baseUri = new Uri("https://api.pushbullet.com");
            Uri methodUri = new Uri(baseUri, method);
            var response = await client.GetAsync(methodUri);

            if (!response.IsSuccessStatusCode)
            {
                logger.Error("Could not GET from '{0}', response: {1}", methodUri, response.StatusCode);
                throw new Exception("Could not push");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            var responseObject = JsonConvert.DeserializeObject<JObject>(responseString);
            return responseObject;
        }

        public async ValueTask<JObject> PushbulletPushAsync(string method, IReadOnlyDictionary<string, string> parameters)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authenticationToken);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            string parametersString = JsonConvert.SerializeObject(parameters);
            Uri baseUri = new Uri("https://api.pushbullet.com");
            Uri methodUri = new Uri(baseUri, method);
            var response = await client.PostAsync(methodUri, new StringContent(parametersString, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                logger.Error("Could not PUSH to '{0}' content '{1}', response: {2}", methodUri, parametersString, response.StatusCode);
                throw new Exception("Could not push");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            var responseDict = JsonConvert.DeserializeObject<JObject>(responseString);
            return responseDict;
        }
    }
}
