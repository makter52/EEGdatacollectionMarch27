using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayAudio : MonoBehaviour
{
    public AudioSource myRightSound;
    public AudioSource myLeftSound;

    // Start is called before the first frame update
    void Start()
    {
        if (myLeftSound == null)
        {
            Debug.LogWarning("my left sound not found");
        }

        if (myRightSound == null)
        {
            Debug.LogWarning("my right sound not found");
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            myLeftSound.Play();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            myRightSound.Play();
        }

    }
}
