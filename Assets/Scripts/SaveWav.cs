using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;

public class SaveWav : MonoBehaviour {
    const int HEADER_SIZE = 44;

    public static string SaveToPath(string fileName,string path, AudioClip clip)
    {
        //Checking name of the file for .mp3 suffix
        Debug.Log("Filename before shrinking: " + fileName);
        if (fileName.ToLower().EndsWith(".mp3"))
        {
            fileName = fileName.Substring(0, fileName.Length - 4);
        }
        //Checking name of the file for .wav suffix
        if (!fileName.ToLower().EndsWith(".wav"))
        {
            fileName += ".wav";
        }
        //Creating full path to the file
        string filePath = Path.Combine(path, fileName);

        Debug.Log("path: " + path);
        Debug.Log("fileName: " + fileName);
        Debug.Log("filePath: " + filePath);


        //Creating Empty File
        using (FileStream fileStream = CreateEmpty(filePath))
        {
            //Converting clip and writing it to fileStream
            ConvertAndWrite(fileStream, clip);
            //
            WriteHeader(fileStream, clip);
        }

        return filePath;
    }


    static FileStream CreateEmpty(string filepath)
    {
        FileStream fileStream = new FileStream(filepath, FileMode.Create);
        byte emptyByte = new byte();

        //preparing the header
        for (int i = 0; i < HEADER_SIZE; i++) 
        {
            fileStream.WriteByte(emptyByte);
        }

        return fileStream;
    }


    //Converts audio clip to bytes and writes it in file stream
    static void ConvertAndWrite(FileStream fileStream, AudioClip clip)
    {

        float[] samples = new float[clip.samples];

        clip.GetData(samples, 0);

        //converting in 2 float[] steps to Int16[], //then Int16[] to Byte[]
        Int16[] intData = new Int16[samples.Length];
        
        Byte[] bytesData = new Byte[samples.Length * 2];
        //bytesData array is twice the size of
        //dataSource array because a float converted in Int16 is 2 bytes.

        int rescaleFactor = 32767; //to convert float to Int16

        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * rescaleFactor);
            Byte[] byteArr = new Byte[2];
            byteArr = BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i * 2);
        }

        fileStream.Write(bytesData, 0, bytesData.Length);
    }

    static void WriteHeader(FileStream fileStream, AudioClip clip)
    {

        int hz = clip.frequency;
        int channels = clip.channels;
        int samples = clip.samples;

        fileStream.Seek(0, SeekOrigin.Begin);

        Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
        fileStream.Write(riff, 0, 4);

        Byte[] chunkSize = BitConverter.GetBytes(fileStream.Length - 8);
        fileStream.Write(chunkSize, 0, 4);

        Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
        fileStream.Write(wave, 0, 4);

        Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
        fileStream.Write(fmt, 0, 4);

        Byte[] subChunk1 = BitConverter.GetBytes(16);
        fileStream.Write(subChunk1, 0, 4);

        //UInt16 two = 2;
        UInt16 one = 1;

        Byte[] audioFormat = BitConverter.GetBytes(one);
        fileStream.Write(audioFormat, 0, 2);

        Byte[] numChannels = BitConverter.GetBytes(channels);
        fileStream.Write(numChannels, 0, 2);

        Byte[] sampleRate = BitConverter.GetBytes(hz);
        fileStream.Write(sampleRate, 0, 4);

        Byte[] byteRate = BitConverter.GetBytes(hz * channels * 2);
        fileStream.Write(byteRate, 0, 4);

        UInt16 blockAlign = (ushort)(channels * 2);
        fileStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

        UInt16 bps = 16;
        Byte[] bitsPerSample = BitConverter.GetBytes(bps);
        fileStream.Write(bitsPerSample, 0, 2);

        Byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
        fileStream.Write(datastring, 0, 4);

        Byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2);
        fileStream.Write(subChunk2, 0, 4);

    }
}
