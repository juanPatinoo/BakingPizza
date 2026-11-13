using BreakingPizza.Controls;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

namespace BreakingPizza.UI
{
    // Video de introducción.
    public class IntroVideoController : MonoBehaviour
    {
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private string nextScene = "DemoScene";
        [SerializeField] private bool allowSkip = true;

        private bool sceneLoading;

        private void OnEnable()
        {
            if (videoPlayer != null)
            {
                videoPlayer.loopPointReached += HandleVideoFinished;
            }
        }

        private void OnDisable()
        {
            if (videoPlayer != null)
            {
                videoPlayer.loopPointReached -= HandleVideoFinished;
            }
        }

        private void Update()
        {
            if (!allowSkip || sceneLoading) return;

            bool skipPressed =
                GameInput.AnyPressed; // cualquier tecla, click o botón del control

            if (skipPressed)
            {
                LoadNextScene();
            }
        }

        private void HandleVideoFinished(VideoPlayer source)
        {
            LoadNextScene();
        }

        private void LoadNextScene()
        {
            if (sceneLoading) return;
            sceneLoading = true;

            ScreenFader.LoadScene(nextScene); // transición suave
        }
    }
}
