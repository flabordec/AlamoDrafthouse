using FluentAssertions;
using PushbulletDotNet;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace PushbulletDotNetTests
{
    public class PushbulletDotNetTests
    {
        private readonly Pushbullet _pushbulletApi;
        private readonly ITestOutputHelper _output;

        public PushbulletDotNetTests(ITestOutputHelper output)
        {
            _output = output;
            _pushbulletApi = new Pushbullet("o.ZLqyDvXi0qXlYzdsNT9osfJ35wphrVoR");
        }

        [Fact]
        public async Task Pushbullet_GetDevices_ReturnsDevices()
        {
            var devices = await _pushbulletApi.GetAllDevicesAsync()
            foreach (var device in devices)
            {
                device.Id.Should().NotBeNullOrEmpty();
                _output.WriteLine($"{device.Nickname} {device.Id}");
            }
        }
    }
}
