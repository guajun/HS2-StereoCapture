using BepInEx;
using BepInEx.Configuration;
using Pngcs.Unity;
using UnityEngine;
using System;
using System.Collections;
using UnityEngine.Rendering;
using System.Threading;
using HarmonyLib;
using UnityEngine.XR;
using UnityEngine.Experimental.Rendering;


namespace StereoCapture
{
    [BepInPlugin("com.gua_jun.VRCapture", "VRCapture", "1.0.1")]
    [BepInProcess("StudioNEOV2.exe")]
    

    public class StereoCapture : BaseUnityPlugin
    {

        enum FOVangle
        {
            All = 63,
            NoBackward = 31,
            Half = 0
        };



        bool readyFlag = false;
        bool capFlag = false;
        static int i = 0;
        int cachedCaptureFramerate = 0;
        //static bool bugstate = false;



        
        ConfigEntry<int> NumberofFrames { get; set; }
        private static ConfigEntry<int> LengthofSide { get; set; }
        ConfigEntry<int> FrameRate { get; set; }
        ConfigEntry<KeyboardShortcut> KeyCaptureStart { get; set; }
        ConfigEntry<KeyboardShortcut> KeyCaptureCancel { get; set; }
        private static ConfigEntry<string> OutputPath { get; set; }
        private static ConfigEntry<FOVangle> Viewangle { get; set; }
        private static ConfigEntry<float> InterpupillaryDistance { get; set; }


        RenderTexture EquirectTexture;


        private static Material equirectangularConverter = null;
        static bool LR;
        void Start()
        {
            Logger.LogInfo("VRCapture Loaded");
            //new Harmony("com.gua_jun.VRCapture").PatchAll();
           

        }
        /*
        [HarmonyPatch(typeof(Camera), "stereoEnabled", MethodType.Getter)]
        class StereoEnabledPatch
        {
            public static void Postfix(Camera __instance, ref bool __result)
            {
                
                if (__instance.name == "renderCam")
                {
                    __result = true;
                    //Console.WriteLine(__instance.ToString() + "Patched");
                }
                

            }
        }
        [HarmonyPatch(typeof(Camera), "stereoEnabled", MethodType.Setter)]
        class StereoEnabledPatch2
        {
            public static void Postfix(Camera __instance, ref bool __result)
            {
                
                if (__instance.name == "renderCam")
                {
                    __result = true;
                    //Console.WriteLine(__instance.ToString() + "Patched");
                }
                

            }
        }
        */

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

        
        
        private static void Init(ref RenderTexture EquirectTexture)
        {
            //Shader.EnableKeyword("STEREO_CUBEMAP_RENDER_ON");
            //XRSettings.enabled = true;

            if (equirectangularConverter == null)
            {
                var abd = Properties.Resource.EquirectangularConverter;
                Console.WriteLine("loading");
                var ab = AssetBundle.LoadFromMemory(abd);
                Console.WriteLine("loaded");
                equirectangularConverter = new Material(ab.LoadAsset<Shader>("assets/shaders/equirectangularconverter.shader"));
                ab.Unload(false);
                equirectangularConverter.SetFloat(Shader.PropertyToID("_PaddingX"), 0f);

            }
            
            

            

            EquirectTexture = new RenderTexture(LengthofSide.Value, LengthofSide.Value/2 , 32, RenderTextureFormat.ARGB32);
            EquirectTexture.dimension = TextureDimension.Tex2D;
            EquirectTexture.antiAliasing = 8;
            //EquirectTexture.volumeDepth = 32;
            //EquirectTexture.IsCreated();

        }

       

        private static void CaptureScreenLR(ref RenderTexture EquirectTexture)
        {
            




            //RenderTexture currentActiveRT = RenderTexture.active;

            RenderTexture EyeCubemap = RenderTexture.GetTemporary(LengthofSide.Value, LengthofSide.Value, 32 ,RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default,8);
            //RenderTexture EyeCubemap = new RenderTexture(LengthofSide.Value, LengthofSide.Value, 32, RenderTextureFormat.ARGB32);
            EyeCubemap.dimension = TextureDimension.Cube;
            EyeCubemap.antiAliasing = 8;
            EyeCubemap.volumeDepth = 32;
            //EyeCubemap.depth = LengthofSide.Value;







            //Camera renderCam = Instantiate(Camera.main);



            Camera renderCam = Instantiate(Camera.main);

            renderCam.stereoTargetEye = StereoTargetEyeMask.Both;
            renderCam.name = "renderCam";
            renderCam.stereoSeparation = InterpupillaryDistance.Value;




            Console.WriteLine(renderCam.stereoEnabled);
            Console.WriteLine(renderCam.stereoSeparation);
            Console.WriteLine(renderCam.name);
            //Console.WriteLine(SystemInfo.maxCubemapSize);
            //Console.WriteLine(GraphicsSettings.renderPipelineAsset.ToString());


            if (Viewangle.Value == 0)
            {

                EquirectTexture = RenderTexture.GetTemporary(LengthofSide.Value, LengthofSide.Value / 2, 32, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 8, RenderTextureMemoryless.MSAA);
                EquirectTexture.dimension = TextureDimension.Tex2D;
                EquirectTexture.antiAliasing = 8;

            }
            else
            {
                //Cubemap ECM = new Cubemap(4096, TextureFormat.ARGB32 ,false);
                //Console.WriteLine(LR);
                //Camera.main.RenderToCubemap(EyeCubemap, (int)Viewangle.Value, LR ? Camera.MonoOrStereoscopicEye.Left : Camera.MonoOrStereoscopicEye.Right);
                Console.WriteLine("Pre Render");
                renderCam.RenderToCubemap(EyeCubemap, (int)Viewangle.Value, LR ? Camera.MonoOrStereoscopicEye.Left : Camera.MonoOrStereoscopicEye.Right);
                //renderCam.RenderToCubemap(ECM, (int)Viewangle.Value);
                //renderCam.RenderToCubemap(EyeCubemap, (int)Viewangle.Value,Camera.MonoOrStereoscopicEye.Mono);
                Console.WriteLine("Rendered");
                DestroyImmediate(renderCam.gameObject);


                Graphics.Blit(EyeCubemap, EquirectTexture, equirectangularConverter);
                RenderTexture.ReleaseTemporary(EyeCubemap);
                DestroyImmediate(EyeCubemap);



            }

            //RenderTexture.active = currentActiveRT;

        }
        
        

        private IEnumerator CaptureWrite()
        {
            yield return new WaitForEndOfFrame();

            //Init(ref EquirectTexture);
            AsyncGPUReadbackRequest reqL;
            AsyncGPUReadbackRequest reqR;
            
            Color32[] bufferTemp;
            Color32[] buffer;
            string Target = OutputPath.Value + $"output_{i}.png";
            try
            {
                Console.WriteLine("StartL");
                LR = true;

                CaptureScreenLR(ref EquirectTexture);
                Console.WriteLine("EquirectTextureL");


                reqL = AsyncGPUReadback.Request(EquirectTexture, 0, 0, EquirectTexture.width, 0, EquirectTexture.height, 0, 1, TextureFormat.RGBA32);

                reqL.WaitForCompletion();
                Console.WriteLine("reqL");

                bufferTemp = new Color32[reqL.GetData<Color32>().Length];
                buffer = new Color32[bufferTemp.Length * 2];
                reqL.GetData<Color32>().CopyTo(bufferTemp);
                bufferTemp.CopyTo(buffer, bufferTemp.Length);
            }
            catch(Exception ex)
            {
                
                Console.WriteLine(ex);
                Console.WriteLine("Sleep");
                Thread.Sleep(30000);


                Init(ref EquirectTexture);
                Console.WriteLine("StartL");
                LR = true;

                CaptureScreenLR(ref EquirectTexture);
                Console.WriteLine("EquirectTextureL");


                reqL = AsyncGPUReadback.Request(EquirectTexture, 0, 0, EquirectTexture.width, 0, EquirectTexture.height, 0, 1, TextureFormat.RGBA32);

                reqL.WaitForCompletion();
                Console.WriteLine("reqL");

                bufferTemp = new Color32[reqL.GetData<Color32>().Length];
                buffer = new Color32[bufferTemp.Length * 2];
                reqL.GetData<Color32>().CopyTo(bufferTemp);
                bufferTemp.CopyTo(buffer, bufferTemp.Length);

                
            }


            try
            {
                Console.WriteLine("StartR");

                LR = false;
                CaptureScreenLR(ref EquirectTexture);

                Console.WriteLine("EquirectTextureR");

                reqR = AsyncGPUReadback.Request(EquirectTexture, 0, 0, EquirectTexture.width, 0, EquirectTexture.height, 0, 1, TextureFormat.RGBA32);
                reqR.WaitForCompletion();
                Console.WriteLine("reqR");

                bufferTemp = new Color32[reqR.GetData<Color32>().Length];
                reqR.GetData<Color32>().CopyTo(bufferTemp);
                bufferTemp.CopyTo(buffer, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine("Sleep");
                Thread.Sleep(30000);
                Init(ref EquirectTexture);


                Console.WriteLine("StartR");

                LR = false;
                CaptureScreenLR(ref EquirectTexture);

                Console.WriteLine("EquirectTextureR");

                reqR = AsyncGPUReadback.Request(EquirectTexture, 0, 0, EquirectTexture.width, 0, EquirectTexture.height, 0, 1, TextureFormat.RGBA32);
                reqR.WaitForCompletion();
                Console.WriteLine("reqR");

                bufferTemp = new Color32[reqR.GetData<Color32>().Length];
                reqR.GetData<Color32>().CopyTo(bufferTemp);
                bufferTemp.CopyTo(buffer, 0);

                
            }

            Console.WriteLine("GetData");
            Console.WriteLine(i);

            i++;
            
            yield return PNG.WriteAsync(buffer, EquirectTexture.width, EquirectTexture.height * 2, 8, false, false, Target);

 
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
                Camera.main.stereoSeparation = InterpupillaryDistance.Value;
                Init(ref EquirectTexture);

            }
            if ((i < NumberofFrames.Value) && capFlag)
            {

                StartCoroutine(CaptureWrite());

            }
            if (i >= NumberofFrames.Value)
            {
                i = 0;
                capFlag = false;

                Time.captureFramerate = cachedCaptureFramerate;
                cachedCaptureFramerate = 0;
            }
        }
    }
}