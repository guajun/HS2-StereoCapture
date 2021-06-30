using BepInEx;
using BepInEx.Configuration;
using Pngcs.Unity;
using UnityEngine;
using System;
using System.Collections;

namespace VRCapture
{
    [BepInPlugin("com.gua_jun.VRCapture", "VRCapture", "1.0")]
    [BepInProcess("StudioNEOV2.exe")]

    public class VRCapture : BaseUnityPlugin
    {

        enum FOVangle
        {
            All = 63,
            NoBackward = 31
        };



        bool readyFlag = false;
        bool capFlag = false;
        int i = 0;
        //int FrameCount = 0;
        int cachedCaptureFramerate = 0;
        ConfigEntry<int> NumberofFrames { get; set; }
        ConfigEntry<int> LengthofSide { get; set; }
        ConfigEntry<int> FrameRate { get; set; }
        ConfigEntry<KeyboardShortcut> KeyCaptureStart { get; set; }
        ConfigEntry<KeyboardShortcut> KeyCaptureCancel { get; set; }
        ConfigEntry<string> OutputPath { get; set; }
        ConfigEntry<FOVangle> Viewangle { get; set; }
        ConfigEntry<float> InterpupillaryDistance { get; set; }





        void Start()
        {
            Logger.LogInfo("VRCapture Loaded");
        }
        void Awake()
        {
            NumberofFrames = Config.Bind<int>("Capture Settings", "Number of frames", 0);
            LengthofSide = Config.Bind<int>("Capture Settings", "Number of pixel", 4096, "The value must be no more than 8192 and square of 2");
            FrameRate = Config.Bind<int>("Capture Settings", "Framerate", 60);
            OutputPath = Config.Bind<string>("Capture Settings", "Output location", @"D:\outputtest\", "The location of output screenshot");
            Viewangle = Config.Bind<FOVangle>("Capture Settings","Viewangle",FOVangle.All);
            KeyCaptureStart = Config.Bind<KeyboardShortcut>("Hotkeys", "Key start capture", new KeyboardShortcut(KeyCode.F9), "After pressing this key, capture will start after next leftclick of mouse");
            KeyCaptureCancel = Config.Bind<KeyboardShortcut>("Hotkeys", "Key cancel capture", new KeyboardShortcut(KeyCode.LeftControl, KeyCode.F9), "Cancel the effect of the start key");
            InterpupillaryDistance = Config.Bind<float>("Camera Settings", "Distacne between eyes", 0.068f, "The unit is meter");
            
        }

        public static Texture2D CaptureScreen(int sidelength, int face, float Dist)
        {
            Camera.main.stereoSeparation = Dist;
            RenderTexture EyeCubemap = new RenderTexture(sidelength, sidelength, 8, RenderTextureFormat.Default, RenderTextureReadWrite.Default);

            EyeCubemap.dimension = UnityEngine.Rendering.TextureDimension.Cube;
            EyeCubemap.antiAliasing = 1024;


            RenderTexture EquirectTexture = new RenderTexture(sidelength, sidelength, 8, RenderTextureFormat.Default, RenderTextureReadWrite.Default);
            EquirectTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;


            Camera.main.RenderToCubemap(EyeCubemap, face, Camera.MonoOrStereoscopicEye.Left);
            EyeCubemap.ConvertToEquirect(EquirectTexture, Camera.MonoOrStereoscopicEye.Left);

            Camera.main.RenderToCubemap(EyeCubemap, face, Camera.MonoOrStereoscopicEye.Right);
            EyeCubemap.ConvertToEquirect(EquirectTexture, Camera.MonoOrStereoscopicEye.Right);

            Texture2D tempTexture = new Texture2D(EquirectTexture.width, EquirectTexture.height);


            RenderTexture currentActiveRT = RenderTexture.active;
            RenderTexture.active = EquirectTexture;
            tempTexture.ReadPixels(new Rect(0, 0, EquirectTexture.width, EquirectTexture.height), 0, 0);
            
            RenderTexture.active = currentActiveRT;


            Destroy(EquirectTexture);
            Destroy(EyeCubemap);
            return tempTexture;
        }
        public IEnumerator WritePNG(Texture2D Tex2D,string Path,int i)
        {
            // Exports to a PNG
            string Target = Path + $"output_{i}.png";
            yield return PNG.WriteAsync(Tex2D,Target);
            // Restores the active render texture
        }

        public IEnumerator CaptureScreenshots()
        {
            yield return new WaitForEndOfFrame();

            Console.WriteLine(i);
            StartCoroutine(WritePNG(CaptureScreen(LengthofSide.Value, (int)Viewangle.Value, InterpupillaryDistance.Value), OutputPath.Value, i));
            
            //i = i + (Time.frameCount - FrameCount);
            i++;
        }

        public IEnumerator WritePNG()
        {

            yield break;
        }


        public void Update()
        {
            if(KeyCaptureStart.Value.IsDown())
            {
                readyFlag = true;
            }
            if(KeyCaptureCancel.Value.IsDown())
            {
                readyFlag = false;
            }
            if(Input.GetMouseButtonDown(0) && readyFlag)
            {
                capFlag = true;
                readyFlag = false;
                cachedCaptureFramerate = Time.captureFramerate;
                Time.captureFramerate = FrameRate.Value;
                //FrameCount = Time.frameCount;
            }
            if ((i < NumberofFrames.Value) && capFlag)
            {
                StartCoroutine(CaptureScreenshots());
            }
            if (i >= NumberofFrames.Value)
            {
                i = 0;
                capFlag = false;
                //FrameCount = 0;
                Time.captureFramerate = cachedCaptureFramerate;
                cachedCaptureFramerate = 0;
            }
        }
    }
}