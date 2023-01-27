using System;
using UnityEngine;

namespace UI
{
    public class CameraManager : MonoBehaviour
    {
        [SerializeField] private Camera main;
        [SerializeField] private Camera secondary;

        private static CameraManager _cameraManager;

        private static readonly Rect PrimaryRect = new Rect(Vector2.zero,new Vector2(1,1));
        private static readonly Rect SecondaryRect = new Rect(new Vector2(0.65f, 0.05f),new Vector2(0.3f, 0.25f));

        private static bool mainIsPrimary = true;
        
        private void Start()
        {
            _cameraManager = this;
        }

        public static void ShowBoth(bool active)
        {
            _cameraManager.secondary.gameObject.SetActive(active);
            if (active)
            {
                mainIsPrimary = true;
                _cameraManager.secondary.rect = SecondaryRect;
                _cameraManager.secondary.depth = 1;
            }
            _cameraManager.main.rect = PrimaryRect;
            _cameraManager.main.depth = 0;
        }

        public static void Swap()
        {
            if (mainIsPrimary)
            {
                _cameraManager.main.rect = SecondaryRect;
                _cameraManager.secondary.rect = PrimaryRect;
                _cameraManager.main.depth = 1;
                _cameraManager.secondary.depth = 0;
            }
            else
            {
                _cameraManager.main.rect = PrimaryRect;
                _cameraManager.secondary.rect = SecondaryRect;
                _cameraManager.main.depth = 0;
                _cameraManager.secondary.depth = 1;
            }

            mainIsPrimary = !mainIsPrimary;
        }

        public static Camera GetSecondary()
        {
            return _cameraManager.secondary;
        }
    }
}