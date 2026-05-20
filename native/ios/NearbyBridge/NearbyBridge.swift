//
//  NearbyBridge.swift
//  Bridges Google's NearbyConnections iOS SDK to Unity via a flat C ABI.
//
//  Unity GameObject named "_NearbyTransportReceiver" receives all callbacks
//  through UnityFramework's UnitySendMessageToGOWithName().
//
//  Payload conventions match the Android bridge:
//    - "endpointId|name"           for endpoint-found / connection-initiated
//    - "endpointId|statusCode"     for connection-result (0=Ok 1=Rejected 2=Error)
//    - "endpointId|base64bytes"    for received payloads
//    - "endpointId"                for endpoint-lost / disconnected
//

import Foundation
import NearbyConnections

@objc public final class NearbyBridge: NSObject {

    public static let shared = NearbyBridge()

    private let serviceIdKey = "service"
    private let strategy: Strategy = .cluster

    private var advertiser: Advertiser?
    private var discoverer: Discoverer?
    private var connectionManager: ConnectionManager?
    private var connections: [String: Connection] = [:]

    private let receiver = "_NearbyTransportReceiver"

    private override init() { super.init() }

    // MARK: - Public API

    public func startAdvertising(serviceId: String, displayName: String) {
        let cm = ConnectionManager(serviceID: serviceId, strategy: strategy)
        cm.delegate = self
        connectionManager = cm

        let adv = Advertiser(connectionManager: cm)
        adv.delegate = self
        advertiser = adv
        adv.startAdvertising(using: displayName.data(using: .utf8) ?? Data()) { error in
            if let e = error { NSLog("[Nearby] startAdvertising error: \(e)") }
        }
    }

    public func startDiscovery(serviceId: String) {
        let cm = ConnectionManager(serviceID: serviceId, strategy: strategy)
        cm.delegate = self
        connectionManager = cm

        let disc = Discoverer(connectionManager: cm)
        disc.delegate = self
        discoverer = disc
        disc.startDiscovery { error in
            if let e = error { NSLog("[Nearby] startDiscovery error: \(e)") }
        }
    }

    public func stop() {
        advertiser?.stopAdvertising()
        discoverer?.stopDiscovery()
        connections.values.forEach { $0.disconnect() }
        connections.removeAll()
        advertiser = nil
        discoverer = nil
        connectionManager = nil
    }

    public func requestConnection(endpointId: String) {
        let context = ("local").data(using: .utf8) ?? Data()
        discoverer?.requestConnection(to: endpointId, using: context) { error in
            if let e = error { NSLog("[Nearby] requestConnection error: \(e)") }
        }
    }

    public func acceptConnection(endpointId: String) {
        connectionManager?.acceptConnectionRequest(from: endpointId) { error in
            if let e = error { NSLog("[Nearby] accept error: \(e)") }
        }
    }

    public func rejectConnection(endpointId: String) {
        connectionManager?.rejectConnectionRequest(from: endpointId)
    }

    public func disconnect(endpointId: String) {
        connections[endpointId]?.disconnect()
        connections.removeValue(forKey: endpointId)
    }

    public func sendBytes(endpointId: String, payload: Data, reliable: Bool) {
        guard let conn = connections[endpointId] else { return }
        conn.send(payload)
    }

    // MARK: - Unity callback helper

    private func sendToUnity(method: String, payload: String) {
        UnityFrameworkBridge_SendMessage(receiver, method, payload)
    }
}

// MARK: - AdvertiserDelegate

extension NearbyBridge: AdvertiserDelegate {
    public func advertiser(_ advertiser: Advertiser,
                           didReceiveConnectionRequestFrom endpointID: EndpointID,
                           with context: Data,
                           connectionRequestHandler: @escaping (Bool) -> Void) {
        let name = String(data: context, encoding: .utf8) ?? "peer"
        sendToUnity(method: "OnConnectionInitiated", payload: "\(endpointID)|\(name)")
        // Auto-accept for v1; reject UI can be wired later by routing this through the bridge.
        connectionRequestHandler(true)
    }
}

// MARK: - DiscovererDelegate

extension NearbyBridge: DiscovererDelegate {
    public func discoverer(_ discoverer: Discoverer,
                           didFind endpointID: EndpointID,
                           with context: Data) {
        let name = String(data: context, encoding: .utf8) ?? "host"
        sendToUnity(method: "OnEndpointFound", payload: "\(endpointID)|\(name)")
    }
    public func discoverer(_ discoverer: Discoverer, didLose endpointID: EndpointID) {
        sendToUnity(method: "OnEndpointLost", payload: endpointID)
    }
}

// MARK: - ConnectionManagerDelegate

extension NearbyBridge: ConnectionManagerDelegate {
    public func connectionManager(_ connectionManager: ConnectionManager,
                                   didReceive verificationCode: String,
                                   from endpointID: EndpointID,
                                   verificationHandler: @escaping (Bool) -> Void) {
        // Trust nearby peers for v1.
        verificationHandler(true)
    }
    public func connectionManager(_ connectionManager: ConnectionManager,
                                   didReceive data: Data,
                                   withID payloadID: PayloadID,
                                   from endpointID: EndpointID) {
        let b64 = data.base64EncodedString()
        sendToUnity(method: "OnPayloadReceived", payload: "\(endpointID)|\(b64)")
    }
    public func connectionManager(_ connectionManager: ConnectionManager,
                                   didReceive stream: InputStream,
                                   withID payloadID: PayloadID,
                                   from endpointID: EndpointID,
                                   cancellationToken token: CancellationToken) {
        // Streams reserved for future unreliable channel work.
    }
    public func connectionManager(_ connectionManager: ConnectionManager,
                                   didStartReceivingResourceWithID payloadID: PayloadID,
                                   from endpointID: EndpointID,
                                   at localURL: URL,
                                   withName name: String,
                                   cancellationToken token: CancellationToken) {}
    public func connectionManager(_ connectionManager: ConnectionManager,
                                   didReceiveTransferUpdate update: TransferUpdate,
                                   from endpointID: EndpointID,
                                   forPayload payloadID: PayloadID) {}
    public func connectionManager(_ connectionManager: ConnectionManager,
                                   didChangeTo state: ConnectionState,
                                   for endpointID: EndpointID) {
        switch state {
        case .connected:
            // Track an opaque handle; we just use the connectionManager to send.
            sendToUnity(method: "OnConnectionResult", payload: "\(endpointID)|0")
        case .disconnected:
            connections.removeValue(forKey: endpointID)
            sendToUnity(method: "OnDisconnected", payload: endpointID)
        case .rejected:
            sendToUnity(method: "OnConnectionResult", payload: "\(endpointID)|1")
        @unknown default:
            sendToUnity(method: "OnConnectionResult", payload: "\(endpointID)|2")
        }
    }
}

// MARK: - C ABI for Unity DllImport("__Internal")

@_cdecl("NearbyBridge_StartAdvertising")
public func NearbyBridge_StartAdvertising(_ serviceId: UnsafePointer<CChar>,
                                          _ displayName: UnsafePointer<CChar>) {
    NearbyBridge.shared.startAdvertising(
        serviceId: String(cString: serviceId),
        displayName: String(cString: displayName))
}

@_cdecl("NearbyBridge_StartDiscovery")
public func NearbyBridge_StartDiscovery(_ serviceId: UnsafePointer<CChar>) {
    NearbyBridge.shared.startDiscovery(serviceId: String(cString: serviceId))
}

@_cdecl("NearbyBridge_Stop")
public func NearbyBridge_Stop() {
    NearbyBridge.shared.stop()
}

@_cdecl("NearbyBridge_RequestConnection")
public func NearbyBridge_RequestConnection(_ endpointId: UnsafePointer<CChar>) {
    NearbyBridge.shared.requestConnection(endpointId: String(cString: endpointId))
}

@_cdecl("NearbyBridge_AcceptConnection")
public func NearbyBridge_AcceptConnection(_ endpointId: UnsafePointer<CChar>) {
    NearbyBridge.shared.acceptConnection(endpointId: String(cString: endpointId))
}

@_cdecl("NearbyBridge_RejectConnection")
public func NearbyBridge_RejectConnection(_ endpointId: UnsafePointer<CChar>) {
    NearbyBridge.shared.rejectConnection(endpointId: String(cString: endpointId))
}

@_cdecl("NearbyBridge_Disconnect")
public func NearbyBridge_Disconnect(_ endpointId: UnsafePointer<CChar>) {
    NearbyBridge.shared.disconnect(endpointId: String(cString: endpointId))
}

@_cdecl("NearbyBridge_SendBytes")
public func NearbyBridge_SendBytes(_ endpointId: UnsafePointer<CChar>,
                                    _ payload: UnsafePointer<UInt8>,
                                    _ length: Int32,
                                    _ reliable: Int32) {
    let data = Data(bytes: payload, count: Int(length))
    NearbyBridge.shared.sendBytes(
        endpointId: String(cString: endpointId),
        payload: data,
        reliable: reliable != 0)
}

// MARK: - Unity message bridge

/// Calls UnityFramework's UnitySendMessageToGOWithName via a small ObjC shim.
/// The shim is implemented in NearbyBridge_UnityShim.m (added to this target).
@_silgen_name("UnityFrameworkBridge_SendMessage")
func UnityFrameworkBridge_SendMessage(_ go: String, _ method: String, _ message: String)
