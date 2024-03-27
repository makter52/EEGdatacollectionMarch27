using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playMyAudio : MonoBehaviour
{

    public AudioSource myAudio;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void PlayMyAudioSource()
    {
        myAudio.Play();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
