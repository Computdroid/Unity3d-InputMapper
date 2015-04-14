using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ws.winx.platform;
using UnityEngine;
using ws.winx.devices;
using ws.winx.unity;
using System.IO;

namespace ws.winx.drivers
{
	public class UnityDriver:IDriver
	{



		public UnityDriver ()
		{

		}
			
		public devices.IDevice ResolveDevice (IHIDDevice hidDevice)
		{
			int i = 0;

			int inx;

			//Give Unity Update(Input works only in Update on Main single thread
			//and this funciton is happen on Event on other thread)
			//to be done so GetJoysticksNames return something
			//!!! Not tested yet on WIN
			System.Threading.Thread.Sleep (300);//sleep this Event add device thread
                    
			//find device match based on "names"
			string[] names = Input.GetJoystickNames ();
			inx = Array.IndexOf (names, hidDevice.Name);


			Debug.Log (String.Join (",", names));

			if (inx < 0)
				return null;

			if (inx > 3) {
				Debug.LogWarning ("Unity supports up to 4 Joysticks");
				return null;
			}

			DeviceProfile profile = hidDevice.loadProfile ();

					
                           
                        

			JoystickDevice device = new JoystickDevice (inx, hidDevice.PID, hidDevice.VID, hidDevice.ID, 12, 20, this);
			device.Name = hidDevice.Name;
			device.profile = profile;

			int numAxis = device.Axis.Count;
			int numButtons = device.Buttons.Count;

			for (; i < numAxis; i++) {
				device.Axis [i] = new AxisDetails ();
				if (profile != null && profile.axisNaming.Length > i) {
					device.Axis [i].name = profile.axisNaming [i];

				}
                
			}

			for (i=0; i < numButtons; i++) {
				device.Buttons [i] = new ButtonDetails ();
				if (profile != null && profile.buttonNaming.Length > i) {
					device.Buttons [i].name = profile.buttonNaming [i];
				}
			}
				
				

			return device;
		}

		public void Update (devices.IDevice device)
		{

			if (device == null)
				return;


			int i = 0;
			int numAxis = device.Axis.Count;
			int numButtons = device.Buttons.Count;
                   
			//int index = device.Index;//if this was as easy=> Unity reorder/insert new added devices

			//  Debug.Log(String.Join(Input.GetJoystickNames()
			int index = Array.IndexOf (Input.GetJoystickNames (), device.Name);
			if (index < 0) {
				Debug.LogWarning ("Devices can't be found by UnityDriver");		
				return;
			}

			// Debug.Log("axis value raw:" + Input.GetAxisRaw("10") + " " + Input.GetAxis("11"));
			//Debug.Log("axis value raw:" +);
			//   joystick.Axis[0].value=Input.GetAxis("00");//index-of joystick, i-ord number of axis
			// Debug.Log("axis value:" + joystick.Axis[0].value + " state:" + joystick.Axis[0].buttonState);

			// index = 1;
			float axisValue = 0f;
			for (; i < numAxis; i++) {

				axisValue = Input.GetAxisRaw (index.ToString () + i.ToString ());
				device.Axis [i].value = axisValue;
				//(Input.GetAxis (index.ToString () + i.ToString ()) + 1f) * 0.5f;//index-of joystick, i-ord number of axis

				//axisValue = Input.GetAxis (index.ToString () + i.ToString ()) + " ";

//							if(i==1){
//								Debug.Log(axisValue);
//								
//							}

			}

						


			for (i=0; i < numButtons; i++) {
               
				device.Buttons [i].value = Input.GetKey ((KeyCode)Enum.Parse (typeof(KeyCode), "Joystick" + (index + 1) + "Button" + i)) == true ? 1f : 0f;

			}
		}




        #region ButtonDetails
		public sealed class ButtonDetails : IButtonDetails
		{

            #region Fields

			float _value;
			uint _uid;
			ButtonState _buttonState;
			string _name;

            #region IDeviceDetails implementation
			public string name {
				get {
					return _name;
				}
				set {
					_name = value;
				}
			}

			public uint uid {
				get {
					return _uid;
				}
				set {
					_uid = value;
				}
			}

			public ButtonState buttonState {
				get { return _buttonState; }
			}

			public float value {
				get {
					return _value;
					//return (_buttonState==JoystickButtonState.Hold || _buttonState==JoystickButtonState.Down);
				}
				set {

					_value = value;

					//  UnityEngine.Debug.Log("Value:" + _value);

					//if pressed==TRUE
					//TODO check the code with triggers
					if (value > 0) {
						if (_buttonState == ButtonState.None
							|| _buttonState == ButtonState.Up) {

							_buttonState = ButtonState.Down;



						} else {
							//if (buttonState == JoystickButtonState.Down)
							_buttonState = ButtonState.Hold;

						}


					} else { //
						if (_buttonState == ButtonState.Down
							|| _buttonState == ButtonState.Hold) {
							_buttonState = ButtonState.Up;
						} else {//if(buttonState==JoystickButtonState.Up){
							_buttonState = ButtonState.None;
						}

					}
				}
			}
            #endregion
            #endregion

            #region Constructor
			public ButtonDetails (uint uid = 0)
			{
				this.uid = uid;
			}
            #endregion






		}

        #endregion

        #region AxisDetails
		public sealed class AxisDetails : IAxisDetails
		{

            #region Fields
			float _value;
			uint _uid;
			int _min;
			int _max;
			ButtonState _buttonState = ButtonState.None;
			bool _isNullable;
			bool _isHat;
			bool _isTrigger;
			string _name;

            #region IAxisDetails implementation

			public bool isTrigger {
				get {
					return _isTrigger;
				}
				set {
					_isTrigger = value;
				}
			}

			public int min {
				get {
					return _min;
				}
				set {
					_min = value;
				}
			}

			public int max {
				get {
					return _max;
				}
				set {
					_max = value;
				}
			}

			public bool isNullable {
				get {
					return _isNullable;
				}
				set {
					_isNullable = value;
				}
			}

			public bool isHat {
				get {
					return _isHat;
				}
				set {
					_isHat = value;
				}
			}


            #endregion


            #region IDeviceDetails implementation
			public string name {
				get {
					return _name;
				}
				set {
					_name = value;
				}
			}

			public uint uid {
				get {
					return _uid;
				}
				set {
					_uid = value;
				}
			}


            #endregion

			public ButtonState buttonState {
				get { return _buttonState; }
			}

			public float value {
				get { return _value; }
				set {
					
					if (value == -1 || value == 1) {
						if (_buttonState == ButtonState.None
						    //|| _buttonState == ButtonState.PosToUp || _buttonState==ButtonState.NegToUp)
						    ) {
							
							_buttonState = ButtonState.Down;
							
							//Debug.Log("val:"+value+"_buttonState:"+_buttonState);
							
						} else {
							_buttonState = ButtonState.Hold;
						}
						
						
					} else {
						
						if (_buttonState == ButtonState.Down
							|| _buttonState == ButtonState.Hold) {
							
							//if previous value was >0 => PosToUp
							if (_value == 1)
								_buttonState = ButtonState.PosToUp;
							else
								_buttonState = ButtonState.NegToUp;
							
							//Debug.Log("val:"+value+"_buttonState:"+_buttonState);
							
						} else {//if(buttonState==JoystickButtonState.Up){
							_buttonState = ButtonState.None;
						}
						
						
					}
					
					
					_value = value;
					
					
					
				}//set
			}


            #endregion

		}

        #endregion

	}
}
