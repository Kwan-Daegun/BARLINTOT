using UnityEngine;

public class AudioManager : MonoBehaviour
{
   public AudioClip[] bgmTracks;
   public AudioSource AS;
   private int currentTrack = 0;


   void Start()
    {
        AS = GetComponent<AudioSource>();
        PlayCurrentTrack();

    }

    void Update()
    {
        if(!AS.isPlaying)
        {
            currentTrack = (currentTrack + 1) % bgmTracks.Length;
            PlayCurrentTrack();
        }
    }


    void PlayCurrentTrack()
    {
        AS.clip = bgmTracks[currentTrack];
        AS.Play();
    }
}
