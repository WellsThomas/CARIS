using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using Communication;
using JetBrains.Annotations;
using Tools;
using Unity.Collections;
using Unity.iOS.Multipeer;
using UnityEngine.Serialization;
using UnityEngine.XR.ARFoundation.Samples.Communication;

namespace UnityEngine.XR.ARFoundation.Samples.Player
{
    public class PlayerManager : MonoBehaviour
    {
        private static PlayerManager _manager;

        public static PlayerManager GetManager()
        {
            return _manager;
        }
        
        // REQUIRES ARParticipantManager!
        private ARParticipantManager _arParticipantManager;
        private Stringifier _stringifier;
        private int localPlayerID;
        private static string _leaderParticipantID = "LEADER";

        private Dictionary<ARParticipant, int> _players = new Dictionary<ARParticipant, int>();
        private Dictionary<ARParticipant, int> _temporaryPlayers;
        private void Start()
        {
            _arParticipantManager = gameObject.GetComponent<ARParticipantManager>();
            _arParticipantManager.participantsChanged += ParticipantsChanged;
            _stringifier = gameObject.GetComponent<HouseManager>().GetStringifier();
            _manager = this;
        }

        private void ParticipantsChanged(ARParticipantsChangedEventArgs args)
        {
            if(args.added.Count != 0) AddParticipants(args.added);
            if(args.removed.Count != 0) RemoveParticipants(args.removed);
        }

        private void RemoveParticipants(List<ARParticipant> deleted)
        {
            foreach (var arParticipant in deleted)
            {
                _players.Remove(arParticipant);
            }
        }

        private void AddParticipants(List<ARParticipant> added)
        {
            foreach (var arParticipant in added)
            {
                _players.Add(arParticipant, 0);
            }
        }

        public void UpdateIDs()
        {
            _temporaryPlayers = new Dictionary<ARParticipant, int>();
            var id = 1;
            foreach (var (arParticipant, value) in _players)
            {
                _temporaryPlayers[arParticipant] = id;
                id++;
            }

            _players = _temporaryPlayers;

            localPlayerID = 0;
        }
        
        public class PlayerID
        {
            public int ID;
            public string ParticipantID;

            public PlayerID(int id, string participantID)
            {
                ID = id;
                ParticipantID = participantID;
            }
        }

        public void SendIDs()
        {
            if (_stringifier == null) return;

            StringBuilder sb = new StringBuilder("", 100);
            
            foreach (var (trackable, id) in _players)
            {
                sb.Append(JsonUtility.ToJson(new PlayerID(id, trackable.trackableId.ToString())));
                sb.Append("|||");
            }
            sb.Append(JsonUtility.ToJson(new PlayerID(0, _leaderParticipantID)));
            
            Debug.Log(sb.ToString());
            
            // Send sb content
            var data = Encoding.UTF8.GetBytes(sb.ToString()); 
            var slice = new NativeSlice<byte>(new NativeArray<byte>(data,Allocator.Temp));
            _stringifier.Forward(NSData.CreateWithBytes(slice), TypeOfPackage.PlayerIDs, true);
        }

        public void HandleIncomingIDs(byte [] data)
        {
            // Reset all IDs
            _temporaryPlayers = new Dictionary<ARParticipant, int>();
            
            // Decode ID package
            var str = Encoding.UTF8.GetString(data);
            Debug.Log(str);
            var stringObjects = str.Split("|||");
            Debug.Log("Adding "+stringObjects.Length + " players");
            
            // For each package assign the new ID to the participant
            foreach (var stringObject in stringObjects)
            {
                var playerID = JsonUtility.FromJson<PlayerID>(stringObject);
                SetParticipantID(playerID.ParticipantID, playerID.ID);
            }
            
            foreach (var (key, value) in _players)
            {
                if (_temporaryPlayers.ContainsKey(key)) continue;

                _temporaryPlayers[key] = 0;
            }

            _players = _temporaryPlayers;
        }

        private ARParticipant FindParticipantByID(String id)
        {
            foreach (var (key, value) in _players)
            {
                if (key.trackableId.ToString().Equals(id)) return key;
            }

            return null;
        }

        private void SetParticipantID(string participantID, int id)
        {
            Debug.Log("Incoming: "+participantID+" with ID "+id);
            // Find the corresponding ARParticipant with the participantID
            foreach (var (arParticipant, value) in _players)
            {
                if (!arParticipant.trackableId.ToString().Equals(participantID)) continue;
                _temporaryPlayers[arParticipant] = id;
                Debug.Log("Added ID using arParticipantID");
                return;
            }

            // If ID doesnt belong to array. See if ID is not leader himself.
            // This means the ID belongs to the device processing this information
            if (!participantID.Equals(_leaderParticipantID))
            {
                Debug.Log("Added localPlayerID");
                localPlayerID = id;
                return;
            }
        }

        public int GetID(ARParticipant participant)
        {
            return _players.GetValueOrDefault(participant, 0);
        }

        [CanBeNull]
        public ARParticipant GetParticipant(int id)
        {
            foreach (var (key, value) in _players)
            {
                if (value == id) return key;
            }

            return null;
        }

        public int GetLocalPlayerID()
        {
            return localPlayerID;
        }
    }
}