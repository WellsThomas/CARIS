using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class HomeMenuEvents : MonoBehaviour
    {
        public void LoadIphoneScene()
        {
            SceneManager.LoadScene("Scenes/Builder", LoadSceneMode.Single);
        }
        
        public void LoadOverviewScene()
        {
            SceneManager.LoadScene("Scenes/Overview", LoadSceneMode.Single);
        }
    }
}