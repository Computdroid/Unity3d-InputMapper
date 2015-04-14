using System;
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

using ws.winx.platform;
using ws.winx.unity;
using ws.winx.csharp.extensions;
using UnityEngine.Events;
using UnityEngine.Serialization;


namespace ws.winx.input.components{
[RequireComponent(typeof(UserInterfaceWindow))]
public class InputComponent : MonoBehaviour
{

		

		//
		// Fields
		//


//TODO Maybe
//		public TextAsset profileList;
//
//		public TextAsset[] profiles;

		public string settingsFileName="InputSettings.xml";

		public DeviceProfiles profiles;

		[FormerlySerializedAs ("onLoad"), UnityEngine.SerializeField]
		private UnityEvent m_onLoad = new UnityEvent ();


		//
		// Nested Types
		//
		[Serializable]
		public class InputComponentEvent : UnityEvent
		{
		}
		
	
	//
	// Properties
	//
	public UnityEvent onLoad
	{
		get
		{
			return this.m_onLoad;
		}
		set
		{
			this.m_onLoad = value;
		}
	}


		UserInterfaceWindow ui;

		// Use this for initialization
		void Start ()
		{
				

				   if (String.IsNullOrEmpty (settingsFileName)) {

						Debug.LogError("Please add settings(.xml or .bin) fileName from StreamingAssets");
						return;
					}


		ui= this.GetComponent<UserInterfaceWindow>();

		//supporting devices with custom drivers
		//When you add them add specialized first then XInputDriver  then wide range supporting drivers like WinMM or OSXDriver
		//supporting devices with custom drivers
		//When you add them add specialized first then XInputDriver  then wide range supporting drivers WinMM or OSXDriver
		#if (UNITY_STANDALONE_WIN)
		InputManager.AddDriver(new ThrustMasterDriver());
		//InputManager.AddDriver(new WiiDriver());
		InputManager.AddDriver(new XInputDriver());
		//change default driver
		//InputManager.hidInterface.defaultDriver=new UnityDriver();
		#endif
		#if (UNITY_STANDALONE_OSX)
		InputManager.AddDriver(new ThrustMasterDriver());
		InputManager.AddDriver(new XInputDriver());
		//change default driver
		//InputManager.hidInterface.defaultDriver=new UnityDriver();
		
		#endif
		
		#if (UNITY_STANDALONE_ANDROID)
		InputManager.AddDriver(new ThrustMasterDriver());
		InputManager.AddDriver(new XInputDriver());
		#endif
		
			if (profiles == null)
								InputManager.hidInterface.LoadProfiles ("DeviceProfiles");
						else
								InputManager.hidInterface.SetProfiles (profiles);

		InputManager.hidInterface.Enumerate();
		
		
		

		
		//if you want to load some states from .xml and add custom manually first load settings xml
		//!!!Application.streamingAssetPath gives "Raw" folder in web player
		
		#if (UNITY_STANDALONE || UNITY_EDITOR ) && !UNITY_WEBPLAYER && !UNITY_ANDROID
		//UnityEngine.Debug.Log("Standalone");

		
		
		if (ui != null && !String.IsNullOrEmpty (settingsFileName))
		{//settingsXML would trigger internal loading mechanism (only for testing)

			//load settings from external file

			ui.settings=InputManager.loadSettings(Path.Combine(Application.streamingAssetsPath, settingsFileName));

			//	ui.settings=InputManager.loadSettingsFromXMLText(Path.Combine(Application.streamingAssetsPath,settingsFileName));




			//dispatch Event 
			this.m_onLoad.Invoke();

		}
		
		

		
		#endif
		
		#region Load InputSettings.xml Android
		#if UNITY_ANDROID

		
		
		Loader request = new Loader();
		
		
		if (Application.platform == RuntimePlatform.Android)
		{
				if (File.Exists(Application.persistentDataPath + "/" + settingsFileName))
			{
				
				if (ui != null)
				{
					Debug.Log("Game>> Try to load from " + Application.persistentDataPath);

						ui.settings=InputManager.loadSettings(Application.persistentDataPath + "/" + settingsFileName);
					

						//dispatch load complete
						this.m_onLoad.Invoke();
					
					return;
					
				}
			}
			else
			{// content of StreamingAssets get packed inside .APK and need to be load with WWW
				request.Add(Path.Combine(Application.streamingAssetsPath, "InputSettings.xml"));
				request.Add(Path.Combine(Application.streamingAssetsPath, "profiles.txt"));
				request.Add(Path.Combine(Application.streamingAssetsPath, "xbox360_drd.txt"));
				//....

				//unpack everything in presistentDataPath
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
				
					InputManager.loadSettings(Path.Combine(Application.streamingAssetsPath, settingsFileName));
				
				
				
				ui.settings = InputManager.Settings;
			}
			
			

			
		}
		
		
		
		#endif
		#endregion
		
		#if(UNITY_WEBPLAYER || UNITY_EDITOR) && !UNITY_STANDALONE && !UNITY_ANDROID
		Loader request = new Loader();
		
		//UNITY_WEBPLAYER: Application.dataPath "http://localhost/appfolder/"
		request.Add(Application.dataPath+"/StreamingAssets/"+settingsFileName);
		
		
		request.LoadComplete += new EventHandler<LoaderEvtArgs<List<WWW>>>(onLoadComplete);
		request.Error += new EventHandler<LoaderEvtArgs<String>>(onLoadError);
		request.LoadItemComplete += new EventHandler<LoaderEvtArgs<WWW>>(onLoadItemComplete);
		request.load();
		#endif
		}



			#if (UNITY_WEBPLAYER || UNITY_EDITOR || UNITY_ANDROID) && !UNITY_STANDALONE
			void onLoadComplete(object sender, LoaderEvtArgs<List<WWW>> args)
			{
				// Debug.Log(((List<WWW>)args.data).ElementAt(0).text);
				
				if (System.Threading.Thread.CurrentThread.ManagedThreadId != 1) return;
				
				
				//UnityEngine.Debug.Log("WebPlayer " + Path.Combine(Path.Combine(Application.dataPath, "StreamingAssets"), "InputSettings.xml"));
				
				
				
				
				
				
				if (ui != null)
				{
					InputManager.loadSettingsFromText(args.data.ElementAt(0).text);
					ui.settings = InputManager.Settings;
				}
				
				
			   //dispatch load complete
				this.m_onLoad.Invoke();
				
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

	
		// Update is called once per frame
		void Update ()
		{
			InputManager.dispatchEvent();
		}


		void OnGUI(){

			if (ui != null && ui.settings != null && GUI.Button (new Rect (0, 0, 100, 30), "Settings"))
								ui.enabled = !ui.enabled;



		}


        /// <summary>
        /// DONT FORGET TO CLEAN AFTER YOURSELF
        /// </summary>
        void OnDestroy()
        {
            InputManager.Dispose();
        }
}
}
