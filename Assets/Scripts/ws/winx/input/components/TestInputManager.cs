﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Timers;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;
using ws.winx.devices;
using ws.winx.drivers;
using ws.winx.input;
using ws.winx.input.states;
using ws.winx.platform;
using ws.winx.unity;
using ws.winx.csharp.extensions;
using ws.winx.input.components;
using ws.winx.processing;

namespace ws.winx.components
{
    public class TestInputManager : MonoBehaviour
    {






    
        private float vSliderValue;
        private ThrustmasterRGTFFDDevice TTFFDDevice;
        private XInputDevice XDevice;
        private byte forceX;
        private Timer timer;
        private float vSliderValuePrev;
        IEnumerator runEffectEnumerator;
        ComplementaryFilter complementaryFuzer;
        float lastTime = -1f;
        public static double RAD_TO_DEG = 180 / Math.PI;
        public static double DEG_TO_RAD = Math.PI / 180;
        GameObject wiimote;
        Vector3 Angles;
		UserInterfaceWindow ui;


        // Use this for initialization
        void Start()
        {
            complementaryFuzer = new ComplementaryFilter();
            wiimote = GameObject.Find("wiimote");
            Angles = new Vector3();



            vSliderValuePrev = vSliderValue = 128f;

            timer = new Timer(500.0);
            timer.Elapsed += new ElapsedEventHandler(onTimerElapsed);

			 ui= this.GetComponent<UserInterfaceWindow>();


            //supporting devices with custom drivers
            //When you add them add specialized first then XInputDriver  then wide range supporting drivers UnityDriver
#if (UNITY_STANDALONE_WIN)
            InputManager.hidInterface.defaultDriver = new UnityDriver();
            InputManager.AddDriver(new ThrustMasterDriver());
            InputManager.AddDriver(new WiiDriver());
            //InputManager.AddDriver(new XInputDriver());

#endif

#if (UNITY_STANDALONE_OSX)
			//InputManager.AddDriver(new ThrustMasterDriver());
			//InputManager.AddDriver(new XInputDriver());

			InputManager.hidInterface.defaultDriver=new UnityDriver();
#endif

#if (UNITY_STANDALONE_ANDROID)
        InputManager.AddDriver(new ThrustMasterDriver());
#endif


            //TODO think of better entry point
            InputManager.hidInterface.Enumerate();



            // !!!Postive аxes mapping only currently(need to find way to distinct postive from negative axis in Unity way of handling)
            // if(Application.isPlaying)
            //     InputManager.AddDriver(new UnityDriver());


            //if you want to load some states from .xml and add custom manually first load settings xml
            //!!!Application.streamingAssetPath gives "Raw" folder in web player

#if (UNITY_STANDALONE || UNITY_EDITOR ) && !UNITY_WEBPLAYER && !UNITY_ANDROID
            //UnityEngine.Debug.Log("Standalone");
           


            if (ui != null)
            {//settingsXML would trigger internal loading mechanism (only for testing)

				InputManager.loadSettings(Path.Combine(Application.streamingAssetsPath, "InputSettings.xml"));



                ui.settings = InputManager.Settings;
            }


            manuallyAddStateAndHandlers();

#endif

            #region Load InputSettings.xml Android
#if UNITY_ANDROID



            Loader request = new Loader();


            if (Application.platform == RuntimePlatform.Android)
            {
                if (File.Exists(Application.persistentDataPath + "/" + "InputSettings.xml"))
                {

                    if (ui != null)
                    {
                        Debug.Log("Game>> Try to load from " + Application.persistentDataPath);
                        InputManager.loadSettings(Application.persistentDataPath + "/" + "InputSettings.xml");
                        ui.settings = InputManager.Settings;
                        manuallyAddStateAndHandlers();
                        return;

                    }
                }
                else
                {// content of StreamingAssets get packed inside .APK and need to be load with WWW
                    request.Add(Path.Combine(Application.streamingAssetsPath, "InputSettings.xml"));
                }


                request.LoadComplete += new EventHandler<LoaderEvtArgs<List<WWW>>>(onLoadComplete);
                request.Error += new EventHandler<LoaderEvtArgs<String>>(onLoadError);
                request.LoadItemComplete += new EventHandler<LoaderEvtArgs<WWW>>(onLoadItemComplete);
                request.load();
            }
            else //TARGET=ANDROID but playing in EDITOR => use Standalone setup
            {
                if (ui != null)
                {//settingsXML would trigger internal loading mechanism (only for testing)

                    InputManager.loadSettings(Path.Combine(Application.streamingAssetsPath, "InputSettings.xml"));



                    ui.settings = InputManager.Settings;
                }


                manuallyAddStateAndHandlers();

            }
           

            
#endif
            #endregion

#if(UNITY_WEBPLAYER || UNITY_EDITOR) && !UNITY_STANDALONE && !UNITY_ANDROID
            Loader request = new Loader();

				//UNITY_WEBPLAYER: Application.dataPath "http://localhost/appfolder/"
                request.Add(Application.dataPath+"/StreamingAssets/InputSettings.xml");
            

			request.LoadComplete += new EventHandler<LoaderEvtArgs<List<WWW>>>(onLoadComplete);
			request.Error += new EventHandler<LoaderEvtArgs<String>>(onLoadError);
			request.LoadItemComplete += new EventHandler<LoaderEvtArgs<WWW>>(onLoadItemComplete);
            request.load();
#endif






        }

        void onUp()
        {
            Debug.Log("Up");
        }

        void onDown()
        {
            Debug.Log("Down");
        }

        void Handle1()
        {
            Debug.Log("Handle1");
        }

        void Handle2()
        {
            Debug.Log("Handle2");
        }

#if (UNITY_WEBPLAYER || UNITY_EDITOR || UNITY_ANDROID) && !UNITY_STANDALONE
        void onLoadComplete(object sender, LoaderEvtArgs<List<WWW>> args)
        {
           // Debug.Log(((List<WWW>)args.data).ElementAt(0).text);

            if (System.Threading.Thread.CurrentThread.ManagedThreadId != 1) return;


			//UnityEngine.Debug.Log("WebPlayer " + Path.Combine(Path.Combine(Application.dataPath, "StreamingAssets"), "InputSettings.xml"));
			
			

		


            if (ui != null)//without settingsXML defined =>load them manually and attach them
            {
                InputManager.loadSettingsFromText(args.data.ElementAt(0).text);
                ui.settings = InputManager.Settings;
            }


            manuallyAddStateAndHandlers();
       
        }

        void onLoadItemComplete(object sender, LoaderEvtArgs<WWW> args)
        {
           // Debug.Log(args.data.text);
        }


        void onLoadError(object sender, LoaderEvtArgs<String> args)
        {
             Debug.Log(args.data);
        }
#endif




        void manuallyAddStateAndHandlers()
        {



            //   UnityEngine.Debug.Log(InputManager.Log());

            //		adding input-states pairs manually
            //			InputManager.MapStateToInput("My State1",new InputCombination(KeyCodeExtension.toCode(Joysticks.Joystick1,JoystickAxis.AxisPovX,JoystickPovPosition.Forward),(int)KeyCode.Joystick4Button9,(int)KeyCode.P,(int)KeyCode.JoystickButton0));
            //			InputManager.MapStateToInput("My State2",new InputCombination(KeyCode.Joystick4Button9,KeyCode.P,KeyCode.JoystickButton0));
            //			InputManager.MapStateToInput("My State3",new InputCombination("A(x2)+Mouse1+JoystickButton31"));
            //			InputManager.MapStateToInput("My State1",new InputCombination("Mouse1+Joystick12AxisXPositive(x2)+B"));


			int ManualAddedState = Animator.StringToHash ("ManualAddedSTATE");

            ////easiest way to map state to combination (ex.of single W and C click)
            if (!InputManager.HasInputState(ManualAddedState))
			    InputManager.MapStateToInput(ManualAddedState,InputPlayer.Player.Player0, InputCode.W.SINGLE, InputCode.C.SINGLE);

            //add secondary
           // InputManager.MapStateToInput("AnyJoystick", InputPlayer.Player.Player0,InputCode.JoystickAxisXPositive.SINGLE);

            UnityEngine.Debug.Log("Log:" + InputManager.Log());


			InputManager.addEventListener(ManualAddedState).UP += onUp;
			InputManager.addEventListener(ManualAddedState).DOWN += onDown;

          



        }


        // Update is called once per frame
        void Update()
        {



         

            //
            //						if (InputManager.GetInputHold ((int)States.Wave)) {
            //							
            //								Debug.Log ("Wave -Hold");
            //								// animator.Play((int)States.Wave);
            //								//	animator.Play (Animator.StringToHash ("Wave"));
            //						}
            //					
            //
            //
            //						if (InputManager.GetInputDown ((int)States.Wave)) {
            //           
            //								Debug.Log ("Wave -Down");
            //								// animator.Play((int)States.Wave);
            //								//	animator.Play (Animator.StringToHash ("Wave"));
            //						}
            //
            //						if (InputManager.GetInputUp ((int)States.Wave)) {
            //							
            //								Debug.Log ("Wave -Up");
            //								// animator.Play((int)States.Wave);
            //								//	animator.Play (Animator.StringToHash ("Wave"));
            //						}
            //
            //
            //						if (InputManager.GetInputDown ((int)States.MyCustomState)) {
            //							Debug.Log (States.MyCustomState + "-Down");
            //							// animator.Play((int)States.Wave);
            //						}
            //
            //						if (InputManager.GetInputUp ((int)States.MyCustomState)) {
            //								Debug.Log (States.MyCustomState + "-Up");
            //								// animator.Play((int)States.Wave);
            //						}


            #region Testing Fuzer
            //Demo of use of M+ and Acc data from Wiimote
            //you will use ofcourse some sofisticate algo based on Kalman or Magdwick filter to process data


            //take all devices of type WiimoteDevice
            List<WiimoteDevice> wiimoteDevices = InputManager.GetDevices<WiimoteDevice>();

            if (wiimoteDevices.Count > 0)
            {
                WiimoteDevice device = wiimoteDevices[0];

                if (device.isReady)
                {
                    if (device.motionPlus != null && device.motionPlus.Enabled)
                    {
                        if (lastTime < 0f)
                        {
                            lastTime = Time.time;
                        }

                        complementaryFuzer.Update(device.Axis[JoystickAxis.AxisAccX].value, device.Axis[JoystickAxis.AxisAccY].value, device.Axis[JoystickAxis.AxisAccZ].value, device.motionPlus.Values.x * DEG_TO_RAD, device.motionPlus.Values.y * DEG_TO_RAD, device.motionPlus.Values.z * DEG_TO_RAD, Time.time - lastTime);
                        lastTime = Time.time;



                        Angles.x = (float)(complementaryFuzer.Angles.x * RAD_TO_DEG);
                        Angles.y = (float)(complementaryFuzer.Angles.z * RAD_TO_DEG);
                        Angles.z = (float)(complementaryFuzer.Angles.y * RAD_TO_DEG);

                        // UnityEngine.Debug.Log(Angles.z);
                        // UnityEngine.Debug.Log(Angles.x + " " + Angles.y + " " + Angles.z);

                        wiimote.transform.rotation = Quaternion.Euler(Angles);
                    }
                }
            }

            #endregion







            //	
            //
//            if (InputManager.GetInputHold(Animator.StringToHash("WalkBackward")))
//            {
//                Debug.Log("WalkBackward-Hold");
//            }
//
//
//            if (InputManager.GetInputDown(Animator.StringToHash("WalkBackward")))
//            {
//                Debug.Log("WalkBackward-Down");
//            }
//
//            if (InputManager.GetInputUp(Animator.StringToHash("WalkBackward")))
//            {
//                Debug.Log("WalkBackward-Up");
//            }
//
//
//            if (InputManager.GetInputHold(Animator.StringToHash("WalkForward")))
//            {
//                Debug.Log("WalkForward-Hold");
//            }
//
//            if (InputManager.GetInputDown(Animator.StringToHash("WalkForward")))
//            {
//                Debug.Log("WalkForward-Down");
//            }
//
//            if (InputManager.GetInputUp(Animator.StringToHash("WalkForward")))
//            {
//                Debug.Log("WalkForward-Up");
//            }
            //
            ////

            //Bind Axis as one part

            //						InputManager.MapStateToInput ("WalkForward", KeyCodeExtension.W.SINGLE);
            //						InputManager.MapStateToInput ("WalkForward", 1, KeyCodeExtension.Joystick1AxisXPositive.SINGLE);
            //
            //
            //						InputManager.MapStateToInput ("WalkBackward", KeyCodeExtension.S.SINGLE);
            //						InputManager.MapStateToInput ("WalkBackward", 1, KeyCodeExtension.Joystick1AxisYNegative.SINGLE);
            //
            //						

            //			float axisPos = InputManager.GetInput (Animator.StringToHash ("WalkForward"), 0.3f, 0.1f, 0.2f);
            //
            //			float axisNeg= InputManager.GetInput (Animator.StringToHash ("WalkBackward"),  0.3f, 0.1f, 0.1f);
            //
            //			float analogVal=axisPos - axisNeg;

            //Debug.Log (analogVal);//would go from  -1 to 1




            // Hardware normalized value in range of -1f to 1f (keys,mouse would return 0f or 1f, triggers 0f to 1f)
            //float analogVal2= InputManager.GetInput (Animator.StringToHash ("WalkBackward"));
            //Debug.Log (analogVal2);




        }

        void OnGUI()
        {

            //			if (InputManager.Devices.ContainsIndex(0))
            //				XDevice = InputManager.Devices.GetDeviceAt(0) as XInputDevice;
            //
            //			if (GUI.Button(new Rect(150, 590, 100, 130), "Rumble"))
            //			{
            //				
            //
            //				
            //				XDevice.SetMotor(0x80,0xFF);
            //				
            //			
            //				
            //
            //
            //				
            //			}


            //don't take device here in the loop this is just for demo

			//don't take device here in the loop this is just for demo
			List<ThrustmasterRGTFFDDevice> devices = InputManager.GetDevices<ThrustmasterRGTFFDDevice> ();
			if (devices.Count>0)
				TTFFDDevice = devices[0];





            if (TTFFDDevice == null)
                return;

            //#if UNITY_ANDROID

            vSliderValue = GUI.HorizontalSlider(new Rect(25, 520, 400, 100), vSliderValue, 255.0F, 0.0F);
            // #endif

            if (vSliderValue != vSliderValuePrev)
                // device.SetMotor(Convert.ToByte(vSliderValue), 0xA7, onMotorSet);
                TTFFDDevice.SetMotor(Convert.ToByte(vSliderValue), Convert.ToByte(vSliderValue), onMotorSet);

            vSliderValuePrev = vSliderValue;



            if (GUI.Button(new Rect(25, 590, 100, 130), "Stop Motor"))
            {
                //timer.Stop();
                if (runEffectEnumerator != null)
                    StopCoroutine(runEffectEnumerator);
                TTFFDDevice.StopMotor(onMotorStop);
                vSliderValue = 128;
            }

            if (GUI.Button(new Rect(150, 590, 100, 130), "Rumble"))
            {

                runEffectEnumerator = runEffect();

                TTFFDDevice.StopMotor(onMotorStop);

                StartCoroutine(runEffectEnumerator);


                //char buf[] = {0x00, 0x01, 0x0f, 0xc0, 0x00, large, small, 0x00, 0x00, 0x00, 0x00, 0x00};

            }
        }

        void onMotorStop(bool success)
        {
            Debug.Log("Motor stop was successful:" + success);
        }

        void onMotorSet(bool success)
        {
            Debug.Log("Motor command was successful:" + success);
        }

        IEnumerator runEffect()
        {
            while (true)
            {
                forceX += 0xA7;
                TTFFDDevice.SetMotor(forceX, forceX, onMotorSet);

                yield return new WaitForSeconds(0.5f);
            }

            // yield break;

        }

        void onTimerElapsed(object sender, ElapsedEventArgs args)
        {
            forceX += 0xA7;
            TTFFDDevice.SetMotor(forceX, forceX, onMotorSet);
        }







        /// <summary>
        /// DONT FORGET TO CLEAN AFTER YOURSELF
        /// </summary>
        void OnDestroy()
        {
            if (TTFFDDevice != null)
                TTFFDDevice.StopMotor();


            InputManager.Dispose();
        }
    }
}
