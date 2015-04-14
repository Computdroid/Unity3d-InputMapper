using UnityEngine;
using System.Collections;
using System;
using System.Reflection;

namespace ws.winx.unity.attributes
{
	public class EnumAttribute : PropertyAttribute
	{
		Type _type;

		public Type GetEnumType(){

			return _type;
		}

		public Enum GetEnumValue(){

			return Enum.GetValues (_type).GetValue(0) as Enum;

		}

		public EnumAttribute(string typeName){
			_type = Type.GetType (typeName);
		}

		public EnumAttribute(Type enumType){
			_type = enumType;

		}
			
	}
}

