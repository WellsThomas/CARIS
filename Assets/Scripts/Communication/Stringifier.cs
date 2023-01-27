using Communication;
using Unity.Collections;
using Unity.iOS.Multipeer;

namespace UnityEngine.XR.ARFoundation.Samples.Communication
{
    public class Stringifier
    {

        private static Stringifier _stringifier;

        public Stringifier()
        {
            _stringifier = this;
        }

        public static Stringifier GetStringifier()
        {
            return _stringifier;
        }
        
        
        // Event which is triggered when new data is available. NetworkHandlers can subscribe to this to export information
        public static event DataPackage<NSData, TypeOfPackage, bool> OnNSDataReady;
        
        /**
         * Stringifies object and forwards 
         */
        public void StringifyAndForward<T>(T content, TypeOfPackage type, bool reliable)
        {
            var json = JsonUtility.ToJson(content);
            byte[] data = System.Text.Encoding.UTF8.GetBytes(json); 
            var slice = new NativeSlice<byte>(new NativeArray<byte>(data,Allocator.Temp));
            Forward(NSData.CreateWithBytes(slice), type, reliable);
        }
        
        public void Forward(NSData data, TypeOfPackage type, bool reliable)
        {
            OnNSDataReady?.Invoke(data, type, reliable);
        }
    }

    // Required to define an event. See public static event in Stringifier class
    public delegate void DataPackage<in T, in T1, in T2>(T x, T1 y, T2 z);
}