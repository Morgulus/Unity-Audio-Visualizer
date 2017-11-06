using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Threading;
using NAudio;
using NAudio.Wave;
using UnityEngine.UI;

public class AudioSelector : MonoBehaviour {

    public Text currentPlaying,
                directoryText,
                currentPlaylist;

    private AudioSource source;
    private AudioVisualiser visual;

    public int clipIndex;

    private AudioClip clip;

    private string[] trackList,
                     convertedList;

    private string defaultPath,
                   musicPath,
                   convertedPath;

    private bool hasPaused;

    private float time;
    private WWW www;


    private void Awake()
    {
        source = GetComponent<AudioSource>();
        visual = GetComponent<AudioVisualiser>();

        defaultPath = Application.dataPath;
        musicPath = "/music";
        convertedPath = "/converted";

        time = 0;
        hasPaused = false;
        clipIndex = 0;
    }
    private void Start()
    {
        CreateDirectories();
        trackList = Directory.GetFiles(defaultPath + musicPath, "*.mp3");
        convertedList = new string[trackList.Length];
        
        for (int i = 0; i < trackList.Length; i++)
        {            
            StartCoroutine(DownloadWWW("file:///" + trackList[i]));

            AudioClip clip = NAudioPlayer.FromMp3Data(www.bytes, trackList[i]);
            string fileName = Path.GetFileNameWithoutExtension(trackList[i]);
            convertedList[i] = SaveWav.SaveToPath(fileName, defaultPath + convertedPath, clip);
        }

        SetClip();
    }
    private IEnumerator DownloadWWW(string url)
    {
        www = new WWW(url);
        yield return www;
    }
    private void CreateDirectories()
    {
        if (!Directory.Exists(defaultPath + musicPath))
        {
            Directory.CreateDirectory(defaultPath + musicPath);
        }
        if (!Directory.Exists(defaultPath + convertedPath))
        {
            Directory.CreateDirectory(defaultPath + convertedPath);
        }
    }
    private void SetClip()
    {
        StartCoroutine(DownloadWWW("file:///" + convertedList[clipIndex]));
        clip = www.GetAudioClip();
        source.clip = clip;
    }
    private void Update()
    {
        //Incrementing time
        //Debug.Log(time + " : " + source.clip.length);
        if (!hasPaused)
        {
            time += Time.deltaTime;
        }
        //Checking if clip has ended
        if (time >= source.clip.length)
        {
            //Incrementing clip index
            clipIndex = (clipIndex < trackList.Length - 1) ? clipIndex + 1 : 0;
            SetClip();
            time = 0f;

        }
        //Checking if source is playing
        if (!source.isPlaying && !hasPaused)
        {
            source.Play();
        }
        //Checking for "Pause" input
        if (Input.GetKeyDown("space"))
        {
            PauseMusic();
        }
        //Checking for "Quit" input
        if (Input.GetKeyDown("escape"))
        {
            Application.Quit();
        }
        //Checking for "Next clip" input
        if (Input.GetKeyDown("right"))
        {
            clipIndex = (clipIndex < trackList.Length - 1) ? clipIndex + 1 : 0;
            SetClip();
            time = 0f;
        }
        //Checking for "Previous clip" input
        else if (Input.GetKeyDown("left"))
        {
            clipIndex = (clipIndex > 0) ? clipIndex - 1 : trackList.Length - 1;
            SetClip();
            time = 0f;
        }
        //Checking for "Circle type" input
        if (Input.GetKey("up"))
        {
            visual.TransformToCircle();
            visual.type = 2;
        }
        //Checking for "Line type" input
        else if (Input.GetKey("down"))
        {
            visual.TransformToLine();
            visual.type = 1;
        }
        //Checking for "Next playlist" input
        //if (Input.GetKeyDown("n"))
        //{
        //    playlistIndex = (playlistIndex < 1) ? 1 : 0;
        //    UpdateCurrent();
        //}
        //Checking for "Previous playlist" input
        //else if (Input.GetKeyDown("b"))
        //{
        //    playlistIndex = (playlistIndex == 1) ? 0 : 1;
        //    UpdateCurrent();
        //}
    }
    private void PauseMusic()
    {
        if (source.isPlaying)
        {
            source.Pause();
            hasPaused = true;
        }
        else if (!source.isPlaying)
        {
            source.UnPause();
            hasPaused = false;
        }

    }
}
