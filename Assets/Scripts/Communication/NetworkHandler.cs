using System;
using Unity.Collections;
using Unity.iOS.Multipeer;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARFoundation.Samples;
using UnityEngine.XR.ARFoundation.Samples.Communication;
using UnityEngine.XR.ARFoundation.Samples.Player;
using UnityEngine.XR.ARKit;

namespace Communication
{
    public class NetworkHandler : MonoBehaviour
{   
    // Name of service. Used to distinquish between services between iOS devices.
    [SerializeField] private string m_ServiceType;
    private HouseManager _houseManager;
    [SerializeField] private GameObject xrOrigin;
    private Parser _parser;
    private PlayerManager _playerManager;

    // Subsystem for communication between phones
    MCSession _MCSession;
    
    // Subsystem for AR
    ARSession _ARSession;
    
    public string serviceType
    {
        get => m_ServiceType;
        set => m_ServiceType = value;
    }
    
    ARKitSessionSubsystem GetSubsystem()
    {
        if (_ARSession == null)
            return null;

        return _ARSession.subsystem as ARKitSessionSubsystem;
    }
    
    void Awake()
    {
        _MCSession = new MCSession(SystemInfo.deviceName, m_ServiceType);
        _ARSession = GetComponent<ARSession>();
    }

    void Start()
    {
        // Instantiate parser which parses information
        _houseManager = xrOrigin.GetComponent<HouseManager>();
        _playerManager = xrOrigin.GetComponent<PlayerManager>();
        _parser = new Parser(_houseManager, _playerManager);
    }
    
    void OnEnable()
    {
        // Subscribe on events to trigger sudden data-packages
        Stringifier.OnNSDataReady += CreatePrefixAndSend;
        
        // Forward enable to subsystem
        GetSubsystem().collaborationRequested = true;
        _MCSession.Enabled = true;
    }
    
    void OnDisable()
    {
        // Unsubscribe from events when disabled to stop sending data-packages
        Stringifier.OnNSDataReady -= CreatePrefixAndSend;
        
        // Forward disable to subsystem
        GetSubsystem().collaborationRequested = false;
        _MCSession.Enabled = false;
    }

    /*
     * Updates and ensures that all communication received is treated accordingly.
     * Equally checks if subsystem has data available.
     */
    private void Update()
    {
        // If no subsystem = return.
        var subsystem = GetSubsystem();
        if (subsystem == null)
            return;
        
        // Ensure subsystems requests are handled
        CheckSubsystem(subsystem);
        
        
        // Following section checks if any data has become available and forwards it accordingly in the system
        while (_MCSession.ReceivedDataQueueSize > 0)
        {
            // Send signal to GUI for debugging, that data has been received
            CollaborationNetworkingIndicator.NotifyIncomingDataReceived();
            
            // Dequeue and turn into array
            using (NSData data = _MCSession.DequeueReceivedData())
            {
                byte[] bytes = data.Bytes.ToArray();
            
                // Get Prefix of communication
                TypeOfPackage typeOfPackage = (TypeOfPackage) bytes[0];
                
                // Separate data-package from the initial flag-byte
                byte[] rawData = new byte[bytes.Length - 1];
                Array.Copy(bytes, 1, rawData, 0, rawData.Length);
                
                // Forward intel to right part of system
                switch (typeOfPackage)
                {
                    case TypeOfPackage.Subsystem:
                        NativeArray<byte> na = new NativeArray<byte>(rawData, Allocator.Temp);
                        NativeSlice<byte> ns = new NativeSlice<byte>(na);
                        NSData nsRawData = NSData.CreateWithBytesNoCopy(ns);
                        
                        using (var collaborationData = new ARCollaborationData(nsRawData.Bytes))
                        {
                            if (collaborationData.valid)
                            {
                                subsystem.UpdateWithCollaborationData(collaborationData);
                            }
                        }
                        break;
                    
                    default:
                        _parser.Parse(typeOfPackage, rawData);
                        break;
                }
            }
        }
        
        
    }

    /**
     * Check for available data in the subsystem to ship it accordingly.
     * We add a Subsystem prefix to ensure receivers can distinguish between subsystem-data and other types. 
     */
    private void CheckSubsystem(ARKitSessionSubsystem subsystem)
    {
        // While any data
        while (subsystem.collaborationDataCount > 0)
        {
            // Decode it
            using (var collaborationData = subsystem.DequeueCollaborationData())
            {
                // Send signal to debug-gui
                CollaborationNetworkingIndicator.NotifyHasCollaborationData();

                if (_MCSession.ConnectedPeerCount == 0){
                    continue;
                }
                
                // Serialize it. Turn it into NSData type. Forward it with Subsystem flag.
                var serializedData = collaborationData.ToSerialized();
                var data = NSData.CreateWithBytesNoCopy(serializedData.bytes);
                var reliable = collaborationData.priority == ARCollaborationDataPriority.Critical;
                CreatePrefixAndSend(data, TypeOfPackage.Subsystem, reliable);

            }
        }
    }

    /**
     * Add prefix and send data
     * Prefix helps distinquish between types of communication on receiver-side
     * Reliable is whether data NEEDS to be received, or whether the data keeps coming.
     * Unreliable data is useful for rapid package sending like position of phones
     * Reliable is useful for synchronized operations
     */
    private void CreatePrefixAndSend(NSData nsData, TypeOfPackage prefix, bool reliable)
    {  
        // Make new array. Add prefix to data. Add nsData to the new array
        byte[] nsBytes = nsData.Bytes.ToArray();
        byte[] prefixedData = new byte[nsBytes.Length + 1];
        prefixedData[0] = (byte)prefix;
        Buffer.BlockCopy(nsBytes, 0, prefixedData, 1, nsBytes.Length); 

        // Turn prefixed data into NSData
        NativeArray<byte> na = new NativeArray<byte>(prefixedData, Allocator.Temp);
        NativeSlice<byte> ns = new NativeSlice<byte>(na);
        NSData prefixNsData = NSData.CreateWithBytesNoCopy(ns);

        // Send to all other peers
        SendData(prefixNsData, reliable);
    }

    /**
     * Sends data to all other peers
     */
    private void SendData(NSData data, bool reliable)
    {
        // If any peers connected. Send to all peers.
        if (_MCSession.ConnectedPeerCount > 0)
        {
            _MCSession.SendToAllPeers(data, reliable ? MCSessionSendDataMode.Reliable : MCSessionSendDataMode.Unreliable);
            
            // Indicate an outgoing data-package to debug-GUI.
            CollaborationNetworkingIndicator.NotifyOutgoingDataSent();
        }
    }
}

}