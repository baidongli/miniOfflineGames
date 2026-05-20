package com.minigames.nearby

import android.app.Activity
import android.util.Base64
import android.util.Log
import com.google.android.gms.nearby.Nearby
import com.google.android.gms.nearby.connection.AdvertisingOptions
import com.google.android.gms.nearby.connection.ConnectionInfo
import com.google.android.gms.nearby.connection.ConnectionLifecycleCallback
import com.google.android.gms.nearby.connection.ConnectionResolution
import com.google.android.gms.nearby.connection.ConnectionsClient
import com.google.android.gms.nearby.connection.DiscoveredEndpointInfo
import com.google.android.gms.nearby.connection.DiscoveryOptions
import com.google.android.gms.nearby.connection.EndpointDiscoveryCallback
import com.google.android.gms.nearby.connection.Payload
import com.google.android.gms.nearby.connection.PayloadCallback
import com.google.android.gms.nearby.connection.PayloadTransferUpdate
import com.google.android.gms.nearby.connection.Strategy
import com.unity3d.player.UnityPlayer

/**
 * Unity-callable bridge over Google Play Services Nearby Connections.
 *
 * All callbacks are forwarded to the GameObject named "_NearbyTransportReceiver"
 * via [UnityPlayer.UnitySendMessage]. Method names on the Unity side must match
 * the strings below.
 *
 * Payload encoding for callbacks that carry binary data: "endpointId|base64bytes".
 * For events that carry an extra string: "endpointId|payloadString".
 */
class NearbyBridge(private val activity: Activity) {

    private val client: ConnectionsClient = Nearby.getConnectionsClient(activity)
    private val strategy: Strategy = Strategy.P2P_CLUSTER

    companion object {
        private const val TAG = "NearbyBridge"
        private const val UNITY_RECEIVER = "_NearbyTransportReceiver"
    }

    fun startAdvertising(serviceId: String, displayName: String) {
        val options = AdvertisingOptions.Builder().setStrategy(strategy).build()
        client.startAdvertising(displayName, serviceId, connectionLifecycle, options)
            .addOnSuccessListener { Log.i(TAG, "advertising started") }
            .addOnFailureListener { e -> Log.e(TAG, "advertise failed", e) }
    }

    fun startDiscovery(serviceId: String) {
        val options = DiscoveryOptions.Builder().setStrategy(strategy).build()
        client.startDiscovery(serviceId, endpointDiscovery, options)
            .addOnSuccessListener { Log.i(TAG, "discovery started") }
            .addOnFailureListener { e -> Log.e(TAG, "discovery failed", e) }
    }

    fun stopAll() {
        client.stopAdvertising()
        client.stopDiscovery()
        client.stopAllEndpoints()
    }

    fun requestConnection(endpointId: String) {
        client.requestConnection("local", endpointId, connectionLifecycle)
            .addOnFailureListener { e -> Log.e(TAG, "requestConnection failed", e) }
    }

    fun acceptConnection(endpointId: String) {
        client.acceptConnection(endpointId, payloadCallback)
    }

    fun rejectConnection(endpointId: String) {
        client.rejectConnection(endpointId)
    }

    fun disconnect(endpointId: String) {
        client.disconnectFromEndpoint(endpointId)
    }

    fun sendBytes(endpointId: String, payload: ByteArray, reliable: Boolean) {
        // Nearby's BYTES payloads are always reliable; for unreliable, we'd use
        // STREAM with our own framing. v1 ships BYTES only.
        client.sendPayload(endpointId, Payload.fromBytes(payload))
    }

    // --- callbacks ---

    private val endpointDiscovery = object : EndpointDiscoveryCallback() {
        override fun onEndpointFound(endpointId: String, info: DiscoveredEndpointInfo) {
            send("OnEndpointFound", "$endpointId|${info.endpointName}")
        }
        override fun onEndpointLost(endpointId: String) {
            send("OnEndpointLost", endpointId)
        }
    }

    private val connectionLifecycle = object : ConnectionLifecycleCallback() {
        override fun onConnectionInitiated(endpointId: String, info: ConnectionInfo) {
            send("OnConnectionInitiated", "$endpointId|${info.endpointName}")
        }
        override fun onConnectionResult(endpointId: String, result: ConnectionResolution) {
            // 0=Ok, 1=Rejected, 2=Error - matches MiniGames.Networking.Transport.ConnectionStatus.
            val code = when (result.status.statusCode) {
                com.google.android.gms.common.api.CommonStatusCodes.SUCCESS -> 0
                com.google.android.gms.nearby.connection.ConnectionsStatusCodes.STATUS_CONNECTION_REJECTED -> 1
                else -> 2
            }
            send("OnConnectionResult", "$endpointId|$code")
        }
        override fun onDisconnected(endpointId: String) {
            send("OnDisconnected", endpointId)
        }
    }

    private val payloadCallback = object : PayloadCallback() {
        override fun onPayloadReceived(endpointId: String, payload: Payload) {
            val bytes = payload.asBytes() ?: return
            val b64 = Base64.encodeToString(bytes, Base64.NO_WRAP)
            send("OnPayloadReceived", "$endpointId|$b64")
        }
        override fun onPayloadTransferUpdate(endpointId: String, update: PayloadTransferUpdate) {
            // No-op for BYTES payloads (always atomic).
        }
    }

    private fun send(method: String, payload: String) {
        try {
            UnityPlayer.UnitySendMessage(UNITY_RECEIVER, method, payload)
        } catch (t: Throwable) {
            Log.e(TAG, "UnitySendMessage failed: $method", t)
        }
    }
}
