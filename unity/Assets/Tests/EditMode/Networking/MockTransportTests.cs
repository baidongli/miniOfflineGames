using System;
using System.Collections.Generic;
using MiniGames.Networking.Transport;
using NUnit.Framework;

namespace MiniGames.Tests.Networking
{
    public class MockTransportTests
    {
        [Test]
        public void Discoverer_sees_already_advertising_host()
        {
            var net = new MockNetwork();
            var host = new MockTransport("host", net);
            var client = new MockTransport("client", net);
            host.StartAdvertising("svc", "Host");

            var found = new List<(PeerId, string)>();
            client.EndpointFound += (p, n) => found.Add((p, n));
            client.StartDiscovery("svc");

            Assert.AreEqual(1, found.Count);
            Assert.AreEqual("host", found[0].Item1.Value);
            Assert.AreEqual("Host", found[0].Item2);
        }

        [Test]
        public void Already_discovering_client_sees_new_advertiser()
        {
            var net = new MockNetwork();
            var client = new MockTransport("client", net);
            var host = new MockTransport("host", net);

            var found = new List<string>();
            client.EndpointFound += (p, _) => found.Add(p.Value);
            client.StartDiscovery("svc");
            host.StartAdvertising("svc", "Host");

            Assert.Contains("host", found);
        }

        [Test]
        public void Both_endpoints_get_ConnectionResult_Ok_after_accept_flow()
        {
            var net = new MockNetwork();
            var host = new MockTransport("host", net);
            var client = new MockTransport("client", net);
            host.StartAdvertising("svc", "Host");
            client.StartDiscovery("svc");

            int hostOks = 0, clientOks = 0;
            host.ConnectionResult += (_, s) => { if (s == ConnectionStatus.Ok) hostOks++; };
            client.ConnectionResult += (_, s) => { if (s == ConnectionStatus.Ok) clientOks++; };

            client.RequestConnection(new PeerId("host"));
            host.AcceptConnection(new PeerId("client"));

            Assert.AreEqual(1, hostOks);
            Assert.AreEqual(1, clientOks);
        }

        [Test]
        public void Payload_delivered_after_connection()
        {
            var net = new MockNetwork();
            var host = new MockTransport("host", net);
            var client = new MockTransport("client", net);
            host.StartAdvertising("svc", "Host");
            client.StartDiscovery("svc");
            client.RequestConnection(new PeerId("host"));
            host.AcceptConnection(new PeerId("client"));

            byte[] received = null;
            host.PayloadReceived += (_, p) => {
                received = new byte[p.Count];
                Buffer.BlockCopy(p.Array, p.Offset, received, 0, p.Count);
            };

            client.Send(new PeerId("host"), new ArraySegment<byte>(new byte[] { 7, 8, 9 }), reliable: true);

            CollectionAssert.AreEqual(new byte[] { 7, 8, 9 }, received);
        }

        [Test]
        public void Disconnect_notifies_both_sides()
        {
            var net = new MockNetwork();
            var host = new MockTransport("host", net);
            var client = new MockTransport("client", net);
            host.StartAdvertising("svc", "Host");
            client.StartDiscovery("svc");
            client.RequestConnection(new PeerId("host"));
            host.AcceptConnection(new PeerId("client"));

            bool hostNotified = false, clientNotified = false;
            host.Disconnected += _ => hostNotified = true;
            client.Disconnected += _ => clientNotified = true;

            host.Disconnect(new PeerId("client"));
            Assert.IsTrue(hostNotified);
            Assert.IsTrue(clientNotified);
        }
    }
}
