using UnityEngine;

public class EffectsManager : MonoBehaviour
{
    [SerializeField]
    private GameManager gameManager;

    [Header("Normal Confetti")]
    [SerializeField]
    private ParticleSystem[] normalConfetti;
    [SerializeField]
    private AudioClip soundConfetti;

    [Header("Normal Confetti")]
    [SerializeField]
    private ParticleSystem[] heartConfetti;
    [SerializeField]
    private AudioClip soundHeartConfetti;

    [SerializeField]
    private AudioSource effectsAudio;

    private void Start()
    {
        gameManager = FindAnyObjectByType<GameManager>();

        if (gameManager != null )
        {
            gameManager.Congratulation += PlayConfetti;
        }

        effectsAudio = GetComponent<AudioSource>();
    }

    public void PlayConfetti(int confettiId)
    {
        switch (confettiId)
        {
            case 0: // Normal confetti 
                foreach (var item in normalConfetti)
                {
                    item.Play();
                }
                effectsAudio.clip = soundConfetti;
                effectsAudio.Play();
                break;

            case 1: // Heart Confetti
                foreach (var item in heartConfetti) 
                {
                    item.Play();
                }
                effectsAudio.clip = soundHeartConfetti;
                effectsAudio.Play();
                break;

            default:
                effectsAudio.clip = soundConfetti;
                effectsAudio.Play();
                break;
        }
    }

}
