using UnityEngine;
using UnityEngine.Video;

public class VideoIntroController : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer; // tu Video Player
    [SerializeField] private GameObject videoPanel;   // panel con RawImage
    [SerializeField] private AudioSource introAudio;  // AudioSource con tu clip

    public bool IntroFinished { get; private set; }

    private void Reset()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        introAudio = GetComponent<AudioSource>();
    }

    private void Start()
    {
        IntroFinished = false;
        if (videoPanel) videoPanel.SetActive(true);

        if (videoPlayer != null)
            videoPlayer.loopPointReached += OnVideoFinished;

        // Reproducir ambos al iniciar
        if (introAudio != null) introAudio.Play();
        if (videoPlayer != null && !videoPlayer.isPlaying) videoPlayer.Play();
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        if (introAudio != null && introAudio.isPlaying) introAudio.Stop();
        FinishIntro();
    }

    private void FinishIntro()
    {
        if (videoPanel) videoPanel.SetActive(false);
        IntroFinished = true;
    }
}
